using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Atm.Api.Health;

/// <summary>Runs a module's trivial query in its own scope (own connection); unhealthy when it throws.</summary>
internal sealed class ModuleHealthCheck(IServiceScopeFactory scopes, Func<IServiceProvider, CancellationToken, Task> probe)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await probe(scope.ServiceProvider, ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
