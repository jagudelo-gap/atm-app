using Atm.Accounts.Contract;
using FluentValidation;

namespace Atm.Transactions.Slices.Shared;

/// <summary>Validation rules common to money-moving commands.</summary>
internal static class CommandRules
{
    public const decimal MaxAmount = 1_000_000.00m;

    public static IRuleBuilderOptions<T, decimal> ValidAmount<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0).WithMessage("Amount must be greater than 0")
            .Must(a => decimal.Round(a, 2) == a).WithMessage("Amount can have at most 2 decimals")
            .LessThanOrEqualTo(MaxAmount).WithMessage("Amount can't be more than 1,000,000.00");

    public static IRuleBuilderOptions<T, Guid> OwnedBy<T>(
        this IRuleBuilder<T, Guid> rule, IAccountsModule accounts, Func<T, Guid> userId) =>
        rule.MustAsync(async (c, accountId, ct) =>
                (await accounts.GetAccountsAsync(userId(c), ct)).Any(a => a.Id == accountId))
            .WithErrorCode("business")
            .WithMessage("Account not found");
}
