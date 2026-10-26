using Atm.Accounts.Contract;
using Atm.Common.Cqrs;

namespace Atm.Accounts.Slices.Debit;

internal record DebitCommand(Guid AccountId, decimal Amount) : ICommand<AccountDto>;
