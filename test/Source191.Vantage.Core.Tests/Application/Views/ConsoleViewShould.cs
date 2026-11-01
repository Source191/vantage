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
        ConsoleState worktrees = ConsoleView.Apply(Loaded, new SelectTab(ConsoleTab.Worktrees));
        ConsoleState sessions = ConsoleView.Apply(worktrees, new SelectTab(ConsoleTab.Sessions));

        worktrees.Active.ShouldBe(ConsoleTab.Worktrees);
        worktrees.Sessions.ShouldBe(Loaded.Sessions);
        worktrees.Worktrees.ShouldBe(Loaded.Worktrees);
        sessions.Active.ShouldBe(ConsoleTab.Sessions);
        sessions.Sessions.ShouldBe(Loaded.Sessions);
    }

    [Fact]
    public void SelectTheConfigTab()
    {
        ConsoleView.Apply(Loaded, new SelectTab(ConsoleTab.Config)).Active.ShouldBe(ConsoleTab.Config);
    }

    [Fact]
    public void MoveRightThroughTheTabsStoppingAtTheLast()
    {
        ConsoleState worktrees = ConsoleView.Apply(Loaded, new NextTab());
        ConsoleState config = ConsoleView.Apply(worktrees, new NextTab());

        worktrees.Active.ShouldBe(ConsoleTab.Worktrees);
        config.Active.ShouldBe(ConsoleTab.Config);
        ConsoleView.Apply(config, new NextTab()).Active.ShouldBe(ConsoleTab.Config);
    }

    [Fact]
    public void MoveLeftThroughTheTabsStoppingAtTheFirst()
    {
        ConsoleState config = Loaded with { Active = ConsoleTab.Config };

        ConsoleState worktrees = ConsoleView.Apply(config, new PreviousTab());
        ConsoleState sessions = ConsoleView.Apply(worktrees, new PreviousTab());

        worktrees.Active.ShouldBe(ConsoleTab.Worktrees);
        sessions.Active.ShouldBe(ConsoleTab.Sessions);
        ConsoleView.Apply(sessions, new PreviousTab()).Active.ShouldBe(ConsoleTab.Sessions);
    }

    [Fact]
    public void OpenAWorktreesBackgroundSessionInItsOwnTab()
    {
        RowDecision decision = ConsoleView.OpenSession(Loaded, new OpenWorktreeSession(SessionId.From("sess-2"), "fix-tests"));

        decision.ShouldBe(new RowDecision.Act(new OpenTabForSession((BackgroundSession)Second.Session, Second.Tab)));
    }

    [Fact]
    public void SwitchToTheOpenTabOfAWorktreesSession()
    {
        SessionRow withTab = Second with { Tab = new LiveTab.Open(new TabColour("Purple", "#8E44AD")) };
        ConsoleState state = Loaded with { Sessions = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([First, withTab])).State };

        RowDecision decision = ConsoleView.OpenSession(state, new OpenWorktreeSession(SessionId.From("sess-2"), "fix-tests"));

        decision.ShouldBe(new RowDecision.Act(new OpenTabForSession((BackgroundSession)withTab.Session, withTab.Tab)));
    }

    [Fact]
    public void PickTheWorktreesSessionByIdWhenTwoShareAName()
    {
        SessionRow twin = Row("sess-3", "fix-tests");
        ConsoleState state = Loaded with { Sessions = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([First, Second, twin])).State };

        RowDecision decision = ConsoleView.OpenSession(state, new OpenWorktreeSession(SessionId.From("sess-3"), "fix-tests"));

        decision.ShouldBe(new RowDecision.Act(new OpenTabForSession((BackgroundSession)twin.Session, twin.Tab)));
    }

    [Fact]
    public void SayAWorktreesTerminalSessionRunsInItsOwnTerminal()
    {
        InteractiveSession terminal = new(SessionId.From("sess-4"), "repos-94", FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), Noon, DisplayState.Idle, ProcessId: 200);
        SessionRow row = new(terminal, new SessionLocation.InRepository("Northwind.Orders", terminal.WorkingDirectory), new Head.Unknown(), new LiveTab.None(), HasTranscript: true);
        ConsoleState state = Loaded with { Sessions = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([First, row])).State };

        RowDecision decision = ConsoleView.OpenSession(state, new OpenWorktreeSession(SessionId.From("sess-4"), "repos-94"));

        decision.ShouldBe(new RowDecision.Explain(new StatusMessage.RunsInOwnTerminal("repos-94", HasTranscript: true)));
    }

    [Fact]
    public void SayAWorktreesSessionWasNeverSentAMessage()
    {
        SessionRow fresh = Second with { HasTranscript = false };
        ConsoleState state = Loaded with { Sessions = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([First, fresh])).State };

        RowDecision decision = ConsoleView.OpenSession(state, new OpenWorktreeSession(SessionId.From("sess-2"), "fix-tests"));

        decision.ShouldBe(new RowDecision.Explain(new StatusMessage.NeverSentAMessage("fix-tests")));
    }

    [Fact]
    public void SayAWorktreesSessionIsNotListed()
    {
        RowDecision decision = ConsoleView.OpenSession(Loaded, new OpenWorktreeSession(SessionId.From("sess-9"), "gone-away"));

        decision.ShouldBe(new RowDecision.Explain(new StatusMessage.SessionNotListed("gone-away")));
    }

    [Fact]
    public void FindAWorktreesSessionThatTheSessionsFilterHides()
    {
        SessionsViewState filtered = SessionsView.Reduce(Loaded.Sessions, new KeyPressed(Key.Slash)).State;
        filtered = SessionsView.Reduce(filtered, new KeyPressed(Key.Of('c'))).State;
        filtered = SessionsView.Reduce(filtered, new KeyPressed(Key.Enter)).State;
        ConsoleState state = Loaded with { Sessions = filtered, Active = ConsoleTab.Worktrees };

        RowDecision decision = ConsoleView.OpenSession(state, new OpenWorktreeSession(SessionId.From("sess-2"), "fix-tests"));

        decision.ShouldBeOfType<RowDecision.Act>("a filter on the Sessions tab does not hide a worktree's session");
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
        BackgroundSession session = new(SessionId.From(sessionId), JobId.From(sessionId == "sess-1" ? "81cdb0f9" : "92ddc1a0"), name, FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), Noon, DisplayState.Idle, new JobProcess.None());

        return new SessionRow(session, new SessionLocation.InRepository("Northwind.Orders", session.WorkingDirectory), new Head.OnBranch(BranchName.From("main")), new LiveTab.None(), HasTranscript: true);
    }
}