namespace WebTransactions.Api.DTO;

/// <summary>
/// Represents a purchase transaction retrieved from the database,
/// including the converted amount in the requested target currency.
/// </summary>
public class TransactionResponse
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public decimal OriginalAmountUsd { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal ConvertedAmount { get; set; }
}