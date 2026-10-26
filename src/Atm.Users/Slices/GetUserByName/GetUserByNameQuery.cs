using Atm.Common.Cqrs;
using Atm.Users.Contract;

namespace Atm.Users.Slices.GetUserByName;

internal record GetUserByNameQuery(string Name) : IQuery<UserDto>;
