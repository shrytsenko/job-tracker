using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Api.Contracts;

/// <summary>Body for creating (POST) or fully updating (PUT) a contact.</summary>
public record SaveContactRequest(
    [Required, MaxLength(200)] string Name,
    ContactRole Role,
    [MaxLength(200)] string? Company = null,
    [Url, MaxLength(2000)] string? LinkedInUrl = null,
    [EmailAddress, MaxLength(320)] string? Email = null,
    [MaxLength(Contact.MaxNotesLength)] string? Notes = null,
    DateOnly? LastContactOn = null);

public record ContactSummaryResponse(Guid Id, string Name, ContactRole Role, string? Company)
{
    public static ContactSummaryResponse From(Contact c) => new(c.Id, c.Name, c.Role, c.Company);
}

public record ContactDetailResponse(
    Guid Id,
    string Name,
    ContactRole Role,
    string? Company,
    string? LinkedInUrl,
    string? Email,
    string? Notes,
    DateOnly? LastContactOn,
    IReadOnlyList<ApplicationSummaryResponse> Applications)
{
    public static ContactDetailResponse From(Contact c) => new(
        c.Id, c.Name, c.Role, c.Company, c.LinkedInUrl, c.Email, c.Notes, c.LastContactOn,
        c.Applications.OrderBy(a => a.Company).Select(ApplicationSummaryResponse.From).ToList());
}
