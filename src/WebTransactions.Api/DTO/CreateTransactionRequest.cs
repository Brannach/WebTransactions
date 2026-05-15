using System.ComponentModel.DataAnnotations;

namespace WebTransactions.Api.DTO;

/// <summary>
/// Represents the data required to create a new purchase transaction
/// Description must not exceed 50 characters and amount must be USD value between $0.01 and $9,999,999,999.99 
/// </summary>
public class CreateTransactionRequest
{
    [Required]
    [MaxLength(50)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateOnly TransactionDate { get; set; }

    [Required]
    [Range(0.01, 9999999999.99, ErrorMessage = "Amount must be between $0.01 and $9,999,999,999.99.")]
    public decimal Amount { get; set; }
}