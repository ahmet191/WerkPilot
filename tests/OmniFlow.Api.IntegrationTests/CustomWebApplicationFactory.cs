using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// Boots the real API host against a disposable PostgreSQL container. The app's
/// own startup path (Database.MigrateAsync + DatabaseSeeder) runs against the
/// container, so tests get the full schema + seed data. The schema uses JSONB
/// columns, so a real Postgres is required — the EF in-memory provider would lie.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("omniflow_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Override the connection string BEFORE the host builds, so the app's
        // AddDbContext + startup migration/seed target the test container.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _db.GetConnectionString(),
                // No Telegram, no OTLP exporter in tests.
                ["Telegram:BotToken"] = "",
                ["Otlp:Endpoint"] = "",
                // The test host has no TLS listener; a 307 to https:// would
                // break POST/PUT/DELETE round-trips through the test client.
                ["DisableHttpsRedirection"] = "true",
            });
        });
    }

    public async Task InitializeAsync() => await _db.StartAsync();

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
