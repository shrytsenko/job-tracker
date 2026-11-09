using JobTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Endpoints;

/// <summary>
/// Company is free text on both applications and contacts (a recruiter's company is often an agency,
/// not the hiring company). This endpoint only feeds autocomplete so the same name is reused consistently.
/// </summary>
public static class CompanyEndpoints
{
    public static RouteGroupBuilder MapCompanyEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/companies").WithTags("Companies");

        group.MapGet("/", List).WithSummary("Distinct company names used by applications and contacts, for autocomplete");

        return group;
    }

    private static async Task<Ok<List<string>>> List(JobTrackerDbContext db, CancellationToken ct)
    {
        var applicationCompanies = await db.Applications.Select(a => a.Company).Distinct().ToListAsync(ct);
        var contactCompanies = await db.Contacts.Where(c => c.Company != null).Select(c => c.Company!).Distinct().ToListAsync(ct);

        // Merge in memory: case-insensitive de-duplication ("contoso" and "Contoso" are one suggestion).
        var names = applicationCompanies
            .Concat(contactCompanies)
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .DistinctBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return TypedResults.Ok(names);
    }
}
