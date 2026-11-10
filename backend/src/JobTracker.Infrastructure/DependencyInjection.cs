using JobTracker.Core.Ai;
using JobTracker.Infrastructure.Ai;
using JobTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, AnthropicOptions anthropicOptions)
    {
        services.AddDbContext<JobTrackerDbContext>(options => options.UseNpgsql(connectionString));

        // One SDK client for the whole app (it pools HTTP connections); the analyzer itself is stateless.
        services.AddSingleton(anthropicOptions);
        services.AddSingleton<ILlmClient, AnthropicLlmClient>();
        services.AddScoped<JobAnalyzer>();

        return services;
    }
}
