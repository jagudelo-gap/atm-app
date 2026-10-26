using Atm.Accounts.Contract;
using Atm.Common.Cqrs;

namespace Atm.Accounts.Slices.GetAccounts;

internal record GetAccountsQuery(Guid UserId) : IQuery<IReadOnlyList<AccountDto>>;
