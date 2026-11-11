using JobTracker.Core.Ai;

namespace JobTracker.Api;

/// <summary>
/// The API's log messages, source-generated: typed, and free when the level is off.
/// Job text, CVs and drafts are personal data and are never part of a message.
/// </summary>
internal static partial class ApiLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Job analysis for application {ApplicationId} failed: {Failure}")]
    public static partial void AnalysisFailed(ILogger logger, Guid applicationId, LlmFailure failure);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reply draft for application {ApplicationId} failed: {Failure}")]
    public static partial void ReplyDraftFailed(ILogger logger, Guid applicationId, LlmFailure failure);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded fictional demo data")]
    public static partial void SeededDemoData(ILogger logger);
}
