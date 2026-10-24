using Source191.Vantage.Domain;
using Source191.Vantage.Screens;

namespace Source191.Vantage.UnitTests.Screens;

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
        SessionRow working = Row("repos-31", DisplayState.Working, SessionKind.Interactive);
        SessionRow waiting = working with { Session = working.Session with { State = DisplayState.NeedsInput } };

        Notifier.Observe([working], [waiting]);

        terminal.ToString().ShouldBeEmpty();
    }

    private static SessionRow Row(string name, DisplayState state, SessionKind kind = SessionKind.Background)
    {
        Session session = new(Guid.NewGuid().ToString(), kind == SessionKind.Background ? "81cdb0f9" : null, name, @"C:\repos\Northwind.Orders", kind, Noon, state);

        return new SessionRow(session, "Northwind.Orders", "main", IsWorktree: false, Description: null, HasLiveTab: false, HasTranscript: true);
    }
}