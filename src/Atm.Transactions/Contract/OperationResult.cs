using Atm.Accounts.Contract;

namespace Atm.Transactions.Contract;

public record OperationResult(Guid TransactionId, string Type, IReadOnlyList<AccountDto> Balances);
