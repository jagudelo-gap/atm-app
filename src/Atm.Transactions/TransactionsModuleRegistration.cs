using Atm.Common.Cqrs;
using Atm.Common.Data;
using Atm.Transactions.Contract;
using Atm.Transactions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Transactions;

public static class TransactionsModuleRegistration
{
    public static IServiceCollection AddTransactionsModule(this IServiceCollection s)
    {
        s.AddCqrs(typeof(TransactionsModuleRegistration).Assembly);
        s.AddModuleDbContext<TransactionsDbContext>("__EFMigrations_Transactions");
        s.AddScoped<ITransactionsModule, TransactionsModule>();
        return s;
    }

    /// <summary>Applies this module's migrations (after Accounts'). Call on a scoped provider.</summary>
    public static Task MigrateTransactionsModuleAsync(this IServiceProvider scoped, CancellationToken ct = default) =>
        scoped.GetRequiredService<TransactionsDbContext>().Database.MigrateAsync(ct);

    /// <summary>Runs a trivial query on this module's table; throws when it is not reachable.</summary>
    public static Task CheckTransactionsModuleAsync(this IServiceProvider scoped, CancellationToken ct = default) =>
        scoped.GetRequiredService<TransactionsDbContext>().Transactions.AnyAsync(ct);
}
