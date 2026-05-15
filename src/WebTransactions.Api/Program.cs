using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using WebTransactions.Api.Components;
using WebTransactions.Api.Data;
using WebTransactions.Api.Services;

namespace WebTransactions.Api;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        //Registers MVC controllers as API endpoints in /controllers folder
        builder.Services.AddControllers();
        
        //Enables razor interactive (dynamic) components - they are going to render the HTML pages
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        
        //Adds EF core using Sqlite as database service
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
        
        // Registers exchange rate service (which calls external exchange API) with an HttpClient
        builder.Services.AddHttpClient<IExchangeRateService, ExchangeRateService>();
        builder.Services.AddScoped<ITransactionService, TransactionService>();

        // Enabling API versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddMvc();
        
        WebApplication app = builder.Build();

        // On startup, apply any pending EF Core migrations automatically
        // This creates the SQLite database file and schema if they don't exist yet
        using (IServiceScope scope = app.Services.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapControllers();
        
        // Sets App.razor as root HTML component with interactive/dynamic pages
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.Run();
    }
}