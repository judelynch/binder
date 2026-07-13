using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PokeBinder.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ConnectionString =
        "Server=localhost\\SQLEXPRESS;Database=PokeBinderTest;Trusted_Connection=True;TrustServerCertificate=True";

    // Default source for anything that resolves ICardDataSource (e.g. admin sync endpoints), so no
    // test ever accidentally triggers a live download of the full pokemon-tcg-data repo from GitHub.
    public static readonly string DefaultCardDataLocalPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CardData");

    private readonly string _connectionString;
    private readonly string _cardDataLocalPath;

    public CustomWebApplicationFactory(string? connectionString = null, string? cardDataLocalPath = null)
    {
        _connectionString = connectionString ?? ConnectionString;
        _cardDataLocalPath = cardDataLocalPath ?? DefaultCardDataLocalPath;
    }

    public static string ConnectionStringFor(string databaseName) =>
        $"Server=localhost\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString,
                ["CardData:LocalPath"] = _cardDataLocalPath
            });
        });
    }
}
