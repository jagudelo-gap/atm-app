using Atm.Accounts.Contract;
using Atm.Transactions.Slices.Shared;
using FluentValidation;

namespace Atm.Transactions.Slices.Withdraw;

internal class WithdrawValidator : AbstractValidator<WithdrawCommand>
{
    public WithdrawValidator(IAccountsModule accounts)
    {
        RuleFor(c => c.Amount).ValidAmount()
            .Must(a => decimal.Truncate(a) == a).WithMessage("Amount must be a whole dollar amount");
        RuleFor(c => c.AccountId).OwnedBy(accounts, c => c.UserId);
    }
}
