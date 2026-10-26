namespace Atm.Transactions.Domain;

internal class TransactionRecord
{
    public const string DepositType = "Deposit";
    public const string WithdrawType = "Withdraw";
    public const string TransferType = "Transfer";
    public const string Completed = "Completed";
    public const string Failed = "Failed";

    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? ToAccountId { get; set; }
    public string Type { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
