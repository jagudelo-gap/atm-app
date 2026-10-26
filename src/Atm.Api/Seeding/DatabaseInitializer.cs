using Atm.Accounts;
using Atm.Accounts.Contract;
using Atm.Common.Cqrs;
using Atm.Common.Data;
using Atm.Transactions;
using Atm.Users;
using Atm.Users.Contract;

namespace Atm.Api.Seeding;

/// <summary>Applies module migrations in dependency order, then seeds the demo user when it does not exist yet.</summary>
public sealed class DatabaseInitializer(IServiceScopeFactory scopes, ILogger<DatabaseInitializer> logger)
{
    public const string SeedUserName = "user-1";

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        // Accounts' foreign key needs Users' table, and Transactions' needs Accounts'.
        await sp.MigrateUsersModuleAsync(ct);
        await sp.MigrateAccountsModuleAsync(ct);
        await sp.MigrateTransactionsModuleAsync(ct);

        var users = sp.GetRequiredService<IUsersModule>();
        if ((await users.GetByNameAsync(SeedUserName, ct)).IsSuccess) return;

        // Seed only a fresh database, so balances survive restarts. User and accounts commit together.
        var accounts = sp.GetRequiredService<IAccountsModule>();
        var seeded = await sp.GetRequiredService<IUnitOfWork>().ExecuteAsync(async () =>
        {
            var user = await users.EnsureSeededAsync(SeedUserName, ct);
            await accounts.ResetToSeedAsync(user.Id, ct);
            return Result<Guid>.Ok(user.Id);
        }, ct);
        logger.LogInformation("Seeded demo user {UserName} ({UserId})", SeedUserName, seeded.Value);
    }
}
