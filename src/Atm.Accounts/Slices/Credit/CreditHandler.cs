using Atm.Accounts.Contract;
using Atm.Accounts.Data;
using Atm.Common.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Atm.Accounts.Slices.Credit;

internal class CreditHandler(AccountsDbContext db) : IHandler<CreditCommand, AccountDto>
{
    public async Task<Result<AccountDto>> HandleAsync(CreditCommand request, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId, ct);
        if (account is null) return Result<AccountDto>.Fail(new Error(ErrorKind.Business, "Account not found"));

        var dto = account.Credit(request.Amount);
        await db.SaveChangesAsync(ct);
        return Result<AccountDto>.Ok(dto);
    }
}
