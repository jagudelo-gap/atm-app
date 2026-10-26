using Atm.Accounts.Contract;
using Atm.Accounts.Data;
using Atm.Common.Cqrs;
using Atm.Common.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Accounts;

public static class AccountsModuleRegistration
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection s)
    {
        s.AddCqrs(typeof(AccountsModuleRegistration).Assembly);
        s.AddModuleDbContext<AccountsDbContext>("__EFMigrations_Accounts");
        s.AddScoped<IAccountsModule, AccountsModule>();
        return s;
    }

    /// <summary>Applies this module's migrations (after Users'). Call on a scoped provider.</summary>
    public static Task MigrateAccountsModuleAsync(this IServiceProvider scoped, CancellationToken ct = default) =>
        scoped.GetRequiredService<AccountsDbContext>().Database.MigrateAsync(ct);

    /// <summary>Runs a trivial query on this module's table; throws when it is not reachable.</summary>
    public static Task CheckAccountsModuleAsync(this IServiceProvider scoped, CancellationToken ct = default) =>
        scoped.GetRequiredService<AccountsDbContext>().Accounts.AnyAsync(ct);
}
