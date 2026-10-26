using Atm.Accounts.Contract;
using Atm.Transactions.Contract;

namespace Atm.Api.Endpoints;

/// <summary>Request and response shapes of the HTTP API (serialized camelCase).</summary>
public record AmountRequest(Guid AccountId, decimal Amount);

public record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount);

public record ResetRequest(Guid UserId);

public record AccountResponse(Guid Id, string Type, decimal Balance)
{
    public static AccountResponse From(AccountDto a) => new(a.Id, a.Type, a.Balance);
}

public record OperationResponse(Guid TransactionId, string Type, IReadOnlyList<AccountResponse> Balances)
{
    public static OperationResponse From(OperationResult r) =>
        new(r.TransactionId, r.Type, r.Balances.Select(AccountResponse.From).ToList());
}
