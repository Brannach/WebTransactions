using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using WebTransactions.Api.Components;
using WebTransactions.Api.Data;
using WebTransactions.Api.Services;

namespace WebTransactions.UI.Tests;

public class PlaywrightFixture : IAsyncLifetime
{
    private WebApplication? _app;
    private SqliteConnection? _connection;
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;
    public string BaseUrl => "http://localhost:5200";

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(BaseUrl);

        builder.Services.AddControllers();
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddMvc();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(_connection));
        builder.Services.AddScoped<IExchangeRateService>(_ => new FakeExchangeRateService(1.08m));
        builder.Services.AddScoped<ITransactionService, TransactionService>();

        _app = builder.Build();

        using (IServiceScope scope = _app.Services.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        _app.UseStaticFiles();
        _app.UseAntiforgery();
        _app.MapControllers();
        _app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        await _app.StartAsync();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        _playwright?.Dispose();

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        _connection?.Dispose();
    }
}