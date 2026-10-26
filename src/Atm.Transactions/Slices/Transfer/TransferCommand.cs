using Atm.Common.Cqrs;
using Atm.Transactions.Contract;

namespace Atm.Transactions.Slices.Transfer;

internal record TransferCommand(Guid UserId, Guid FromAccountId, Guid ToAccountId, decimal Amount) : ICommand<OperationResult>;
