namespace Atm.Accounts.Contract;

public record AccountDto(Guid Id, Guid UserId, string Type, decimal Balance);
