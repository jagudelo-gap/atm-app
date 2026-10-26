using Atm.Accounts;
using Atm.Accounts.Contract;
using Atm.Common.Data;
using Atm.Transactions;
using Atm.Transactions.Data;
using Atm.Users;
using Atm.Users.Contract;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Transactions.Tests;

/// <summary>Temp-file SQLite database with the Users, Accounts then Transactions migrations applied.</summary>
public sealed class TransactionsFixture : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"transactions-{Guid.NewGuid():N}.db");

    public ServiceProvider Root { get; }

    public TransactionsFixture(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection()
            .AddSharedSqlite($"Data Source={_file};Pooling=False")
            .AddUsersModule()
            .AddAccountsModule()
            .AddTransactionsModule();
        configure?.Invoke(services);
        Root = services.BuildServiceProvider();

        using var s = Root.CreateScope();
        // Users' and Accounts' contexts are internal; resolve them by name so those modules are not modified.
        Migrate(s, typeof(UsersModuleRegistration).Assembly, "Atm.Users.Data.UsersDbContext");
        Migrate(s, typeof(AccountsModuleRegistration).Assembly, "Atm.Accounts.Data.AccountsDbContext");
        s.ServiceProvider.GetRequiredService<TransactionsDbContext>().Database.Migrate();
    }

    private static void Migrate(IServiceScope s, System.Reflection.Assembly assembly, string contextTypeName)
    {
        var type = assembly.GetType(contextTypeName)!;
        ((DbContext)s.ServiceProvider.GetRequiredService(type)).Database.Migrate();
    }

    /// <summary>Seeds the user with Checking 1,500.00 and Savings 5,000.00.</summary>
    public async Task<(Guid UserId, AccountDto Checking, AccountDto Savings)> SeedAsync(string name)
    {
        using var s = Root.CreateScope();
        var userId = (await s.ServiceProvider.GetRequiredService<IUsersModule>().EnsureSeededAsync(name)).Id;
        var accounts = s.ServiceProvider.GetRequiredService<IAccountsModule>();
        await accounts.ResetToSeedAsync(userId);
        var list = await accounts.GetAccountsAsync(userId);
        return (userId, list.Single(a => a.Type == "Checking"), list.Single(a => a.Type == "Savings"));
    }

    public void Dispose()
    {
        Root.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }
}
