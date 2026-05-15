using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebTransactions.Api;
using WebTransactions.Api.Data;
using WebTransactions.Api.Services;

namespace WebTransactions.Api.Tests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that boots the full application
/// in memory for functional testing. Makes two substitutions over the real application:
/// <list type="bullet">
/// <item><description>
/// Replaces the SQLite file database with an in-memory SQLite connection, keeping it open
/// for the lifetime of the factory to prevent the in-memory database from being destroyed
/// between requests
/// </description></item>
/// <item><description>
/// Replaces <see cref="IExchangeRateService"/> with <see cref="FakeExchangeRateService"/>,
/// returning a fixed exchange rate of 1.08 to avoid real HTTP calls to the Treasury API
/// </description></item>
/// </list>
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ServiceDescriptor? dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            ServiceDescriptor? exchangeRateDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IExchangeRateService));
            if (exchangeRateDescriptor is not null)
                services.Remove(exchangeRateDescriptor);

            services.AddScoped<IExchangeRateService>(_ => new FakeExchangeRateService(1.08m));

            
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}