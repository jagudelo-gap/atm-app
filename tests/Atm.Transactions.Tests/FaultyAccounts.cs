using Atm.Accounts.Contract;
using Atm.Common.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Transactions.Tests;

/// <summary>IAccountsModule decorator whose Debit and/or Credit return a non-funds Business error.</summary>
internal sealed class FaultyAccounts(IAccountsModule inner, bool failDebit, bool failCredit) : IAccountsModule
{
    public static readonly Error NotFound = new(ErrorKind.Business, "Account not found");

    public static Action<IServiceCollection> Replace(bool failDebit, bool failCredit) => services =>
    {
        var original = services.Last(d => d.ServiceType == typeof(IAccountsModule));
        services.Remove(original);
        services.AddScoped<IAccountsModule>(sp => new FaultyAccounts(
            (IAccountsModule)ActivatorUtilities.CreateInstance(sp, original.ImplementationType!), failDebit, failCredit));
    };

    public Task<IReadOnlyList<AccountDto>> GetAccountsAsync(Guid userId, CancellationToken ct = default) => inner.GetAccountsAsync(userId, ct);
    public Task<Result<AccountDto>> DebitAsync(Guid accountId, decimal amount, CancellationToken ct = default) =>
        failDebit ? Task.FromResult(Result<AccountDto>.Fail(NotFound)) : inner.DebitAsync(accountId, amount, ct);
    public Task<Result<AccountDto>> CreditAsync(Guid accountId, decimal amount, CancellationToken ct = default) =>
        failCredit ? Task.FromResult(Result<AccountDto>.Fail(NotFound)) : inner.CreditAsync(accountId, amount, ct);
    public Task ResetToSeedAsync(Guid userId, CancellationToken ct = default) => inner.ResetToSeedAsync(userId, ct);
}
