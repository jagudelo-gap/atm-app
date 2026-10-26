using Atm.Accounts.Contract;
using Atm.Common.Cqrs;
using Atm.Transactions.Contract;
using Atm.Transactions.Data;
using Microsoft.EntityFrameworkCore;

namespace Atm.Transactions.Slices.GetHistory;

internal class GetHistoryHandler(TransactionsDbContext db, IAccountsModule accounts)
    : IHandler<GetHistoryQuery, IReadOnlyList<TransactionDto>>
{
    public async Task<Result<IReadOnlyList<TransactionDto>>> HandleAsync(GetHistoryQuery request, CancellationToken ct)
    {
        var types = (await accounts.GetAccountsAsync(request.UserId, ct)).ToDictionary(a => a.Id, a => a.Type);
        var ids = types.Keys.ToList();

        // CreatedAt is stored as ISO-8601 TEXT, which sorts chronologically; Id breaks exact ties so the order is stable.
        var rows = await db.Transactions.AsNoTracking()
            .Where(t => ids.Contains(t.AccountId) || (t.ToAccountId != null && ids.Contains(t.ToAccountId.Value)))
            .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
            .ToListAsync(ct);

        // A transfer's other side may belong to another user, so its type is unknown here.
        string TypeOf(Guid id) => types.GetValueOrDefault(id, "");
        var dtos = rows.Select(t => new TransactionDto(
            t.Id, t.Type, t.Status, t.Amount,
            t.AccountId, TypeOf(t.AccountId),
            t.ToAccountId, t.ToAccountId is { } to ? TypeOf(to) : null,
            t.FailureReason, t.CreatedAt)).ToList();
        return Result<IReadOnlyList<TransactionDto>>.Ok(dtos);
    }
}
