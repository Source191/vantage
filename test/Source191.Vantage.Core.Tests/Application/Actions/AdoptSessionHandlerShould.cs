using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using static Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class AdoptSessionHandlerShould
{
    private readonly FakeProcessRunner processes = Processes();
    private readonly FakeProcessRegistry processTable = new FakeProcessRegistry().Running(4242, Noon);
    private readonly FakeTranscripts transcripts = new FakeTranscripts().WithMessages("sess-9");

    private AdoptSessionHandler Handler => new(new ClaudeCli(processes, Claude), processTable, transcripts);

    [Fact]
    public async Task AdoptAnInteractiveSessionByEndingItsProcessAndResumingItUnderTheSupervisor()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 81cdb0f9 · repos-31", string.Empty));

        Outcome message = await Handler.HandleAsync(new AdoptSession(As.Terminal(Interactive("repos-31", 4242))), CancellationToken.None);

        processTable.Terminated.ShouldBe([4242]);
        ProcessRequest resume = processes.CapturedRequests.Single(request => request.Arguments.Contains("--resume"));
        resume.WorkingDirectory.ShouldBe(Repository);
        resume.Arguments.ShouldBe(["--bg", "--resume", "sess-9", "-n", "repos-31"]);
        message.ShouldBe(new AdoptSessionOutcome.Moved("repos-31"));
    }

    [Fact]
    public async Task ReportWhenTheResumeStartedACopy()
    {
        processes.Answer(request => request.Arguments.Contains("--resume"), new ProcessResult(0, "backgrounded · 99aa88bb · repos-31\nnote: the session is already running, so a copy was started", string.Empty));

        Outcome message = await Handler.HandleAsync(new AdoptSession(As.Terminal(Interactive("repos-31", 4242))), CancellationToken.None);

        AdoptSessionOutcome.Failed failed = message.ShouldBeOfType<AdoptSessionOutcome.Failed>();
        failed.Name.ShouldBe("repos-31");
        failed.Message.ShouldContain("copy");
    }

    [Fact]
    public async Task RefuseToAdoptASessionThatHasNoMessagesYetBecauseThereIsNothingToResume()
    {
        AdoptSessionHandler handler = new(new ClaudeCli(processes, Claude), processTable, new FakeTranscripts());

        Outcome message = await handler.HandleAsync(new AdoptSession(As.Terminal(Interactive("repos-f6", 4242))), CancellationToken.None);

        message.ShouldBe(new AdoptSessionOutcome.NoMessages("repos-f6"));
        processTable.Terminated.ShouldBeEmpty();
        processes.CapturedRequests.ShouldBeEmpty();
    }

}