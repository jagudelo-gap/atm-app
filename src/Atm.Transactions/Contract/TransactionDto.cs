namespace Atm.Transactions.Contract;

public record TransactionDto(
    Guid Id, string Type, string Status, decimal Amount,
    Guid AccountId, string AccountType, Guid? ToAccountId, string? ToAccountType,
    string? FailureReason, DateTime CreatedAt);
