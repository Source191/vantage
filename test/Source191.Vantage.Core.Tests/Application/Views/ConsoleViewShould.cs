using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class ConsoleViewShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly SessionRow First = Row("sess-1", "change-feed");
    private static readonly SessionRow Second = Row("sess-2", "fix-tests");

    private static readonly ConsoleState Loaded = ConsoleState.Empty with
    {
        Sessions = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([First, Second])).State,
    };

    [Fact]
    public void StartOnTheSessionsTab()
    {
        ConsoleState.Empty.Active.ShouldBe(ConsoleTab.Sessions);
    }

    [Fact]
    public void SwitchToTheWorktreesTabAndBackWithoutTouchingEitherState()
    {
        ConsoleState worktrees = ConsoleView.Apply(Loaded, new SwitchToWorktrees());
        ConsoleState sessions = ConsoleView.Apply(worktrees, new SwitchToSessions());

        worktrees.Active.ShouldBe(ConsoleTab.Worktrees);
        worktrees.Sessions.ShouldBe(Loaded.Sessions);
        worktrees.Worktrees.ShouldBe(Loaded.Worktrees);
        sessions.Active.ShouldBe(ConsoleTab.Sessions);
        sessions.Sessions.ShouldBe(Loaded.Sessions);
    }

    [Fact]
    public void JumpToASessionOnTheSessionsTabById()
    {
        ConsoleState onWorktrees = ConsoleView.Apply(Loaded, new SwitchToWorktrees());

        ConsoleState jumped = ConsoleView.Apply(onWorktrees, new JumpToSession(SessionId.From("sess-2")));

        jumped.Active.ShouldBe(ConsoleTab.Sessions);
        jumped.Sessions.Selected.ShouldBe(Second);
    }

    [Fact]
    public void ClearAFilterThatWouldHideTheJumpedToSession()
    {
        SessionsViewState filtered = SessionsView.Reduce(Loaded.Sessions, new KeyPressed(Key.Slash)).State;
        filtered = SessionsView.Reduce(filtered, new KeyPressed(Key.Of('c'))).State;
        filtered = SessionsView.Reduce(filtered, new KeyPressed(Key.Enter)).State;
        ConsoleState state = Loaded with { Sessions = filtered, Active = ConsoleTab.Worktrees };

        ConsoleState jumped = ConsoleView.Apply(state, new JumpToSession(SessionId.From("sess-2")));

        jumped.Sessions.Selected.ShouldBe(Second);
        jumped.Sessions.VisibleRows.Count.ShouldBe(2);
    }

    [Fact]
    public void HandleEveryEffectThatSwitchesTab()
    {
        IEnumerable<Type> switching = typeof(ViewEffect).Assembly.GetTypes().Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(ISwitchesTab)));

        switching.ShouldAllBe(type => ConsoleView.Handles(type), "ConsoleView.Apply has no case for a tab-switching effect");
        ConsoleView.Handles(typeof(Quit)).ShouldBeFalse();
    }

    private static SessionRow Row(string sessionId, string name)
    {
        BackgroundSession session = new(SessionId.From(sessionId), JobId.From(sessionId == "sess-1" ? "81cdb0f9" : "92ddc1a0"), name, FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), Noon, DisplayState.Idle);

        return new SessionRow(session, "Northwind.Orders", BranchName.From("main"), HasLiveTab: false, HasTranscript: true);
    }
}
