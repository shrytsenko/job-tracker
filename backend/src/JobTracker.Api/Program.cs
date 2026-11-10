using System.Text.Json.Serialization;
using JobTracker.Api.Endpoints;
using JobTracker.Api.ErrorHandling;
using JobTracker.Infrastructure;
using JobTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("JobTracker")
    ?? throw new InvalidOperationException("Connection string 'JobTracker' is not configured.");
builder.Services.AddInfrastructure(connectionString);

// Enums travel as strings ("Applied", not 2) — readable in Swagger and stable for the UI.
// Integer values are rejected so a typo like 99 can't sneak in as an undefined enum.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();

var app = builder.Build();

// Personal single-user tool: applying migrations on startup keeps "dotnet run" zero-setup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JobTrackerDbContext>();
    db.Database.Migrate();

    // Fictional demo data, only when asked for (the "demo" launch profile) and only into an empty database.
    if (app.Configuration.GetValue<bool>("SeedDemoData"))
    {
        var today = DateOnly.FromDateTime(app.Services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        if (await DemoData.SeedIfEmptyAsync(db, today))
        {
            app.Logger.LogInformation("Seeded fictional demo data");
        }
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "JobTracker API v1");
        options.DocumentTitle = "JobTracker API";
    });
}
else
{
    // Locally, redirecting http://localhost:5122 to https://localhost:7175 only breaks Swagger UI:
    // the browser treats the redirect as cross-origin and drops the request.
    app.UseHttpsRedirection();
}

app.MapApplicationEndpoints();
app.MapContactEndpoints();
app.MapCompanyEndpoints();
app.MapDashboardEndpoints();

app.Run();
