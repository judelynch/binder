using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PokeBinder.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ConnectionString =
        "Server=localhost\\SQLEXPRESS;Database=PokeBinderTest;Trusted_Connection=True;TrustServerCertificate=True";

    private readonly string _connectionString;

    public CustomWebApplicationFactory(string? connectionString = null)
    {
        _connectionString = connectionString ?? ConnectionString;
    }

    public static string ConnectionStringFor(string databaseName) =>
        $"Server=localhost\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString
            });
        });
    }
}
