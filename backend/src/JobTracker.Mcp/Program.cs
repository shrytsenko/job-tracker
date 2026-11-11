using JobTracker.Mcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// MCP server for the Job Tracker, started by Claude Desktop / Claude Code over stdio.
// It talks only to the tracker's REST API (no database access); set JobTrackerApi__BaseUrl to point elsewhere.
var builder = Host.CreateApplicationBuilder(args);

// stdout carries the MCP protocol, so every log line must go to stderr.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var baseUrl = builder.Configuration["JobTrackerApi:BaseUrl"] ?? "http://localhost:8080";
builder.Services.AddHttpClient<TrackerApi>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services
    .AddMcpServer(options => options.ServerInfo = new() { Name = "job-tracker", Version = "1.0.0" })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
