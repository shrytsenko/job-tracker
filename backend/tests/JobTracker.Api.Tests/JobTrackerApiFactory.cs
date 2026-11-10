using Microsoft.AspNetCore.Hosting;
using JobTracker.Core.Ai;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace JobTracker.Api.Tests;

/// <summary>
/// Boots the real API in memory against a real PostgreSQL running in a throwaway Docker container.
/// One container is shared by the whole test run (starting one takes a few seconds); every factory, and so
/// every test class, gets its own database inside it, so classes stay isolated. The app runs its real
/// migrations on startup, so the schema under test is exactly the production schema.
/// Requires Docker to be running.
/// </summary>
public sealed class JobTrackerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly Lazy<Task<PostgreSqlContainer>> SharedContainer = new(StartContainerAsync);

    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        var container = await SharedContainer.Value;
        _connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = $"jobtracker_test_{Guid.NewGuid():N}",
        }.ConnectionString;
    }

    // The container is removed automatically when the test run ends (Testcontainers' cleanup).
    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // EF's Migrate() creates the database if it does not exist yet.
        builder.UseSetting("ConnectionStrings:JobTracker", _connectionString);

        // Never call the real AI provider from tests: swap in a fake each test class can steer.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILlmClient>();
            services.AddSingleton<ILlmClient>(FakeLlm);
        });
    }

    /// <summary>The AI client the API uses in tests. Set <see cref="FakeLlmClient.Respond"/> to control its answer.</summary>
    public FakeLlmClient FakeLlm { get; } = new();

    private static async Task<PostgreSqlContainer> StartContainerAsync()
    {
        var container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await container.StartAsync();
        return container;
    }
}
