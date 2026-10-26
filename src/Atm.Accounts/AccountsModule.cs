using Atm.Accounts.Contract;
using Atm.Accounts.Slices.Credit;
using Atm.Accounts.Slices.Debit;
using Atm.Accounts.Slices.GetAccounts;
using Atm.Accounts.Slices.ResetToSeed;
using Atm.Common.Cqrs;

namespace Atm.Accounts;

internal class AccountsModule(IDispatcher dispatcher) : IAccountsModule
{
    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(Guid userId, CancellationToken ct = default) =>
        (await dispatcher.QueryAsync(new GetAccountsQuery(userId), ct)).Value;

    public Task<Result<AccountDto>> DebitAsync(Guid accountId, decimal amount, CancellationToken ct = default) =>
        dispatcher.SendAsync(new DebitCommand(accountId, amount), ct);

    public Task<Result<AccountDto>> CreditAsync(Guid accountId, decimal amount, CancellationToken ct = default) =>
        dispatcher.SendAsync(new CreditCommand(accountId, amount), ct);

    public async Task ResetToSeedAsync(Guid userId, CancellationToken ct = default) =>
        await dispatcher.SendAsync(new ResetToSeedCommand(userId), ct);
}
