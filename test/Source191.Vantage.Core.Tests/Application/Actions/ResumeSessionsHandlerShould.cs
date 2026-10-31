using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using static Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class ResumeSessionsHandlerShould
{
    private readonly FakeProcessRunner processes = Processes();
    private readonly FakeTranscripts transcripts = new();

    private ResumeSessionsHandler Handler => new(new ClaudeCli(processes, Claude), transcripts);

    [Fact]
    public void SeparateFailedSessionsFromStoppedOnesWhenPlanningAResume()
    {
        SessionRow failed = Background(Repository, isWorktree: false, DisplayState.Failed);
        SessionRow stopped = Background(Repository, isWorktree: false, DisplayState.Stopped) with { Session = Background(Repository, false).Session with { SessionId = SessionId.From("sess-2"), State = DisplayState.Stopped } };
        SessionRow stoppedLater = stopped with { Session = stopped.Session with { SessionId = SessionId.From("sess-3"), Name = "later" } };
        SessionRow working = Background(Repository, isWorktree: false);

        ResumePlan plan = ResumeSessionsHandler.Plan([failed, stoppedLater, stopped, working]);

        plan.Automatic.ShouldBe([failed]);
        plan.ToChoose.ShouldBe([stoppedLater, stopped], "the checklist keeps the order it was given, which is the table's order");
    }

    [Fact]
    public async Task SkipASessionWithNoTranscriptWhenResumingAndSaySo()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages("sess-1");
        BackgroundSession withTranscript = Job(Repository, DisplayState.Failed);
        BackgroundSession stub = withTranscript with { SessionId = SessionId.From("sess-stub"), Name = "repos-f6" };

        Outcome message = await Handler.HandleAsync(new ResumeSessions([withTranscript, stub]), CancellationToken.None);

        processes.CapturedRequests.Count(request => request.Arguments.Contains("--resume")).ShouldBe(1);
        ResumeSessionsOutcome resume = message.ShouldBeOfType<ResumeSessionsOutcome>();
        (resume.Resumed, resume.Chosen).ShouldBe((1, 2));
        resume.Notes.ShouldBe([new ResumeNote.NeverSentAMessage("repos-f6")]);
    }

    [Fact]
    public async Task ResumeEachChosenSessionInItsOwnDirectory()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages("sess-1").WithMessages("sess-2");
        BackgroundSession first = Job(Repository, DisplayState.Failed);
        BackgroundSession second = Job(Worktree, DisplayState.Stopped) with { SessionId = SessionId.From("sess-2") };

        Outcome message = await Handler.HandleAsync(new ResumeSessions([first, second]), CancellationToken.None);

        List<ProcessRequest> resumes = [.. processes.CapturedRequests.Where(request => request.Arguments.Contains("--resume"))];
        resumes.Select(request => request.WorkingDirectory).ShouldBe([Repository, Worktree]);
        resumes.Select(request => request.Arguments[2]).ShouldBe(["sess-1", "sess-2"]);
        resumes.Select(request => request.Arguments[^1]).ShouldAllBe(name => name == "fix-123", "the display name survives the resume");
        message.ShouldBeOfType<ResumeSessionsOutcome>().Resumed.ShouldBe(2);
    }
}