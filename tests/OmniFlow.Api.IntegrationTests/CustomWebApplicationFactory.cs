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

        // UseSetting is applied as the LAST configuration source, so it beats
        // appsettings.Development.json. (ConfigureAppConfiguration's in-memory
        // source lands BEFORE the json files under minimal hosting — the dev
        // connection string silently won and tests wrote to the developer DB.)
        builder.UseSetting("ConnectionStrings:DefaultConnection", _db.GetConnectionString());
        // No Telegram, no OTLP exporter in tests.
        builder.UseSetting("Telegram:BotToken", "");
        builder.UseSetting("Otlp:Endpoint", "");
        // The test host has no TLS listener; a 307 to https:// would break
        // POST/PUT/DELETE round-trips through the test client.
        builder.UseSetting("DisableHttpsRedirection", "true");
        // Keep scheduling/messaging local to the test host.
        builder.UseSetting("Messaging:Transport", "InMemory");
        builder.UseSetting("Quartz:UseInMemoryStore", "true");
    }

    public async Task InitializeAsync() => await _db.StartAsync();

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
