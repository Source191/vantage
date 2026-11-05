using Source191.Torni.Application;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;

using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application;

public sealed class StateTransitionNotifierShould : IDisposable
{
    private readonly StringWriter terminal = new();

    private StateTransitionNotifier Notifier => new(terminal);

    public void Dispose()
    {
        terminal.Dispose();
    }

    [Fact]
    public void RingTheTerminalBellOnceWhenABackgroundSessionStartsWaitingForInput()
    {
        SessionRow working = Row("remove-sonar", DisplayState.Working);
        SessionRow waiting = working with { Session = working.Session with { State = DisplayState.NeedsInput } };

        Notifier.Observe([working], [waiting]);

        terminal.ToString().ShouldBe("\a");
    }

    [Fact]
    public void StayQuietForFinishingFailingAnUnchangedWaitOrTheFirstLoad()
    {
        SessionRow working = Row("remove-sonar", DisplayState.Working);
        SessionRow waiting = working with { Session = working.Session with { State = DisplayState.NeedsInput } };
        SessionRow finished = working with { Session = working.Session with { State = DisplayState.Completed } };
        SessionRow failed = working with { Session = working.Session with { State = DisplayState.Failed } };

        Notifier.Observe([working], [finished]);
        Notifier.Observe([working], [failed]);
        Notifier.Observe([waiting], [waiting]);
        Notifier.Observe([], [waiting]);

        terminal.ToString().ShouldBeEmpty();
    }

    [Fact]
    public void IgnoreInteractiveSessionsBecauseClaudeRingsTheirOwnTab()
    {
        SessionRow working = Row("repos-31", DisplayState.Working, interactive: true);
        SessionRow waiting = working with { Session = working.Session with { State = DisplayState.NeedsInput } };

        Notifier.Observe([working], [waiting]);

        terminal.ToString().ShouldBeEmpty();
    }

    private static SessionRow Row(string name, DisplayState state, bool interactive = false)
    {
        SessionId id = SessionId.From(Guid.NewGuid().ToString());
        FolderPath directory = FolderPath.From(@"C:\repos\Northwind.Orders");
        Session session = interactive
            ? new Session.Interactive(id, Listed.As(name), directory, Fixtures.Noon, state, ProcessId: 4242)
            : new Session.Background(id, JobId.From("81cdb0f9"), Listed.As(name), directory, Fixtures.Noon, state, new JobProcess.None());

        return new SessionRow(session, new SessionLocation.InRepository("Northwind.Orders", directory), new Head.OnBranch(BranchName.From("main")), new LiveTab.None(), HasTranscript: true);
    }
}