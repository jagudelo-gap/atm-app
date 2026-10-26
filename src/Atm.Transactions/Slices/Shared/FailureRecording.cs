using Atm.Common.Cqrs;
using Atm.Common.Data;
using Atm.Transactions.Data;
using Atm.Transactions.Domain;

namespace Atm.Transactions.Slices.Shared;

internal static class FailureRecording
{
    /// <summary>Must equal Accounts' Account.InsufficientFunds message (internal to Atm.Accounts).</summary>
    public const string InsufficientFundsMessage = "Account doesn't have enough funds";

    /// <summary>
    /// After the main unit of work rolled back, persists a Failed row in its own unit of work so it survives.
    /// Only the insufficient-funds error is recorded; any other failure is left alone.
    /// </summary>
    public static async Task RecordFundsFailureAsync<T>(
        IUnitOfWork unitOfWork, TransactionsDbContext db, Result<T> result,
        Func<string, TransactionRecord> failedRecord, CancellationToken ct)
    {
        if (result.IsSuccess) return;
        var funds = result.Errors.FirstOrDefault(e =>
            e.Kind == ErrorKind.Business && e.Message == InsufficientFundsMessage);
        if (funds is null) return;

        await unitOfWork.ExecuteAsync(async () =>
        {
            db.Transactions.Add(failedRecord(funds.Message));
            await db.SaveChangesAsync(ct);
            return Result<bool>.Ok(true);
        }, ct);
    }
}
