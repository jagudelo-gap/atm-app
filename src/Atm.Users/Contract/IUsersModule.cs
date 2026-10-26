using Atm.Common.Cqrs;

namespace Atm.Users.Contract;

public interface IUsersModule
{
    Task<Result<UserDto>> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Returns the existing user or creates it.</summary>
    Task<UserDto> EnsureSeededAsync(string name, CancellationToken ct = default);
}
