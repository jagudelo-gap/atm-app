using Atm.Common.Cqrs;

namespace Atm.Transactions.Contract;

public interface ITransactionsModule
{
    Task<Result<OperationResult>> DepositAsync(Guid userId, Guid accountId, decimal amount, CancellationToken ct = default);
    Task<Result<OperationResult>> WithdrawAsync(Guid userId, Guid accountId, decimal amount, CancellationToken ct = default);
    Task<Result<OperationResult>> TransferAsync(Guid userId, Guid fromAccountId, Guid toAccountId, decimal amount, CancellationToken ct = default);
    Task<IReadOnlyList<TransactionDto>> GetHistoryAsync(Guid userId, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}
