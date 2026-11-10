using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Entities;

namespace JobTracker.Api.Contracts;

/// <param name="UpdatedAt">Null until the profile has been saved once.</param>
public record ProfileResponse(string CvSummary, DateTimeOffset? UpdatedAt);

public record SaveProfileRequest([MaxLength(Profile.MaxCvSummaryLength)] string? CvSummary);
