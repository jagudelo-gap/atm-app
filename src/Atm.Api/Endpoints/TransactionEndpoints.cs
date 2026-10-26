using Atm.Api.Errors;
using Atm.Transactions.Contract;

namespace Atm.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var tx = app.MapGroup("/api/users/{userId}/transactions");

        tx.MapGet("",async (Guid userId, ITransactionsModule module, CancellationToken ct) =>
            Results.Ok(await module.GetHistoryAsync(userId, ct)));

        tx.MapPost("/deposit", async (Guid userId, AmountRequest body, ITransactionsModule module, CancellationToken ct) =>
            (await module.DepositAsync(userId, body.AccountId, body.Amount, ct)).ToHttp(OperationResponse.From));

        tx.MapPost("/withdraw", async (Guid userId, AmountRequest body, ITransactionsModule module, CancellationToken ct) =>
            (await module.WithdrawAsync(userId, body.AccountId, body.Amount, ct)).ToHttp(OperationResponse.From));

        tx.MapPost("/transfer", async (Guid userId, TransferRequest body, ITransactionsModule module, CancellationToken ct) =>
            (await module.TransferAsync(userId, body.FromAccountId, body.ToAccountId, body.Amount, ct))
                .ToHttp(OperationResponse.From));

        return app;
    }
}
