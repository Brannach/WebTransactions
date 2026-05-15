using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WebTransactions.Api.DTO;
using WebTransactions.Api.Services;

namespace WebTransactions.Api.Tests;

/// <summary>
/// Functional tests for the Transactions API endpoints
/// Each test boots the full application in memory using <see cref="CustomWebApplicationFactory"/>,
/// which substitutes an in-memory SQLite database and a <see cref="FakeExchangeRateService"/>
/// returning a fixed exchange rate of 1.08, avoiding any real external API calls
/// </summary>
public class TransactionApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    
    public TransactionApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Verifies a valid POST request creates a transaction and returns HTTP 201
    /// with a response body containing the new transaction ID
    /// </summary>
    [Fact]
    public async Task CreateTransaction_ValidRequest_Returns201WithId()
    {
        CreateTransactionRequest request = new CreateTransactionRequest
        {
            Description = "Test purchase",
            TransactionDate = new DateOnly(2024, 1, 15),
            Amount = 100.50m
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/transactions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("id", out JsonElement idElement));
        Assert.True(Guid.TryParse(idElement.GetString(), out _));
    }

    /// <summary>
    /// Verifies that a POST request with a description exceeding 50 characters
    /// is rejected with HTTP 400 Bad Request
    /// Uses ASP.NET Core's automatic model validation.
    /// </summary>
    [Fact]
    public async Task CreateTransaction_DescriptionTooLong_Returns400()
    {
        CreateTransactionRequest request = new CreateTransactionRequest
        {
            Description = new string('A', 51),
            TransactionDate = new DateOnly(2024, 1, 15),
            Amount = 100.50m
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/transactions", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateTransaction_DescriptionExactly50Chars_Returns201()
    {
        CreateTransactionRequest request = new CreateTransactionRequest
        {
            Description = new string('A', 50),
            TransactionDate = new DateOnly(2024, 1, 15),
            Amount = 100.00m
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/transactions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Verifies a POST request with a negative amount is rejected with HTTP 400 Bad Request
    /// The <see cref="System.ComponentModel.DataAnnotations.RangeAttribute"/> on
    /// <see cref="WebTransactions.Api.DTOs.CreateTransactionRequest.Amount"/> enforces
    /// that the amount must be a positive value
    /// </summary>
    [Fact]
    public async Task CreateTransaction_NegativeAmount_Returns400()
    {
        CreateTransactionRequest request = new CreateTransactionRequest
        {
            Description = "Test purchase",
            TransactionDate = new DateOnly(2024, 1, 15),
            Amount = -10m
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/transactions", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateTransaction_ZeroAmount_Returns400()
    {
        CreateTransactionRequest request = new CreateTransactionRequest
        {
            Description = "Test purchase",
            TransactionDate = new DateOnly(2024, 1, 15),
            Amount = 0m
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/transactions", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateTransaction_AmountTooLarge_Returns400()
    {
        CreateTransactionRequest request = new CreateTransactionRequest
        {
            Description = "Test purchase",
            TransactionDate = new DateOnly(2024, 1, 15),
            Amount = 10000000000.00m
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/transactions", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateTransaction_AmountWithExcessDecimals_IsRoundedToNearestCent()
    {
        CreateTransactionRequest createRequest = new CreateTransactionRequest
        {
            Description = "Rounding test",
            TransactionDate = new DateOnly(2024, 1, 15),
            Amount = 100.999m
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", createRequest);
        JsonElement created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        string id = created.GetProperty("id").GetString()!;

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/transactions/{id}?currency=Canada-Dollar");
        TransactionResponse? transaction = await getResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        Assert.NotNull(transaction);
        Assert.Equal(101.00m, transaction.OriginalAmountUsd);
    }

    [Fact]
    public async Task GetTransaction_ExistingTransaction_Returns200WithAllCorrectFields()
    {
        CreateTransactionRequest createRequest = new CreateTransactionRequest
        {
            Description = "Full fields test",
            TransactionDate = new DateOnly(2024, 1, 15),
            Amount = 100.00m
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", createRequest);
        JsonElement created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        string id = created.GetProperty("id").GetString()!;

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/transactions/{id}?currency=Canada-Dollar");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        TransactionResponse? transaction = await getResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(transaction);
        Assert.Equal(Guid.Parse(id), transaction.Id);
        Assert.Equal("Full fields test", transaction.Description);
        Assert.Equal(new DateOnly(2024, 1, 15), transaction.TransactionDate);
        Assert.Equal(100.00m, transaction.OriginalAmountUsd);
        Assert.Equal(1.08m, transaction.ExchangeRate);
        Assert.Equal(108.00m, transaction.ConvertedAmount);
    }
    
    [Fact]
    public async Task GetTransaction_NoExchangeRateAvailable_Returns404()
    {
        HttpClient clientWithNullRate = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IExchangeRateService));
                if (descriptor is not null)
                    services.Remove(descriptor);
                services.AddScoped<IExchangeRateService>(_ => new FakeExchangeRateService(null));
            });
        }).CreateClient();

        CreateTransactionRequest createRequest = new CreateTransactionRequest
        {
            Description = "No rate test",
            TransactionDate = new DateOnly(2024, 1, 15),
            Amount = 100.00m
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", createRequest);
        JsonElement created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        string id = created.GetProperty("id").GetString()!;

        HttpResponseMessage getResponse = await clientWithNullRate.GetAsync($"/api/v1/transactions/{id}?currency=Canada-Dollar");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>
    /// Verifies that requesting a transaction with a non-existent ID returns HTTP 404 Not Found
    /// A freshly generated <see cref="Guid"/> is used to guarantee the ID is not stored
    /// </summary>
    [Fact]
    public async Task GetTransaction_NonExistentId_Returns404()
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/transactions/{Guid.NewGuid()}?currency=Euro Zone-Euro");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllTransactions_ReturnsListContainingCreatedTransaction()
    {
        CreateTransactionRequest createRequest = new CreateTransactionRequest
        {
            Description = "List inclusion test",
            TransactionDate = new DateOnly(2024, 1, 15),
            Amount = 75.00m
        };

        await _client.PostAsJsonAsync("/api/v1/transactions", createRequest);

        HttpResponseMessage response = await _client.GetAsync("/api/v1/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<JsonElement>? transactions = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.NotNull(transactions);
        Assert.True(transactions.Count > 0);
        Assert.Contains(transactions, t =>
            t.GetProperty("description").GetString() == "List inclusion test");
    }
}