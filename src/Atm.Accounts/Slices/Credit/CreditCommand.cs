using Atm.Accounts.Contract;
using Atm.Common.Cqrs;

namespace Atm.Accounts.Slices.Credit;

internal record CreditCommand(Guid AccountId, decimal Amount) : ICommand<AccountDto>;
