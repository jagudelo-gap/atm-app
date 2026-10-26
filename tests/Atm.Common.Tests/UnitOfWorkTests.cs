using Atm.Common.Cqrs;
using Atm.Common.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Atm.Common.Tests;

public class Row { public int Id { get; set; } }

public class AContext(DbContextOptions<AContext> o) : DbContext(o)
{
    public DbSet<Row> Rows => Set<Row>();
    protected override void OnModelCreating(ModelBuilder b) => b.Entity<Row>().ToTable("A");
}

public class BContext(DbContextOptions<BContext> o) : DbContext(o)
{
    public DbSet<Row> Rows => Set<Row>();
    protected override void OnModelCreating(ModelBuilder b) => b.Entity<Row>().ToTable("B");
}

public sealed class UnitOfWorkTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"uow-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider _root;

    public UnitOfWorkTests()
    {
        _root = new ServiceCollection()
            .AddSharedSqlite($"Data Source={_file};Pooling=False")
            .AddModuleDbContext<AContext>("__EFMigrations_A")
            .AddModuleDbContext<BContext>("__EFMigrations_B")
            .BuildServiceProvider();
        using var s = _root.CreateScope();
        s.ServiceProvider.GetRequiredService<AContext>().Database.EnsureCreated();
        var b = s.ServiceProvider.GetRequiredService<BContext>();
        b.Database.ExecuteSqlRaw("CREATE TABLE B (Id INTEGER PRIMARY KEY)");
    }

    public void Dispose()
    {
        _root.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }

    private async Task<(int a, int b)> Counts()
    {
        using var s = _root.CreateScope();
        return (await s.ServiceProvider.GetRequiredService<AContext>().Rows.CountAsync(),
                await s.ServiceProvider.GetRequiredService<BContext>().Rows.CountAsync());
    }

    private static async Task Add(IServiceProvider sp)
    {
        var a = sp.GetRequiredService<AContext>();
        var b = sp.GetRequiredService<BContext>();
        a.Rows.Add(new Row { Id = 1 });
        b.Rows.Add(new Row { Id = 1 });
        await a.SaveChangesAsync();
        await b.SaveChangesAsync();
    }

    [Fact]
    public async Task Commits_changes_from_two_contexts_when_result_is_success()
    {
        using var s = _root.CreateScope();
        var r = await s.ServiceProvider.GetRequiredService<IUnitOfWork>().ExecuteAsync(async () =>
        {
            await Add(s.ServiceProvider);
            return Result<int>.Ok(1);
        });
        Assert.True(r.IsSuccess);
        Assert.Equal((1, 1), await Counts());
    }

    [Fact]
    public async Task Rolls_back_both_contexts_when_result_is_failure()
    {
        using var s = _root.CreateScope();
        var r = await s.ServiceProvider.GetRequiredService<IUnitOfWork>().ExecuteAsync(async () =>
        {
            await Add(s.ServiceProvider);
            return Result<int>.Fail(new Error(ErrorKind.Business, "no"));
        });
        Assert.False(r.IsSuccess);
        Assert.Equal((0, 0), await Counts());
    }

    [Fact]
    public async Task Rolls_back_both_contexts_and_rethrows_when_work_throws()
    {
        using var s = _root.CreateScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.ServiceProvider.GetRequiredService<IUnitOfWork>().ExecuteAsync<int>(async () =>
            {
                await Add(s.ServiceProvider);
                throw new InvalidOperationException("boom");
            }));
        Assert.Equal((0, 0), await Counts());
    }

    [Fact]
    public async Task Enlists_contexts_resolved_before_and_after_execute_starts()
    {
        using var s = _root.CreateScope();
        var early = s.ServiceProvider.GetRequiredService<AContext>();
        await s.ServiceProvider.GetRequiredService<IUnitOfWork>().ExecuteAsync(async () =>
        {
            early.Rows.Add(new Row { Id = 1 });
            await early.SaveChangesAsync();
            s.ServiceProvider.GetRequiredService<BContext>().Rows.Add(new Row { Id = 1 });
            await s.ServiceProvider.GetRequiredService<BContext>().SaveChangesAsync();
            return Result<int>.Fail(new Error(ErrorKind.Business, "no"));
        });
        Assert.Equal((0, 0), await Counts());
    }

    [Fact]
    public async Task Failing_enlistment_rolls_back_and_resets_state()
    {
        using var other = new SqliteConnection("Data Source=:memory:");
        other.Open();
        var sp = new ServiceCollection()
            .AddSharedSqlite($"Data Source={_file};Pooling=False")
            .AddModuleDbContext<AContext>("__EFMigrations_A")
            .Replace(ServiceDescriptor.Scoped(_ => new DbContextOptionsBuilder<AContext>().UseSqlite(other).Options))
            .BuildServiceProvider();
        using var s = sp.CreateScope();
        s.ServiceProvider.GetRequiredService<AContext>(); // enlisted; bound to a different connection
        var uow = s.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await Assert.ThrowsAnyAsync<Exception>(() => uow.ExecuteAsync(() => Task.FromResult(Result<int>.Ok(1))));
        // state reset: not stuck "in progress"
        var ex = await Record.ExceptionAsync(() => uow.ExecuteAsync(() => Task.FromResult(Result<int>.Ok(1))));
        Assert.DoesNotContain("already in progress", ex?.Message ?? "");
        Assert.Equal((0, 0), await Counts());
    }

    [Fact]
    public async Task Nested_execute_is_rejected()
    {
        using var s = _root.CreateScope();
        var uow = s.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ex = await Record.ExceptionAsync(() => uow.ExecuteAsync(() =>
            uow.ExecuteAsync(() => Task.FromResult(Result<int>.Ok(1)))));
        Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public async Task Same_scope_can_run_sequential_units_of_work()
    {
        using var s = _root.CreateScope();
        var uow = s.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await uow.ExecuteAsync(async () => { await Add(s.ServiceProvider); return Result<int>.Fail(new Error(ErrorKind.Business, "x")); });
        await uow.ExecuteAsync(async () => { await Add(s.ServiceProvider); return Result<int>.Ok(1); });
        Assert.Equal((1, 1), await Counts());
    }

    [Fact]
    public async Task Transaction_takes_the_write_lock_up_front()
    {
        using var s = _root.CreateScope();
        await s.ServiceProvider.GetRequiredService<IUnitOfWork>().ExecuteAsync(() =>
        {
            using var other = new SqliteConnection($"Data Source={_file};Pooling=False");
            other.Open();
            using var cmd = other.CreateCommand();
            cmd.CommandText = "BEGIN IMMEDIATE";
            cmd.CommandTimeout = 1;
            Assert.Throws<SqliteException>(() => cmd.ExecuteNonQuery()); // locked => BEGIN IMMEDIATE was used
            return Task.FromResult(Result<int>.Ok(1));
        });
    }
}
