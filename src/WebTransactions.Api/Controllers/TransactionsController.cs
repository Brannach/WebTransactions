using Microsoft.AspNetCore.Mvc;
using WebTransactions.Api.DTO;
using WebTransactions.Api.Models;
using WebTransactions.Api.Services;

namespace WebTransactions.Api.Controllers;

/// <summary>
/// Handles HTTP requests for purchase transaction operations.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>
    /// Stores a new purchase transaction
    /// </summary>
    /// <param name="request">The transaction details: description, date, and USD amount</param>
    /// <param name="cancellationToken"></param>
    /// <returns>HTTP 201 with the new transaction ID, or HTTP 400 if validation fails</returns>
    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        Guid id = await _transactionService.CreateTransactionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTransaction), new { id }, new { id });
    }

    /// <summary>
    /// Retrieves a transaction by ID and converts its amount to the specified currency
    /// </summary>
    /// <param name="id">The unique identifier of the transaction</param>
    /// <param name="currency">The target currency in Treasury API format, e.g. "Yemen-Rial".</param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// HTTP 200 with the transaction and converted amount,
    /// or HTTP 404 if the transaction does not exist or no exchange rate is available
    /// </returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransaction(Guid id, [FromQuery] string currency, CancellationToken cancellationToken = default)
    {
        TransactionResponse? response = await _transactionService.GetTransactionAsync(id, currency, cancellationToken);

        if (response is null)
            return NotFound(new { error = "Transaction not found or no exchange rate available within 6 months of the transaction date." });

        return Ok(response);
    }
    
    /// <summary>
    /// Retrieves all persisted transactions
    /// </summary>
    /// <returns>HTTP 200 with a list of all transactions</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllTransactions(CancellationToken cancellationToken = default)
    {
        List<Transaction> transactions = await _transactionService.GetAllTransactionsAsync(cancellationToken);
        return Ok(transactions);
    }
}