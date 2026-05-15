using System.Text.Json;
using System.Linq;

namespace WebTransactions.Api.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;

    public ExchangeRateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal?> GetExchangeRateAsync(string currency, DateOnly transactionDate, CancellationToken cancellationToken = default)
    {
        DateOnly sixMonthsBefore = transactionDate.AddMonths(-6);

        string encodedCurrency = Uri.EscapeDataString(currency);

        string url = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange" +
                     $"?fields=country_currency_desc,exchange_rate,record_date" +
                     $"&filter=country_currency_desc:eq:{encodedCurrency},record_date:gte:{sixMonthsBefore:yyyy-MM-dd},record_date:lte:{transactionDate:yyyy-MM-dd}" +
                     $"&sort=-record_date" +
                     $"&page%5Bsize%5D=1";

        HttpResponseMessage response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        string json = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(json);

        JsonElement data = doc.RootElement.GetProperty("data");
        if (data.GetArrayLength() == 0)
            return null;

        string? rateString = data[0].GetProperty("exchange_rate").GetString();
        if (decimal.TryParse(rateString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal rate))
            return rate;

        return null;
    }
    
    public async Task<List<string>> GetAvailableCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        string url = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange" +
                     "?fields=country_currency_desc" +
                     "&filter=record_date:gte:2024-01-01" +
                     "&sort=country_currency_desc" +
                     "&page%5Bsize%5D=300";

        HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new List<string>();

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        using JsonDocument doc = JsonDocument.Parse(json);

        JsonElement data = doc.RootElement.GetProperty("data");
        List<string> currencies = new List<string>();

        foreach (JsonElement item in data.EnumerateArray())
        {
            string? currency = item.GetProperty("country_currency_desc").GetString();
            
            if (currency is not null && !currencies.Contains(currency))
                currencies.Add(currency);
        }
        currencies = currencies
            .Where(c => !c.EndsWith("-Euro") || c == "Euro Zone-Euro")
            .ToList();
        return currencies;
    }
}