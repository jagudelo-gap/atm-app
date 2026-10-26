using Atm.Common.Cqrs;
using Atm.Users.Contract;
using Atm.Users.Data;
using Atm.Users.Domain;
using Atm.Users.Slices.GetUserByName;
using Microsoft.EntityFrameworkCore;

namespace Atm.Users;

internal class UsersModule(IDispatcher dispatcher, UsersDbContext db) : IUsersModule
{
    public Task<Result<UserDto>> GetByNameAsync(string name, CancellationToken ct = default) =>
        dispatcher.QueryAsync(new GetUserByNameQuery(name), ct);

    public async Task<UserDto> EnsureSeededAsync(string name, CancellationToken ct = default)
    {
        var existing = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Name == name, ct);
        if (existing is not null) return new UserDto(existing.Id, existing.Name);

        var user = new User { Id = Guid.NewGuid(), Name = name };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return new UserDto(user.Id, user.Name);
    }
}
