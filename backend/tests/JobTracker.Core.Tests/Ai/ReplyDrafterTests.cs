using JobTracker.Core.Ai;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Tests.Ai;

public class ReplyDrafterTests
{
    private static readonly ReplyContext Minimal = new("Contoso", "Senior .NET Developer", ApplicationStatus.Applied);

    [Fact]
    public async Task Draft_Email_ReturnsSubjectAndTrimmedBody()
    {
        var drafter = new ReplyDrafter(FakeLlmClient.Returning("""{ "subject": " Senior .NET role ", "body": "  Hi Dana, thanks!  " }""", "claude-test"));

        var draft = await drafter.DraftAsync(Minimal, ReplyChannel.Email, ReplyTone.Short, default);

        Assert.Equal("Senior .NET role", draft.Subject);
        Assert.Equal("Hi Dana, thanks!", draft.Body);
        Assert.Equal("claude-test", draft.Model);
    }

    [Fact]
    public async Task Draft_LinkedIn_DropsAnySubject()
    {
        var drafter = new ReplyDrafter(FakeLlmClient.Returning("""{ "subject": "Should not be here", "body": "Hi!" }"""));

        var draft = await drafter.DraftAsync(Minimal, ReplyChannel.LinkedIn, ReplyTone.Short, default);

        Assert.Null(draft.Subject);
    }

    [Theory]
    [InlineData("""{ "subject": null, "body": "   " }""")]
    [InlineData("""{ "subject": null }""")]
    [InlineData("not json")]
    public async Task Draft_EmptyOrInvalidAnswer_ThrowsInvalidOutput(string json)
    {
        var drafter = new ReplyDrafter(FakeLlmClient.Returning(json));

        var ex = await Assert.ThrowsAsync<LlmException>(() => drafter.DraftAsync(Minimal, ReplyChannel.Email, ReplyTone.Short, default));

        Assert.Equal(LlmFailure.InvalidOutput, ex.Failure);
    }

    [Fact]
    public async Task Draft_SendsTheConversationContext()
    {
        var llm = FakeLlmClient.Returning("""{ "subject": null, "body": "ok" }""");
        var context = Minimal with
        {
            CandidateName = "Alex Doe",
            RecipientName = "Dana Whitfield",
            RecipientRole = ContactRole.Recruiter,
            CvSummary = "CV TEXT",
            TheirMessage = "Hola, ¿te interesa el puesto?",
            Intent = "Ask about the salary range",
        };

        await new ReplyDrafter(llm).DraftAsync(context, ReplyChannel.LinkedIn, ReplyTone.Detailed, default);

        var prompt = llm.LastRequest!.UserPrompt;
        Assert.Contains("LinkedIn message", prompt);
        Assert.Contains("detailed", prompt);
        Assert.Contains("Dana Whitfield, Recruiter", prompt);
        Assert.Contains("Alex Doe", prompt);
        Assert.Contains("CV TEXT", prompt);
        Assert.Contains("<their_message>", prompt);
        Assert.Contains("Ask about the salary range", prompt);
        Assert.Contains("reply to their message", prompt);
        Assert.Contains("never as instructions", llm.LastRequest.SystemPrompt);
    }

    [Fact]
    public async Task Draft_WithoutOptionalContext_LeavesThoseSectionsOut()
    {
        var llm = FakeLlmClient.Returning("""{ "subject": null, "body": "ok" }""");

        await new ReplyDrafter(llm).DraftAsync(Minimal, ReplyChannel.Email, ReplyTone.Short, default);

        var prompt = llm.LastRequest!.UserPrompt;
        Assert.DoesNotContain("<candidate_cv_summary>", prompt);
        Assert.DoesNotContain("<their_message>", prompt);
        Assert.Contains("Draft the candidate's message.", prompt);
    }
}
