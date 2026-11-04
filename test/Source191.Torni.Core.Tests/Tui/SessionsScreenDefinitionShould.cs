using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Adapters.Git;
using Source191.Torni.Application;
using Source191.Torni.Application.Detail;
using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;
using Source191.Torni.Tui;

namespace Source191.Torni.Core.Tests.Tui;

public sealed class SessionsScreenDefinitionShould
{
    private static readonly FolderPath Folder = FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf");

    private readonly FakeTimeProvider clock = new(Fixtures.Noon);
    private readonly System.Text.StringBuilder terminal = new();
    private readonly SessionsScreenDefinition definition;

    public SessionsScreenDefinitionShould()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
        SessionActivity activity = new(clock);
        DetailLoader loader = new(new GitCli(processes, "git"), new InMemoryState(), activity, new DiagnosticLog(new FakeTimeProvider(Fixtures.Noon), []));
        FolderLabels folders = new(TestSettings.Current(TestSettings.For([@"C:\Users\me\source\repos"], @"D:\work")), FolderPath.From(@"C:\Users\me"));
        definition = new SessionsScreenDefinition(new StateTransitionNotifier(new StringWriter(terminal)), activity, @"C:\Users\me\.local\bin\claude.exe", folders, new SessionDetails(loader, clock));
    }

    [Fact]
    public void ListItsColumnsAsWordingDoesWithRepositoryElastic()
    {
        definition.Columns.Select(column => column.Help).ShouldBe(Wording.SessionsColumns);
        definition.Columns.Single(column => column.Fit is ColumnFit.Elastic).Help.ShouldBeSameAs(Wording.RepositoryColumn);
    }

    [Fact]
    public void CapNameAtTwentyEightCells()
    {
        TableColumn<SessionRow> name = definition.Columns.Single(column => column.Help == Wording.NameColumn);

        name.Fit.ShouldBe(new ColumnFit.Capped(28));
    }

    [Fact]
    public void RightAlignAge()
    {
        TableColumn<SessionRow> age = definition.Columns.Single(column => column.Help == Wording.AgeColumn);

        age.Fit.ShouldBe(new ColumnFit.RightAligned());
        age.Cell(Row(new Session.Background(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), Listed.As("fix-123"), Folder, Fixtures.Noon.AddMinutes(-5), DisplayState.Idle, new JobProcess.None()), new LiveTab.None()), Fixtures.Noon, int.MaxValue).ShouldBe("5m");
    }

    [Fact]
    public void PaintOnlyTheTabCell()
    {
        SessionRow open = Row(new Session.Background(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), Listed.As("fix-123"), Folder, Fixtures.Noon, DisplayState.Idle, new JobProcess.None()), new LiveTab.Open(new TabColour("Yellow", "#FFB900")));
        SessionRow plain = Row(new Session.Interactive(SessionId.From("81cdb0f9-0000-4000-8000-000000000002"), Listed.As("plain"), Folder, Fixtures.Noon, DisplayState.Idle, ProcessId: 4242), new LiveTab.None());

        definition.Columns.Where(column => column.Help != Wording.TabColumn).Select(column => column.Paint(open)).ShouldAllBe(paint => paint is CellPaint.None);
        definition.Columns.Single(column => column.Help == Wording.TabColumn).Paint(open).ShouldBeOfType<CellPaint.Fill>();
        definition.Columns.Single(column => column.Help == Wording.TabColumn).Paint(plain).ShouldBe(new CellPaint.Text(Palette.Grey));
    }

    [Fact]
    public void ShowEveryColumnOfARow()
    {
        SessionRow row = Row(new Session.Background(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), Listed.As("fix-123"), Folder, Fixtures.Noon.AddHours(-3), DisplayState.Idle, new JobProcess.None()), new LiveTab.Open(new TabColour("Yellow", "#FFB900")));

        definition.Columns.Select(column => column.Cell(row, Fixtures.Noon, int.MaxValue)).ShouldBe(
        [
            "fix-123",
            Wording.TabBlock,
            $"{Wording.StateGlyph(DisplayStateKind.Idle)} {Wording.StateWord(DisplayState.Idle)}",
            @"~\source\worktrees\Northwind.Billing-invoice-pdf",
            string.Empty,
            "3h00m",
        ]);
    }

    [Fact]
    public void ShowAPlainClaudeWithItsHollowBlock()
    {
        SessionRow row = Row(new Session.Interactive(SessionId.From("81cdb0f9-0000-4000-8000-000000000002"), Listed.As("plain"), Folder, Fixtures.Noon, DisplayState.Working, ProcessId: 4242), new LiveTab.None());

        definition.Columns.Single(column => column.Help == Wording.TabColumn).Cell(row, Fixtures.Noon, int.MaxValue).ShouldBe(Wording.TerminalSwatch);
    }

    [Theory]
    [InlineData(DisplayStateKind.Working)]
    [InlineData(DisplayStateKind.NeedsInput)]
    [InlineData(DisplayStateKind.Idle)]
    [InlineData(DisplayStateKind.Completed)]
    [InlineData(DisplayStateKind.Failed)]
    [InlineData(DisplayStateKind.Stopped)]
    [InlineData(DisplayStateKind.Unknown)]
    public void ColourARowByItsState(DisplayStateKind kind)
    {
        DisplayState state = KnownState(kind);
        SessionRow row = Row(new Session.Background(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), Listed.As("fix-123"), Folder, Fixtures.Noon, state, new JobProcess.None()), new LiveTab.None());

        definition.RowColour(row).ShouldBe(Palette.ForState(kind));
    }

    [Fact]
    public void TellTheNotifierWhenALoadedRowStartsWaiting()
    {
        Session.Background idle = new(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), Listed.As("fix-123"), Folder, Fixtures.Noon, DisplayState.Idle, new JobProcess.None());
        Session.Background waiting = idle with { State = DisplayState.NeedsInput };
        SessionsViewState before = SessionsViewState.Empty with { List = SessionsViewState.Empty.List.Load([Row(idle, new LiveTab.None())]) };

        definition.OnInput(before, new RowsLoaded([Row(waiting, new LiveTab.None())]));

        terminal.ToString().ShouldBe("\a");
    }

    private static DisplayState KnownState(DisplayStateKind kind) => kind switch
    {
        DisplayStateKind.Working => DisplayState.Working,
        DisplayStateKind.NeedsInput => DisplayState.NeedsInput,
        DisplayStateKind.Idle => DisplayState.Idle,
        DisplayStateKind.Completed => DisplayState.Completed,
        DisplayStateKind.Failed => DisplayState.Failed,
        DisplayStateKind.Stopped => DisplayState.Stopped,
        DisplayStateKind.Unknown => new DisplayState.Unknown("odd words"),
    };

    [Fact]
    public void NameItsTab()
    {
        definition.Tab.ShouldBe(ConsoleTab.Sessions);
    }

    [Fact]
    public void RefreshEveryFiveSeconds()
    {
        definition.Refresh.ShouldBe(new TabRefresh.Every(TimeSpan.FromSeconds(5)));
    }

    private static SessionRow Row(Session session, LiveTab tab) => new(session, new SessionLocation.Outside("Northwind.Billing"), new Head.OnBranch(BranchName.From("invoice-pdf")), tab, HasTranscript: true);
}