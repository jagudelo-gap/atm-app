using Atm.Common.Cqrs;
using Atm.Transactions.Data;
using Microsoft.EntityFrameworkCore;

namespace Atm.Transactions.Slices.Clear;

internal class ClearHandler(TransactionsDbContext db) : IHandler<ClearCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(ClearCommand request, CancellationToken ct)
    {
        await db.Transactions.ExecuteDeleteAsync(ct);
        return Result<bool>.Ok(true);
    }
}
