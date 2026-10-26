using Atm.Common.Cqrs;

namespace Atm.Accounts.Contract;

public interface IAccountsModule
{
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(Guid userId, CancellationToken ct = default);

    Task<Result<AccountDto>> DebitAsync(Guid accountId, decimal amount, CancellationToken ct = default);

    Task<Result<AccountDto>> CreditAsync(Guid accountId, decimal amount, CancellationToken ct = default);

    /// <summary>Creates Checking (1,500.00) and Savings (5,000.00) for the user if missing, else resets balances.</summary>
    Task ResetToSeedAsync(Guid userId, CancellationToken ct = default);
}
