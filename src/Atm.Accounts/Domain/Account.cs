using Atm.Accounts.Contract;
using Atm.Common.Cqrs;

namespace Atm.Accounts.Domain;

internal class Account
{
    public const string Checking = "Checking";
    public const string Savings = "Savings";
    public const decimal CheckingSeed = 1500.00m;
    public const decimal SavingsSeed = 5000.00m;

    public static readonly Error InsufficientFunds = new(ErrorKind.Business, "Account doesn't have enough funds");

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = "";
    public decimal Balance { get; set; }

    /// <summary>Optimistic concurrency token, incremented on every change.</summary>
    public int Version { get; set; }

    public Result<AccountDto> Debit(decimal amount)
    {
        if (amount > Balance) return Result<AccountDto>.Fail(InsufficientFunds);
        Balance -= amount;
        Version++;
        return Result<AccountDto>.Ok(ToDto());
    }

    public AccountDto Credit(decimal amount)
    {
        Balance += amount;
        Version++;
        return ToDto();
    }

    public void ResetTo(decimal balance)
    {
        Balance = balance;
        Version++;
    }

    public AccountDto ToDto() => new(Id, UserId, Type, Balance);
}
