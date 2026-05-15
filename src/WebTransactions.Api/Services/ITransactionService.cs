using WebTransactions.Api.DTO;
using WebTransactions.Api.Models;

namespace WebTransactions.Api.Services;
/// <summary>
/// Defines contract methods for managing purchase transactions
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Persists a new purchase transaction to the database
    /// </summary>
    /// <param name="request">The transaction details, including description, date, and USD amount</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The unique identifier assigned to the newly created transaction</returns>
    Task<Guid> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a transaction by ID and converts its amount to the specified currency
    /// Uses the Treasury Reporting Rates of Exchange API to find the exchange rate
    /// within 6 months prior to the transaction date
    /// </summary>
    /// <param name="id">The unique identifier of the transaction.</param>
    /// <param name="currency">The target currency in Treasury API format, e.g. "Tunisia-Dinar".</param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// The transaction with converted amount, or <c>null</c> if the transaction does not exist
    /// or no exchange rate is available within 6 months of the transaction date
    /// </returns>
    Task<TransactionResponse?> GetTransactionAsync(Guid id, string currency, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all stored purchased transactions
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A list of all transactions ordered by insertion</returns>
    Task<List<Transaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default);
}