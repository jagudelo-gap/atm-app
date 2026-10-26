using Atm.Accounts;
using Atm.Accounts.Data;
using Atm.Common.Data;
using Atm.Users;
using Atm.Users.Contract;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Accounts.Tests;

/// <summary>Temp-file SQLite database with the Users then Accounts migrations applied.</summary>
public sealed class AccountsFixture : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"accounts-{Guid.NewGuid():N}.db");

    public ServiceProvider Root { get; }

    public AccountsFixture()
    {
        Root = new ServiceCollection()
            .AddSharedSqlite($"Data Source={_file};Pooling=False")
            .AddUsersModule()
            .AddAccountsModule()
            .BuildServiceProvider();

        using var s = Root.CreateScope();
        // UsersDbContext is internal to Atm.Users; resolve it by name so Atm.Users is not modified.
        var usersContextType = typeof(UsersModuleRegistration).Assembly.GetType("Atm.Users.Data.UsersDbContext")!;
        ((DbContext)s.ServiceProvider.GetRequiredService(usersContextType)).Database.Migrate();
        s.ServiceProvider.GetRequiredService<AccountsDbContext>().Database.Migrate();
    }

    public async Task<Guid> SeedUserAsync(string name)
    {
        using var s = Root.CreateScope();
        return (await s.ServiceProvider.GetRequiredService<IUsersModule>().EnsureSeededAsync(name)).Id;
    }

    public void Dispose()
    {
        Root.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }
}
