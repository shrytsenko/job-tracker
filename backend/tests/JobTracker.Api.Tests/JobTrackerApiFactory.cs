using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace JobTracker.Api.Tests;

/// <summary>
/// Boots the real API in memory against a private in-memory SQLite database.
/// A shared-cache in-memory DB lives as long as one connection to it is open, so the
/// factory holds a "keep-alive" connection for its lifetime. The app runs its real
/// migrations on startup, so the schema under test is exactly the production schema.
/// </summary>
public sealed class JobTrackerApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=file:jobtracker-tests-{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _keepAlive;

    public JobTrackerApiFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:JobTracker", _connectionString);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
