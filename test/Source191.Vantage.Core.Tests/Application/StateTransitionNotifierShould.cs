using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class StateTransitionNotifierShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

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
        SessionId id = new(Guid.NewGuid().ToString());
        FolderPath directory = new(@"C:\repos\Northwind.Orders");
        Session session = interactive
            ? new InteractiveSession(id, name, directory, Noon, state, ProcessId: 4242)
            : new BackgroundSession(id, new JobId("81cdb0f9"), name, directory, Noon, state);

        return new SessionRow(session, "Northwind.Orders", new BranchName("main"), IsWorktree: false, Description: null, HasLiveTab: false, HasTranscript: true);
    }
}