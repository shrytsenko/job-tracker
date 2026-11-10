using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Entities;

namespace JobTracker.Api.Contracts;

/// <param name="UpdatedAt">Null until the profile has been saved once.</param>
public record ProfileResponse(string CvSummary, string? FullName, DateTimeOffset? UpdatedAt);

/// <param name="FullName">Used to sign reply drafts.</param>
public record SaveProfileRequest(
    [MaxLength(Profile.MaxCvSummaryLength)] string? CvSummary,
    [MaxLength(Profile.MaxFullNameLength)] string? FullName = null);
