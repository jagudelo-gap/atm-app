using Atm.Common.Cqrs;
using Atm.Transactions.Contract;

namespace Atm.Transactions.Slices.Deposit;

internal record DepositCommand(Guid UserId, Guid AccountId, decimal Amount) : ICommand<OperationResult>;
