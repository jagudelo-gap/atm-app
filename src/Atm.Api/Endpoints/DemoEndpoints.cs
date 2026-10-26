using Atm.Accounts.Contract;
using Atm.Api.Errors;
using Atm.Common.Cqrs;
using Atm.Common.Data;
using Atm.Transactions.Contract;

namespace Atm.Api.Endpoints;

/// <summary>Demo-only endpoints.</summary>
public static class DemoEndpoints
{
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/demo/reset", ResetAsync);
        return app;
    }

    /// <summary>Clears all history and restores the seed balances, in one transaction.</summary>
    private static async Task<IResult> ResetAsync(ResetRequest body, IUnitOfWork unitOfWork,
        IAccountsModule accounts, ITransactionsModule transactions, CancellationToken ct)
    {
        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            // Every seeded user has accounts; refusing unknown ids avoids creating accounts for a missing user.
            if ((await accounts.GetAccountsAsync(body.UserId, ct)).Count == 0)
                return Result<bool>.Fail(new Error(ErrorKind.Business, "User not found"));

            await transactions.ClearAsync(ct);
            await accounts.ResetToSeedAsync(body.UserId, ct);
            return Result<bool>.Ok(true);
        }, ct);

        return result.IsSuccess ? Results.NoContent() : ProblemDetailsMapper.Map(result.Errors);
    }
}
