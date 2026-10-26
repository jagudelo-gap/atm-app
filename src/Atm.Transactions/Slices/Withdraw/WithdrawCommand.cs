using Atm.Common.Cqrs;
using Atm.Transactions.Contract;

namespace Atm.Transactions.Slices.Withdraw;

internal record WithdrawCommand(Guid UserId, Guid AccountId, decimal Amount) : ICommand<OperationResult>;
