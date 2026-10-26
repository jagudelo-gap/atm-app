using Atm.Common.Cqrs;

namespace Atm.Transactions.Slices.Clear;

internal record ClearCommand : ICommand<bool>;
