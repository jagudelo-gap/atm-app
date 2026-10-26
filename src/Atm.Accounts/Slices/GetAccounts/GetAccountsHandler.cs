using Atm.Accounts.Contract;
using Atm.Accounts.Data;
using Atm.Common.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Atm.Accounts.Slices.GetAccounts;

internal class GetAccountsHandler(AccountsDbContext db) : IHandler<GetAccountsQuery, IReadOnlyList<AccountDto>>
{
    public async Task<Result<IReadOnlyList<AccountDto>>> HandleAsync(GetAccountsQuery request, CancellationToken ct)
    {
        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => a.UserId == request.UserId)
            .OrderBy(a => a.Type)
            .Select(a => new AccountDto(a.Id, a.UserId, a.Type, a.Balance))
            .ToListAsync(ct);
        return Result<IReadOnlyList<AccountDto>>.Ok(accounts);
    }
}
