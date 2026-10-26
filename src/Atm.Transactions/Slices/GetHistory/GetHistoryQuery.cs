using Atm.Common.Cqrs;
using Atm.Transactions.Contract;

namespace Atm.Transactions.Slices.GetHistory;

internal record GetHistoryQuery(Guid UserId) : IQuery<IReadOnlyList<TransactionDto>>;
