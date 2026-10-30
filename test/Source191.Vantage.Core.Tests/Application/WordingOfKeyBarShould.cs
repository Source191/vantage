using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WordingOfKeyBarShould
{
    private const int MostCells = 118;

    private static readonly SessionRow Running = Background("sess-1", "fix-123", DisplayState.Working, processId: 100);
    private static readonly SessionRow Stopped = Background("sess-2", "old-spike", DisplayState.Stopped, processId: null);
    private static readonly SessionRow EmptyTerminal = Terminal("sess-3", "repos-93", hasTranscript: false);
    private static readonly SessionRow BusyTerminal = Terminal("sess-4", "repos-94", hasTranscript: true);

    private static readonly string[] SecondSessionsRow = ["c Copy path", "e Open folder", "v Solution", "p Open PR", "/ Filter", "? Help", "! Report", "q Quit"];

    [Fact]
    public void NameTheKeysForARunningBackgroundSession()
    {
        KeyBar bar = Wording.SessionsBar(Selecting([Running], Running), updateKnown: false);

        Texts(bar.First).ShouldBe(["Enter Attach", "o Open tab", "x Stop", "d Delete", "r Rename", "m Memory", "n New session"]);
        Texts(bar.Second).ShouldBe(SecondSessionsRow);
    }

    [Fact]
    public void NameTheKeysForATerminalSessionNeverSentAMessage()
    {
        KeyBar bar = Wording.SessionsBar(Selecting([Running, EmptyTerminal], EmptyTerminal), updateKnown: false);

        Texts(bar.First).ShouldBe(["b Adopt", "d Close tab", "m Memory", "C Close empty", "n New session"]);
    }

    [Fact]
    public void LeaveDeleteOutForATerminalSessionWithMessages()
    {
        KeyBar bar = Wording.SessionsBar(Selecting([BusyTerminal], BusyTerminal), updateKnown: false);

        Texts(bar.First).ShouldBe(["b Adopt", "m Memory", "n New session"]);
    }

    [Fact]
    public void OfferResumeWhileAStoppedSessionIsListedAndLeaveStopAndMemoryOffAStoppedRow()
    {
        KeyBar onRunning = Wording.SessionsBar(Selecting([Running, Stopped], Running), updateKnown: false);
        KeyBar onStopped = Wording.SessionsBar(Selecting([Running, Stopped], Stopped), updateKnown: false);

        Texts(onRunning.First).ShouldBe(["Enter Attach", "o Open tab", "x Stop", "d Delete", "r Rename", "m Memory", "R Resume", "n New session"]);
        Texts(onStopped.First).ShouldBe(["Enter Attach", "o Open tab", "d Delete", "r Rename", "R Resume", "n New session"]);
    }

    [Fact]
    public void EndTheFirstRowWithUpdateOnlyWhenAReleaseIsKnown()
    {
        Wording.SessionsBar(Selecting([Running], Running), updateKnown: true).First[^1].ShouldBe(Wording.SessionsBar(Selecting([Running], Running), updateKnown: true).First.Single(item => item.Key == "U"));
        Texts(Wording.SessionsBar(Selecting([Running], Running), updateKnown: true).First)[^1].ShouldBe("U Update");
        Wording.WorktreesBar(Loaded(WorktreesViewState.Empty), updateKnown: true).First[^1].Key.ShouldBe("U");
        Wording.SessionsBar(Selecting([Running], Running), updateKnown: false).First.ShouldNotContain(item => item.Key == "U");
    }

    [Fact]
    public void OfferOnlyWhatNeedsNoRowWhenNothingIsSelected()
    {
        KeyBar bar = Wording.SessionsBar(SessionsViewState.Empty, updateKnown: false);

        Texts(bar.First).ShouldBe(["n New session"]);
        Texts(bar.Second).ShouldBe(SecondSessionsRow);
    }

    [Fact]
    public void NameTheWorktreesKeys()
    {
        KeyBar bar = Wording.WorktreesBar(Loaded(WorktreesViewState.Empty), updateKnown: false);

        Texts(bar.First).ShouldBe(["d Remove", "D Force remove", "P Prune", "s Start session", "o Open shell", "Enter Go to session"]);
        Texts(bar.Second).ShouldBe(["c Copy path", "e Open folder", "v Solution", "p Open PR", "/ Filter", "? Help", "! Report", "q Quit"]);
    }

    [Fact]
    public void NameTheConfigKeys()
    {
        KeyBar bar = Wording.ConfigBar(ConfigLoaded, updateKnown: false);

        Texts(bar.First).ShouldBe(["Enter Edit"]);
        Texts(bar.Second).ShouldBe(["c Copy", "e Open folder", "/ Filter", "? Help", "! Report", "q Quit"]);
    }

    [Fact]
    public void AddUpdateToTheConfigBarWhenAReleaseIsKnown()
    {
        KeyBar bar = Wording.ConfigBar(ConfigLoaded.With(ConfigLoaded.List.Select("worktree-root")), updateKnown: true);

        Texts(bar.First).ShouldBe(["Enter Edit", "U Update"]);
    }

    private static readonly ConfigViewState ConfigLoaded = ConfigView.Reduce(
        ConfigViewState.Empty,
        new ConfigRowsLoaded(ConfigRows.For(Settings.Resolve([FolderPath.From(@"C:\Users\me\source\repos"), FolderPath.From(@"D:\work")], worktreeRoot: null, EditorChoice.VsCode, defaultBase: null, FolderPath.From(@"C:\Users\me")), facts: null))).State;

    [Fact]
    public void PlaceEachItemOneCellFromTheEdgeAndTwoCellsAfterTheLast()
    {
        KeyBar bar = Wording.SessionsBar(Selecting([Running], Running), updateKnown: false);

        bar.First[0].X.ShouldBe(1);
        bar.First[1].X.ShouldBe(1 + "Enter Attach".Length + 2);
        bar.First.ShouldAllBe(item => item.Tip.Length > 0);
    }

    [Fact]
    public void FitTheKeyBarAt120ForEveryRowKind()
    {
        SessionRow[] everyKind = [Running, Stopped, EmptyTerminal, BusyTerminal];
        foreach (bool updateKnown in new[] { false, true })
        {
            foreach (SessionRow selected in everyKind)
            {
                KeyBar bar = Wording.SessionsBar(Selecting(everyKind, selected), updateKnown);
                bar.Width.ShouldBeLessThanOrEqualTo(MostCells, $"{selected.Session.Name} with update {updateKnown}");
            }

            Wording.WorktreesBar(Loaded(WorktreesViewState.Empty), updateKnown).Width.ShouldBeLessThanOrEqualTo(MostCells);
            foreach (ConfigRow selected in ConfigLoaded.Rows)
            {
                Wording.ConfigBar(ConfigLoaded.With(ConfigLoaded.List.Select(selected.Key)), updateKnown).Width.ShouldBeLessThanOrEqualTo(MostCells);
            }
        }
    }

    private static string[] Texts(IReadOnlyList<KeyBarItem> items) => [.. items.Select(item => $"{item.Key} {item.Word}")];

    private static SessionsViewState Selecting(SessionRow[] rows, SessionRow selected)
    {
        SessionsViewState loaded = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded(rows)).State;

        return SessionsView.Reduce(loaded, new SelectSession(selected.Session.SessionId)).State;
    }

    private static WorktreesViewState Loaded(WorktreesViewState state) => WorktreesView.Reduce(state, new WorktreeRowsLoaded([])).State;

    private static SessionRow Background(string id, string name, DisplayState state, int? processId)
    {
        BackgroundSession session = new(SessionId.From(id), JobId.From("81cdb0f9"), name, FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), DateTimeOffset.UnixEpoch, state, processId);

        return new SessionRow(session, "Northwind.Orders", BranchName.From("main"), HasLiveTab: false, HasTranscript: true);
    }

    private static SessionRow Terminal(string id, string name, bool hasTranscript)
    {
        InteractiveSession session = new(SessionId.From(id), name, FolderPath.From(@"C:\Users\me\source\repos"), DateTimeOffset.UnixEpoch, DisplayState.Idle, ProcessId: 200);

        return new SessionRow(session, "repos", Branch: null, HasLiveTab: false, hasTranscript);
    }
}