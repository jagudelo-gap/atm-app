using Atm.Common.Cqrs;
using Atm.Transactions.Contract;
using Atm.Transactions.Slices.Clear;
using Atm.Transactions.Slices.Deposit;
using Atm.Transactions.Slices.GetHistory;
using Atm.Transactions.Slices.Transfer;
using Atm.Transactions.Slices.Withdraw;

namespace Atm.Transactions;

internal class TransactionsModule(IDispatcher dispatcher) : ITransactionsModule
{
    public Task<Result<OperationResult>> DepositAsync(Guid userId, Guid accountId, decimal amount, CancellationToken ct = default) =>
        dispatcher.SendAsync(new DepositCommand(userId, accountId, amount), ct);

    public Task<Result<OperationResult>> WithdrawAsync(Guid userId, Guid accountId, decimal amount, CancellationToken ct = default) =>
        dispatcher.SendAsync(new WithdrawCommand(userId, accountId, amount), ct);

    public Task<Result<OperationResult>> TransferAsync(Guid userId, Guid fromAccountId, Guid toAccountId, decimal amount, CancellationToken ct = default) =>
        dispatcher.SendAsync(new TransferCommand(userId, fromAccountId, toAccountId, amount), ct);

    public async Task<IReadOnlyList<TransactionDto>> GetHistoryAsync(Guid userId, CancellationToken ct = default) =>
        (await dispatcher.QueryAsync(new GetHistoryQuery(userId), ct)).Value;

    public Task ClearAsync(CancellationToken ct = default) =>
        dispatcher.SendAsync(new ClearCommand(), ct);
}
