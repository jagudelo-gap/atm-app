using Atm.Common.Cqrs;
using Atm.Common.Data;
using Atm.Users;
using Atm.Users.Contract;
using Atm.Users.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Users.Tests;

public sealed class GetUserByNameTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"users-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider _root;

    public GetUserByNameTests()
    {
        _root = new ServiceCollection()
            .AddSharedSqlite($"Data Source={_file};Pooling=False")
            .AddUsersModule()
            .BuildServiceProvider();
        using var s = _root.CreateScope();
        s.ServiceProvider.GetRequiredService<UsersDbContext>().Database.Migrate();
    }

    public void Dispose()
    {
        _root.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }

    [Fact]
    public async Task GetByName_returns_user_when_seeded()
    {
        using var s = _root.CreateScope();
        var users = s.ServiceProvider.GetRequiredService<IUsersModule>();
        var seeded = await users.EnsureSeededAsync("alice");

        var r = await users.GetByNameAsync("alice");

        Assert.True(r.IsSuccess);
        Assert.Equal(seeded.Id, r.Value.Id);
        Assert.Equal("alice", r.Value.Name);
    }

    [Fact]
    public async Task GetByName_returns_business_error_when_missing()
    {
        using var s = _root.CreateScope();
        var r = await s.ServiceProvider.GetRequiredService<IUsersModule>().GetByNameAsync("nobody");

        Assert.False(r.IsSuccess);
        var e = Assert.Single(r.Errors);
        Assert.Equal(ErrorKind.Business, e.Kind);
        Assert.Equal("User not found", e.Message);
    }

    [Fact]
    public async Task EnsureSeeded_is_idempotent()
    {
        using var s = _root.CreateScope();
        var users = s.ServiceProvider.GetRequiredService<IUsersModule>();
        var a = await users.EnsureSeededAsync("alice");
        var b = await users.EnsureSeededAsync("alice");

        Assert.Equal(a.Id, b.Id);
        Assert.Equal(1, await s.ServiceProvider.GetRequiredService<UsersDbContext>().Users.CountAsync());
    }
}
