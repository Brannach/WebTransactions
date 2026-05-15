using WebTransactions.Api.Services;

namespace WebTransactions.Api.Tests;

/// <summary>
/// Test mock for <see cref="IExchangeRateService"/> that avoids real HTTP calls
/// to the Treasury API. Returns a fixed configurable exchange rate and a hardcoded
/// list of currencies, ensuring tests are deterministic and network-independent
/// </summary>
public class FakeExchangeRateService : IExchangeRateService
{
    private readonly decimal? _rate;

    public FakeExchangeRateService(decimal? rate = 1.08m)
    {
        _rate = rate;
    }

    public Task<decimal?> GetExchangeRateAsync(string currency, DateOnly transactionDate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_rate);
    }

    public Task<List<string>> GetAvailableCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        List<string> currencies = new List<string>
        {
            "Afghanistan-Afghani",
            "Albania-Lek",
            "Algeria-Dinar",
            "Angola-Kwanza",
            "Argentina-Peso",
            "Australia-Dollar",
            "Bahrain-Dinar",
            "Brazil-Real",
            "Canada-Dollar",
            "Chile-Peso",
            "China-Renminbi",
            "Colombia-Peso",
            "Czech Republic-Koruna",
            "Denmark-Krone",
            "Egypt-Pound",
            "Ethiopia-Birr",
            "Euro Zone-Euro",
            "Ghana-Cedi",
            "Hong Kong-Dollar",
            "Hungary-Forint",
            "India-Rupee",
            "Indonesia-Rupiah",
            "Iraq-Dinar",
            "Israel-Shekel",
            "Japan-Yen",
            "Jordan-Dinar",
            "Kazakhstan-Tenge",
            "Kenya-Shilling",
            "Kuwait-Dinar",
            "Lebanon-Pound",
            "Libya-Dinar",
            "Malaysia-Ringgit",
            "Mexico-Peso",
            "Morocco-Dirham",
            "New Zealand-Dollar",
            "Nigeria-Naira",
            "Norway-Krone",
            "Oman-Rial",
            "Pakistan-Rupee",
            "Peru-Sol",
            "Philippines-Peso",
            "Poland-Zloty",
            "Qatar-Riyal",
            "Romania-Leu",
            "Russia-Ruble",
            "Saudi Arabia-Riyal",
            "Singapore-Dollar",
            "South Africa-Rand",
            "South Korea-Won",
            "Sri Lanka-Rupee",
            "Sweden-Krona",
            "Switzerland-Franc",
            "Taiwan-Dollar",
            "Thailand-Baht",
            "Tunisia-Dinar",
            "Turkey-Lira",
            "Uganda-Shilling",
            "Ukraine-Hryvnia",
            "United Arab Emirates-Dirham",
            "United Kingdom-Pound",
            "Uruguay-Peso",
            "Vietnam-Dong",
            "Zambia-Kwacha"
        };

        return Task.FromResult(currencies);
    }
}