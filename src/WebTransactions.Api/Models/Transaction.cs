namespace WebTransactions.Api.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public decimal Amount { get; set; }
}