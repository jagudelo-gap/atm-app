using Atm.Common.Cqrs;
using Atm.Users.Contract;
using Atm.Users.Data;
using Microsoft.EntityFrameworkCore;

namespace Atm.Users.Slices.GetUserByName;

internal class GetUserByNameHandler(UsersDbContext db) : IHandler<GetUserByNameQuery, UserDto>
{
    public async Task<Result<UserDto>> HandleAsync(GetUserByNameQuery request, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Name == request.Name)
            .Select(u => new UserDto(u.Id, u.Name))
            .FirstOrDefaultAsync(ct);

        return user is null
            ? Result<UserDto>.Fail(new Error(ErrorKind.Business, "User not found"))
            : Result<UserDto>.Ok(user);
    }
}
