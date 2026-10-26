using Atm.Common.Cqrs;

namespace Atm.Accounts.Slices.ResetToSeed;

internal record ResetToSeedCommand(Guid UserId) : ICommand<bool>;
