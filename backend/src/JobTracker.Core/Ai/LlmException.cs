namespace JobTracker.Core.Ai;

/// <summary>Why an AI call did not produce a usable answer. The API maps each to an HTTP status and message.</summary>
public enum LlmFailure
{
    /// <summary>No API key configured.</summary>
    NotConfigured,
    /// <summary>The provider account has run out of credits.</summary>
    NoCredits,
    /// <summary>The provider took too long.</summary>
    Timeout,
    /// <summary>Too many requests; try again later.</summary>
    RateLimited,
    /// <summary>The provider declined the request (safety classifier).</summary>
    Refused,
    /// <summary>The answer was cut off, not valid JSON, or failed validation.</summary>
    InvalidOutput,
    /// <summary>Any other provider or network error.</summary>
    ProviderError,
}

public sealed class LlmException(LlmFailure failure, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public LlmFailure Failure { get; } = failure;
}
