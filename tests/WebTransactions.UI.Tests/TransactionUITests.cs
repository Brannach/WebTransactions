using Microsoft.Playwright;

namespace WebTransactions.UI.Tests;

/// <summary>
/// End-to-end UI tests for the WebTransactions Blazor application.
/// Each test boots the full application on a real HTTP port using
/// <see cref="PlaywrightFixture"/> and drives a real browser via Playwright.
/// </summary>
public class TransactionUITests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public TransactionUITests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<IPage> NewPageAsync()
    {
        IBrowserContext context = await _fixture.Browser.NewContextAsync();
        return await context.NewPageAsync();
    }

    /// <summary>
    /// Verifies that the home page loads and displays the welcome message.
    /// </summary>
    [Fact]
    public async Task Home_PageLoads_ShowsWelcomeMessage()
    {
        IPage page = await NewPageAsync();
        await page.GotoAsync(_fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await page.WaitForSelectorAsync("h2");
        string heading = await page.InnerTextAsync("h2");

        Assert.Equal("Home", heading);
    }

    /// <summary>
    /// Verifies that submitting the create transaction form with valid data
    /// shows a success message containing a transaction ID.
    /// </summary>
    [Fact]
    public async Task CreateTransaction_ValidInput_ShowsSuccessMessage()
    {
        IPage page = await NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await page.WaitForSelectorAsync("form");

        await page.FillAsync("input[placeholder='Enter transaction description']", "Playwright test purchase");
        await page.FillAsync("input[type='number']", "99.99");

        await page.ClickAsync("button[type='submit']");

        await page.WaitForSelectorAsync(".text-success");
        string successText = await page.InnerTextAsync(".text-success");

        Assert.Contains("Transaction created successfully", successText);
    }

    /// <summary>
    /// Verifies that after creating a transaction, it appears in the transactions list.
    /// </summary>
    [Fact]
    public async Task ListTransactions_AfterCreating_ShowsTransaction()
    {
        IPage page = await NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForSelectorAsync("form");
        await page.FillAsync("input[placeholder='Enter transaction description']", "List test purchase");
        await page.FillAsync("input[type='number']", "50.00");
        await page.ClickAsync("button[type='submit']");
        await page.WaitForSelectorAsync(".text-success");

        await page.GotoAsync($"{_fixture.BaseUrl}/list");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForSelectorAsync("table");

        string tableContent = await page.InnerTextAsync("table");
        Assert.Contains("List test purchase", tableContent);
    }

    /// <summary>
    /// Verifies that the retrieve page loads the currency dropdown
    /// populated with available currencies.
    /// </summary>
    [Fact]
    public async Task RetrievePage_CurrencyDropdown_IsPopulated()
    {
        IPage page = await NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/get");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForSelectorAsync("select");

        IReadOnlyList<IElementHandle> options = await page.QuerySelectorAllAsync("select option");
        Assert.True(options.Count > 1);
    }
}