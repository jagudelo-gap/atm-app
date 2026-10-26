using Atm.Accounts.Contract;
using Atm.Common.Cqrs;
using Atm.Common.Data;
using Atm.Transactions.Contract;
using Atm.Transactions.Data;
using Atm.Transactions.Domain;

namespace Atm.Transactions.Slices.Deposit;

internal class DepositHandler(IUnitOfWork unitOfWork, IAccountsModule accounts, TransactionsDbContext db)
    : IHandler<DepositCommand, OperationResult>
{
    public Task<Result<OperationResult>> HandleAsync(DepositCommand request, CancellationToken ct) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var credit = await accounts.CreditAsync(request.AccountId, request.Amount, ct);
            if (!credit.IsSuccess) return Result<OperationResult>.Fail(credit.Errors.ToArray());

            var record = new TransactionRecord
            {
                Id = Guid.NewGuid(),
                AccountId = request.AccountId,
                Type = TransactionRecord.DepositType,
                Amount = request.Amount,
                Status = TransactionRecord.Completed,
                CreatedAt = DateTime.UtcNow,
            };
            db.Transactions.Add(record);
            await db.SaveChangesAsync(ct);

            var balances = await accounts.GetAccountsAsync(request.UserId, ct);
            return Result<OperationResult>.Ok(new OperationResult(record.Id, record.Type, balances));
        }, ct);
}
