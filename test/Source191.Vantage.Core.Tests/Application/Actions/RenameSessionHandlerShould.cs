using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using static Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class RenameSessionHandlerShould
{
    private readonly FakeProcessRunner processes = Processes();
    private readonly FakeTranscripts transcripts = new();

    private RenameSessionHandler Handler => new(new ClaudeCli(processes, Claude), transcripts);

    private static RenameSession Rename => new(Background(Repository, isWorktree: false, DisplayState.Idle), SessionName.Parse("fix-124"));

    [Fact]
    public async Task RenameBySwappingTheJobForOneResumedUnderTheNewNameKeepingTheSessionId()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-124", string.Empty));
        transcripts.WithMessages("sess-1");

        string message = await Handler.HandleAsync(Rename, CancellationToken.None);

        List<ProcessRequest> claude = [.. processes.CapturedRequests.Where(request => request.FileName == Claude)];
        claude.Select(request => string.Join(' ', request.Arguments)).ShouldBe(["rm 81cdb0f9", "--bg --resume sess-1 -n fix-124"]);
        claude[1].WorkingDirectory.ShouldBe(Repository);
        message.ShouldBe("Renamed fix-123 to fix-124; it restarts idle and keeps its conversation.");
    }

    [Fact]
    public async Task RefuseToRenameASessionWithNoTranscriptBecauseTheSwapWouldLoseIt()
    {
        string message = await Handler.HandleAsync(Rename, CancellationToken.None);

        message.ShouldBe("fix-123 has no messages yet, so it cannot be renamed; send it something first.");
        processes.CapturedRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task PutTheOldNameBackWhenTheResumeUnderTheNewNameFails()
    {
        processes.Answer(request => request.Arguments.Contains("fix-124"), new ProcessResult(1, string.Empty, "boom"));
        processes.Answer(request => request.Arguments.Contains("fix-123"), new ProcessResult(0, "backgrounded · 81cdb0f9 · fix-123", string.Empty));
        transcripts.WithMessages("sess-1");

        string message = await Handler.HandleAsync(Rename, CancellationToken.None);

        processes.CapturedRequests.Where(request => request.FileName == Claude).Select(request => request.Arguments[^1]).ShouldBe(["81cdb0f9", "fix-124", "fix-123"]);
        message.ShouldStartWith("Could not rename fix-123:");
        message.ShouldEndWith("it is back under its old name.");
    }
}