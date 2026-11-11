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

/// <summary>
/// Thrown by <see cref="ILlmClient"/> implementations and the AI services when no usable answer was produced.
/// <see cref="Failure"/> says why, so callers don't have to parse the message.
/// </summary>
/// <param name="failure">Why the call failed.</param>
/// <param name="message">A message that is safe to show the user.</param>
/// <param name="innerException">The provider's original error, if any.</param>
public sealed class LlmException(LlmFailure failure, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    /// <summary>Why the call failed.</summary>
    public LlmFailure Failure { get; } = failure;
}
