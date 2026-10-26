using Atm.Accounts.Contract;
using Atm.Transactions.Slices.Shared;
using FluentValidation;

namespace Atm.Transactions.Slices.Deposit;

internal class DepositValidator : AbstractValidator<DepositCommand>
{
    public const decimal MaxAmount = CommandRules.MaxAmount;

    public DepositValidator(IAccountsModule accounts)
    {
        RuleFor(c => c.Amount).ValidAmount();
        RuleFor(c => c.AccountId).OwnedBy(accounts, c => c.UserId);
    }
}
