using System.Text.Json;
using Atm.Accounts;
using Atm.Transactions;
using Atm.Users;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Atm.Api.Health;

/// <summary>/health: the database plus one check per module. 200 when all pass, 503 otherwise.</summary>
public static class HealthRegistration
{
    public static IServiceCollection AddAtmHealthChecks(this IServiceCollection s)
    {
        var checks = s.AddHealthChecks();
        Add(checks, "database", PingDatabaseAsync);
        Add(checks, "users", UsersModuleRegistration.CheckUsersModuleAsync);
        Add(checks, "accounts", AccountsModuleRegistration.CheckAccountsModuleAsync);
        Add(checks, "transactions", TransactionsModuleRegistration.CheckTransactionsModuleAsync);
        return s;
    }

    public static IEndpointRouteBuilder MapAtmHealth(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteAsync });
        return app;
    }

    private static void Add(IHealthChecksBuilder checks, string name, Func<IServiceProvider, CancellationToken, Task> probe) =>
        checks.Add(new HealthCheckRegistration(name,
            sp => new ModuleHealthCheck(sp.GetRequiredService<IServiceScopeFactory>(), probe),
            HealthStatus.Unhealthy, tags: null));

    private static async Task PingDatabaseAsync(IServiceProvider scoped, CancellationToken ct)
    {
        await using var command = scoped.GetRequiredService<SqliteConnection>().CreateCommand();
        command.CommandText = "SELECT 1";
        await command.ExecuteScalarAsync(ct);
    }

    private static Task WriteAsync(HttpContext context, HealthReport report)
    {
        // Status only: failure details go to the log, not to anonymous callers.
        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
        };
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
