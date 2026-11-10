using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using JobTracker.Core.Ai;
using Microsoft.Extensions.Logging;

namespace JobTracker.Infrastructure.Ai;

/// <summary>
/// <see cref="ILlmClient"/> on Claude, through the official Anthropic SDK. The only class that knows about the
/// provider: it maps the request to a Messages API call with structured output, and every failure to an
/// <see cref="LlmException"/> the API can explain to the user. Prompts and job text are never logged.
/// </summary>
public sealed class AnthropicLlmClient : ILlmClient
{
    private const int MaxTokens = 16000;

    private readonly AnthropicOptions _options;
    private readonly ILogger<AnthropicLlmClient> _logger;
    private readonly AnthropicClient? _client;

    public AnthropicLlmClient(AnthropicOptions options, ILogger<AnthropicLlmClient> logger)
    {
        _options = options;
        _logger = logger;

        // No key is a normal state (AI features simply report "not configured"), not a startup failure.
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _client = new AnthropicClient
            {
                ApiKey = options.ApiKey,
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                MaxRetries = 1,
            };
        }
    }

    public async Task<LlmJsonResponse> GenerateJsonAsync(LlmJsonRequest request, CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            throw new LlmException(LlmFailure.NotConfigured,
                "AI analysis is not configured. Set the Anthropic API key (Anthropic:ApiKey) and restart the API.");
        }

        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = MaxTokens,
            System = request.SystemPrompt,
            Messages = [new BetaUserMessageParam { Content = request.UserPrompt }],
            OutputConfig = new BetaOutputConfig
            {
                Effort = _options.Effort,
                Format = new BetaJsonOutputFormat
                {
                    Schema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request.JsonSchema)!,
                },
            },
            Betas = _options.UseRefusalFallback ? ["server-side-fallback-2026-07-01"] : null,
            // "default": the API picks the recommended fallback model per refusal category (no model list to maintain).
            Fallbacks = _options.UseRefusalFallback ? (BetaFallbacksParam)new Default() : null,
        };

        var started = System.Diagnostics.Stopwatch.StartNew();
        BetaMessage response;
        try
        {
            response = await _client.Beta.Messages.Create(parameters, cancellationToken);
        }
        catch (Exception ex) when (Map(ex) is { } mapped)
        {
            _logger.LogWarning("Claude request failed after {ElapsedMs} ms: {Failure} ({ExceptionType})",
                started.ElapsedMilliseconds, mapped.Failure, ex.GetType().Name);
            throw mapped;
        }

        _logger.LogInformation("Claude answered with {Model} in {ElapsedMs} ms (stop reason {StopReason}, {InputTokens} in / {OutputTokens} out)",
            response.Model, started.ElapsedMilliseconds, response.StopReason, response.Usage.InputTokens, response.Usage.OutputTokens);

        var stopReason = response.StopReason?.ToString();
        if (stopReason == "refusal")
        {
            throw new LlmException(LlmFailure.Refused, "The AI declined to analyse this text.");
        }

        if (stopReason == "max_tokens")
        {
            throw new LlmException(LlmFailure.InvalidOutput, "The AI answer was cut off before it was complete.");
        }

        var json = string.Concat(response.Content.Select(b => b.Value).OfType<BetaTextBlock>().Select(t => t.Text));
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new LlmException(LlmFailure.InvalidOutput, "The AI returned an empty answer.");
        }

        return new LlmJsonResponse(json, response.Model.ToString() ?? _options.Model);
    }

    /// <summary>Most specific first: each SDK exception becomes a failure the user can act on.</summary>
    private static LlmException? Map(Exception ex) => ex switch
    {
        LlmException => null,
        AnthropicRateLimitException => new(LlmFailure.RateLimited, "The AI service is rate limiting requests. Try again in a minute.", ex),
        AnthropicUnauthorizedException or AnthropicForbiddenException =>
            new(LlmFailure.NotConfigured, "The Anthropic API key was rejected. Check Anthropic:ApiKey.", ex),
        AnthropicRequestTimeoutException or AnthropicGatewayTimeoutException or TaskCanceledException or TimeoutException =>
            new(LlmFailure.Timeout, "The AI service took too long to answer. Try again.", ex),
        AnthropicOverloadedException or AnthropicServiceUnavailableException or Anthropic5xxException =>
            new(LlmFailure.ProviderError, "The AI service is temporarily unavailable. Try again shortly.", ex),
        AnthropicIOException => new(LlmFailure.ProviderError, "Could not reach the AI service. Check the network connection.", ex),
        AnthropicException => new(LlmFailure.ProviderError, "The AI service returned an error.", ex),
        _ => null,
    };
}
