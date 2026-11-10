namespace JobTracker.Infrastructure.Ai;

/// <summary>Settings for the Claude client, bound from the "Anthropic" configuration section.</summary>
public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    /// <summary>From user secrets (development) or the Anthropic__ApiKey environment variable. Never commit it.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-5-5";

    /// <summary>low | medium | high | xhigh | max. Medium suits extraction plus a judgement call like the match score.</summary>
    public string Effort { get; set; } = "medium";

    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>If a safety classifier declines a request, let the API retry it on Anthropic's recommended fallback model.</summary>
    public bool UseRefusalFallback { get; set; } = true;
}
