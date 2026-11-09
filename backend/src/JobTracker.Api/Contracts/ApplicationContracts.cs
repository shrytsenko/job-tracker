using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Api.Contracts;

/// <summary>Body for creating (POST) or fully updating (PUT) an application.</summary>
/// <param name="Status">Optional. On create defaults to Interested; on update, null keeps the current status.</param>
public record SaveApplicationRequest(
    [Required, MaxLength(200)] string Company,
    [Required, MaxLength(200)] string RoleTitle,
    Track Track,
    Channel Channel,
    EmploymentType EmploymentType,
    ApplicationStatus? Status = null,
    [Url, MaxLength(2000)] string? JobUrl = null,
    [MaxLength(100)] string? RemoteRegion = null,
    [MaxLength(100)] string? HybridCity = null,
    [Range(0, 100_000_000)] decimal? CompensationAmount = null,
    [StringLength(3, MinimumLength = 3)] string? CompensationCurrency = null,
    CompensationPeriod? CompensationPeriod = null,
    bool? CompensationIsNet = null,
    DateOnly? AppliedOn = null,
    DateOnly? LastContactOn = null,
    [MaxLength(500)] string? NextActionDescription = null,
    DateOnly? NextActionDueOn = null,
    string? Notes = null);

public record AddActivityRequest(
    ActivityType Type,
    [Required, MaxLength(2000)] string Text,
    DateOnly? Date = null);

public record ApplicationSummaryResponse(
    Guid Id,
    string Company,
    string RoleTitle,
    Track Track,
    Channel Channel,
    ApplicationStatus Status,
    EmploymentType EmploymentType,
    DateOnly? AppliedOn,
    DateOnly? LastContactOn,
    string? NextActionDescription,
    DateOnly? NextActionDueOn)
{
    public static ApplicationSummaryResponse From(Application a) => new(
        a.Id, a.Company, a.RoleTitle, a.Track, a.Channel, a.Status, a.EmploymentType,
        a.AppliedOn, a.LastContactOn, a.NextActionDescription, a.NextActionDueOn);
}

public record ApplicationDetailResponse(
    Guid Id,
    string Company,
    string RoleTitle,
    string? JobUrl,
    Track Track,
    Channel Channel,
    ApplicationStatus Status,
    EmploymentType EmploymentType,
    string? RemoteRegion,
    string? HybridCity,
    decimal? CompensationAmount,
    string? CompensationCurrency,
    CompensationPeriod? CompensationPeriod,
    bool? CompensationIsNet,
    DateOnly? AppliedOn,
    DateOnly? LastContactOn,
    string? NextActionDescription,
    DateOnly? NextActionDueOn,
    string? Notes,
    IReadOnlyList<ContactSummaryResponse> Contacts,
    IReadOnlyList<ActivityResponse> Activities)
{
    public static ApplicationDetailResponse From(Application a) => new(
        a.Id, a.Company, a.RoleTitle, a.JobUrl, a.Track, a.Channel, a.Status, a.EmploymentType,
        a.RemoteRegion, a.HybridCity,
        a.CompensationAmount, a.CompensationCurrency, a.CompensationPeriod, a.CompensationIsNet,
        a.AppliedOn, a.LastContactOn, a.NextActionDescription, a.NextActionDueOn, a.Notes,
        a.Contacts.OrderBy(c => c.Name).Select(ContactSummaryResponse.From).ToList(),
        a.Activities.OrderByDescending(x => x.Date).Select(ActivityResponse.From).ToList());
}

public record ActivityResponse(Guid Id, ActivityType Type, string Text, DateOnly Date)
{
    public static ActivityResponse From(Activity x) => new(x.Id, x.Type, x.Text, x.Date);
}
