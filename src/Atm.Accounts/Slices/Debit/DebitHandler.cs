using Atm.Accounts.Contract;
using Atm.Accounts.Data;
using Atm.Common.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Atm.Accounts.Slices.Debit;

internal class DebitHandler(AccountsDbContext db) : IHandler<DebitCommand, AccountDto>
{
    public async Task<Result<AccountDto>> HandleAsync(DebitCommand request, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId, ct);
        if (account is null) return Result<AccountDto>.Fail(new Error(ErrorKind.Business, "Account not found"));

        var result = account.Debit(request.Amount);
        if (result.IsSuccess) await db.SaveChangesAsync(ct);
        return result;
    }
}
