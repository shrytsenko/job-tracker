using JobTracker.Api.Contracts;
using JobTracker.Core.Entities;
using JobTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Endpoints;

public static class ContactEndpoints
{
    public static RouteGroupBuilder MapContactEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/contacts").WithTags("Contacts");

        group.MapGet("/", List).WithSummary("List contacts");
        group.MapGet("/{id:guid}", GetById).WithName("GetContact").WithSummary("Get one contact with linked applications");
        group.MapPost("/", Create).WithSummary("Create a contact");
        group.MapPut("/{id:guid}", Update).WithSummary("Update a contact (full replace of editable fields)");
        group.MapDelete("/{id:guid}", Delete).WithSummary("Delete a contact (unlinks it from applications)");

        return group;
    }

    private static async Task<Ok<List<ContactSummaryResponse>>> List(JobTrackerDbContext db, CancellationToken ct)
    {
        var contacts = await db.Contacts.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
        return TypedResults.Ok(contacts.Select(ContactSummaryResponse.From).ToList());
    }

    private static async Task<Results<Ok<ContactDetailResponse>, NotFound>> GetById(
        Guid id, JobTrackerDbContext db, CancellationToken ct)
    {
        var contact = await db.Contacts
            .AsNoTracking()
            .Include(c => c.Applications)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return contact is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ContactDetailResponse.From(contact));
    }

    private static async Task<CreatedAtRoute<ContactDetailResponse>> Create(
        SaveContactRequest request, JobTrackerDbContext db, CancellationToken ct)
    {
        var contact = new Contact(request.Name, request.Role);
        Apply(request, contact);

        db.Contacts.Add(contact);
        await db.SaveChangesAsync(ct);

        return TypedResults.CreatedAtRoute(ContactDetailResponse.From(contact), "GetContact", new { id = contact.Id });
    }

    private static async Task<Results<Ok<ContactDetailResponse>, NotFound>> Update(
        Guid id, SaveContactRequest request, JobTrackerDbContext db, CancellationToken ct)
    {
        var contact = await db.Contacts.Include(c => c.Applications).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (contact is null) return TypedResults.NotFound();

        contact.UpdateDetails(request.Name, request.Role);
        Apply(request, contact);

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(ContactDetailResponse.From(contact));
    }

    private static async Task<Results<NoContent, NotFound>> Delete(Guid id, JobTrackerDbContext db, CancellationToken ct)
    {
        var deleted = await db.Contacts.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    private static void Apply(SaveContactRequest request, Contact contact)
    {
        contact.Company = request.Company;
        contact.LinkedInUrl = request.LinkedInUrl;
        contact.Email = request.Email;
        contact.Notes = request.Notes;
        contact.LastContactOn = request.LastContactOn;
    }
}
