using Atm.Accounts.Contract;
using Atm.Common.Cqrs;
using Atm.Common.Data;
using Atm.Transactions.Contract;
using Atm.Transactions.Data;
using Atm.Transactions.Domain;
using Atm.Transactions.Slices.Shared;

namespace Atm.Transactions.Slices.Withdraw;

internal class WithdrawHandler(IUnitOfWork unitOfWork, IAccountsModule accounts, TransactionsDbContext db)
    : IHandler<WithdrawCommand, OperationResult>
{
    public async Task<Result<OperationResult>> HandleAsync(WithdrawCommand request, CancellationToken ct)
    {
        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            var debit = await accounts.DebitAsync(request.AccountId, request.Amount, ct);
            if (!debit.IsSuccess) return Result<OperationResult>.Fail(debit.Errors.ToArray());

            var record = NewRecord(request, TransactionRecord.Completed, null);
            db.Transactions.Add(record);
            await db.SaveChangesAsync(ct);

            var balances = await accounts.GetAccountsAsync(request.UserId, ct);
            return Result<OperationResult>.Ok(new OperationResult(record.Id, record.Type, balances));
        }, ct);

        await FailureRecording.RecordFundsFailureAsync(unitOfWork, db, result,
            reason => NewRecord(request, TransactionRecord.Failed, reason), ct);

        return result;
    }

    private static TransactionRecord NewRecord(WithdrawCommand request, string status, string? failureReason) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = request.AccountId,
        Type = TransactionRecord.WithdrawType,
        Amount = request.Amount,
        Status = status,
        FailureReason = failureReason,
        CreatedAt = DateTime.UtcNow,
    };
}
