using Atm.Accounts.Contract;
using Atm.Transactions.Slices.Shared;
using FluentValidation;

namespace Atm.Transactions.Slices.Transfer;

internal class TransferValidator : AbstractValidator<TransferCommand>
{
    public TransferValidator(IAccountsModule accounts)
    {
        RuleFor(c => c.Amount).ValidAmount();
        RuleFor(c => c.ToAccountId).NotEqual(c => c.FromAccountId)
            .WithMessage("Source and destination accounts must differ");
        RuleFor(c => c.FromAccountId).OwnedBy(accounts, c => c.UserId);
        RuleFor(c => c.ToAccountId).OwnedBy(accounts, c => c.UserId);
    }
}
