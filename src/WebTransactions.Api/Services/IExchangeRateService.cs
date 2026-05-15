namespace WebTransactions.Api.Services;

/// <summary>
/// Defines contract methods  for retrieving currency exchange rates
/// from the Treasury Reporting Rates of Exchange API
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Retrieves the exchange rate for the specified currency closest to the transaction date,
    /// within a 6-month period prior to that date
    /// </summary>
    /// <param name="currency">The target currency in Treasury API format, e.g. "Tunisia-Dinar"</param>
    /// <param name="transactionDate">The date of the purchase transaction</param>
    /// <returns>
    /// The exchange rate as a decimal, or <c>null</c> if no rate is available
    /// within 6 months prior to the transaction date
    Task<decimal?> GetExchangeRateAsync(string currency, DateOnly transactionDate, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Retrieves the list of all currencies currently available in the
    /// Treasury Reporting Rates of Exchange API, excluding obsolete historical entries
    /// (e.g. euro-zone countries local currencies)
    /// </summary>
    /// <returns>A sorted list of currency descriptors, e.g. "Tunisia-Dinar", "Euro Zone-Euro"</returns>
    Task<List<string>> GetAvailableCurrenciesAsync(CancellationToken cancellationToken = default);
}