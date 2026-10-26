using Atm.Accounts.Contract;
using Atm.Common.Cqrs;
using Atm.Common.Data;
using Atm.Transactions.Contract;
using Atm.Transactions.Data;
using Atm.Transactions.Domain;
using Atm.Transactions.Slices.Shared;

namespace Atm.Transactions.Slices.Transfer;

internal class TransferHandler(IUnitOfWork unitOfWork, IAccountsModule accounts, TransactionsDbContext db)
    : IHandler<TransferCommand, OperationResult>
{
    public async Task<Result<OperationResult>> HandleAsync(TransferCommand request, CancellationToken ct)
    {
        // Debit, credit and the Completed row share one unit of work: any failure or exception rolls all of it back.
        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            var debit = await accounts.DebitAsync(request.FromAccountId, request.Amount, ct);
            if (!debit.IsSuccess) return Result<OperationResult>.Fail(debit.Errors.ToArray());

            var credit = await accounts.CreditAsync(request.ToAccountId, request.Amount, ct);
            if (!credit.IsSuccess) return Result<OperationResult>.Fail(credit.Errors.ToArray());

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

    private static TransactionRecord NewRecord(TransferCommand request, string status, string? failureReason) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = request.FromAccountId,
        ToAccountId = request.ToAccountId,
        Type = TransactionRecord.TransferType,
        Amount = request.Amount,
        Status = status,
        FailureReason = failureReason,
        CreatedAt = DateTime.UtcNow,
    };
}
