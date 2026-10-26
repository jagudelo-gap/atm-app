using Atm.Accounts.Data;
using Atm.Accounts.Domain;
using Atm.Common.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Atm.Accounts.Slices.ResetToSeed;

internal class ResetToSeedHandler(AccountsDbContext db) : IHandler<ResetToSeedCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(ResetToSeedCommand request, CancellationToken ct)
    {
        var existing = await db.Accounts.Where(a => a.UserId == request.UserId).ToListAsync(ct);

        Apply(existing, request.UserId, Account.Checking, Account.CheckingSeed);
        Apply(existing, request.UserId, Account.Savings, Account.SavingsSeed);

        await db.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    private void Apply(List<Account> existing, Guid userId, string type, decimal seed)
    {
        var account = existing.FirstOrDefault(a => a.Type == type);
        if (account is null)
            db.Accounts.Add(new Account { Id = Guid.NewGuid(), UserId = userId, Type = type, Balance = seed });
        else
            account.ResetTo(seed);
    }
}
