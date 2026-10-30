using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WordingOfScreensShould
{
    [Fact]
    public void TitleTheTabVantage()
    {
        Wording.TabTitle.ShouldBe("Vantage");
    }

    [Theory]
    [InlineData(DialogName.Keys, "Vantage (Keys)")]
    [InlineData(DialogName.NewSession, "Vantage (New session)")]
    [InlineData(DialogName.Rename, "Vantage (Rename)")]
    [InlineData(DialogName.Resume, "Vantage (Resume)")]
    [InlineData(DialogName.Settings, "Vantage (Settings)")]
    [InlineData(DialogName.DeleteSession, "Vantage (Delete session)")]
    [InlineData(DialogName.DeleteBranch, "Vantage (Delete branch)")]
    [InlineData(DialogName.CloseEmpty, "Vantage (Close empty)")]
    [InlineData(DialogName.ForceRemove, "Vantage (Force remove)")]
    [InlineData(DialogName.OpenSolution, "Vantage (Open solution)")]
    [InlineData(DialogName.Update, "Vantage (Update)")]
    [InlineData(DialogName.Memory, "Vantage (Memory)")]
    [InlineData(DialogName.ProblemReport, "Vantage (Report a problem)")]
    public void NameEachDialogInTheTabTitle(DialogName dialog, string title)
    {
        Wording.TabTitleDuring(dialog).ShouldBe(title);
    }

    [Theory]
    [InlineData(DisplayStateKind.NeedsInput, "◐")]
    [InlineData(DisplayStateKind.Working, "●")]
    [InlineData(DisplayStateKind.Idle, "○")]
    [InlineData(DisplayStateKind.Completed, "✓")]
    [InlineData(DisplayStateKind.Failed, "✗")]
    [InlineData(DisplayStateKind.Stopped, "■")]
    [InlineData(DisplayStateKind.Unknown, "■")]
    public void GiveEveryStateAGlyph(DisplayStateKind kind, string glyph)
    {
        Wording.StateGlyph(kind).ShouldBe(glyph);
    }

    [Fact]
    public void HaveATabTitleForEveryDialog()
    {
        Enum.GetValues<DialogName>().Length.ShouldBe(13, "a new dialog needs its own NameEachDialogInTheTabTitle case");
    }

    [Theory]
    [InlineData(0, "0 MB")]
    [InlineData(29, "29 MB")]
    [InlineData(903, "903 MB")]
    [InlineData(1234, "1.2 GB")]
    [InlineData(12595, "12.3 GB")]
    public void WriteMemoryInWholeMegabytesThenGigabytesToOneDecimal(long megabytes, string expected)
    {
        Wording.Memory(MemorySize.FromMegabytes(megabytes)).ShouldBe(expected);
    }

    [Fact]
    public void RoundMemoryToTheNearestMegabyte()
    {
        Wording.Memory(MemorySize.FromBytes(29 * MemorySize.BytesPerMegabyte + 600_000)).ShouldBe("30 MB");
        Wording.MemoryCell(MemorySize.FromMegabytes(296)).ShouldBe("296 MB");
        Wording.MemoryCell(null).ShouldBe("-");
    }

    [Fact]
    public void ShowALocalBuildAndAnUnreadableClaudeVersionInTheSettingsLine()
    {
        Wording.SettingsLine(new ToolVersion("0.5.0-local"), new ClaudeCodeVersion.Unknown(), Roots(@"C:\Users\me\source\repos"), new UpdateStatus.Off(), VantageMemory, width: 200)
            .ShouldBe(@"Vantage 0.5.0-local (29 MB) · Claude unknown · Repository roots C:\Users\me\source\repos · Worktree root C:\Users\me\source\worktrees");
    }

    private static MemorySize VantageMemory => MemorySize.FromMegabytes(29);

    private static Settings Roots(params string[] roots) => Settings.Resolve([.. roots.Select(root => FolderPath.From(root))], worktreeRoot: null, EditorChoice.VsCode, defaultBase: null, FolderPath.From(@"C:\Users\me"));

    [Fact]
    public void DescribeTableTitles()
    {
        Wording.Describe(new TableTitle("session", Shown: 7, Total: 7, Filter: string.Empty, Selected: "fix-123")).ShouldBe("7 sessions, selected: fix-123");
        Wording.Describe(new TableTitle("session", Shown: 2, Total: 7, Filter: "templates", Selected: "templates-sync")).ShouldBe("2 of 7 sessions match \"templates\", selected: templates-sync");
        Wording.Describe(new TableTitle("session", Shown: 1, Total: 1, Filter: string.Empty, Selected: "fix-123")).ShouldBe("1 session, selected: fix-123");
        Wording.Describe(new TableTitle("session", Shown: 0, Total: 0, Filter: string.Empty, Selected: null)).ShouldBe("0 sessions");
        Wording.Describe(new TableTitle("worktree", Shown: 12, Total: 12, Filter: string.Empty, Selected: "Northwind.Orders-fix-123")).ShouldBe("12 worktrees, selected: Northwind.Orders-fix-123");
    }

    [Fact]
    public void DescribeAFilterThatMatchesNothingOrTheOnlyRow()
    {
        Wording.Describe(new TableTitle("worktree", Shown: 0, Total: 12, Filter: "zzz", Selected: null)).ShouldBe("0 of 12 worktrees match \"zzz\"");
        Wording.Describe(new TableTitle("session", Shown: 1, Total: 1, Filter: "fix", Selected: "fix-123")).ShouldBe("1 of 1 session matches \"fix\", selected: fix-123");
    }

    [Fact]
    public void BuildTheSettingsLineWholeWhenItFits()
    {
        Wording.SettingsLine(new ToolVersion("0.18.0"), new ClaudeCodeVersion.Reported("2.1.310"), Roots(@"C:\Users\me\source\repos"), new UpdateStatus.UpToDate(), MemorySize.FromMegabytes(35), width: 128)
            .ShouldBe(@"Vantage 0.18.0 (35 MB) · Claude 2.1.310 · Repository roots C:\Users\me\source\repos · Worktree root C:\Users\me\source\worktrees");
    }

    [Fact]
    public void CutTheRepositoryRootsFirstToFitTheLine()
    {
        string line = Wording.SettingsLine(new ToolVersion("0.18.0"), new ClaudeCodeVersion.Reported("2.1.310"), Roots(@"C:\Users\me\source\repos"), new UpdateStatus.UpToDate(), MemorySize.FromMegabytes(35), width: 120);

        line.ShouldBe(@"Vantage 0.18.0 (35 MB) · Claude 2.1.310 · Repository roots C:\Users\me\sou… · Worktree root C:\Users\me\source\worktrees");
        line.Length.ShouldBe(120);
    }

    [Fact]
    public void CutTheWorktreeRootNextAndKeepTheNoticeWhole()
    {
        UpdateStatus available = new UpdateStatus.Available(ReleaseVersion.TryParse("0.18.0")!, ReleaseVersion.TryParse("0.19.0")!);

        string narrow = Wording.SettingsLine(new ToolVersion("0.18.0"), new ClaudeCodeVersion.Reported("2.1.310"), Roots(@"C:\Users\me\source\repos"), available, MemorySize.FromMegabytes(35), width: 120);
        string wide = Wording.SettingsLine(new ToolVersion("0.18.0"), new ClaudeCodeVersion.Reported("2.1.310"), Roots(@"C:\Users\me\source\repos"), available, MemorySize.FromMegabytes(35), width: 160);

        narrow.ShouldBe(@"Vantage 0.18.0 (35 MB; 0.19.0 available, U to update) · Claude 2.1.310 · Repository roots … · Worktree root C:\Users\me…");
        narrow.Length.ShouldBe(120);
        wide.ShouldBe(@"Vantage 0.18.0 (35 MB; 0.19.0 available, U to update) · Claude 2.1.310 · Repository roots C:\Users\me\source\repos · Worktree root C:\Users\me\source\worktrees");
    }

    [Fact]
    public void JoinSeveralRepositoryRootsAndShowAClaudeWarning()
    {
        ClaudeCodeVersion old = new ClaudeCodeVersion.BelowMinimum(ReleaseVersion.TryParse("2.1.302")!, ReleaseVersion.TryParse("2.1.309")!);

        Wording.SettingsLine(new ToolVersion("0.18.0"), old, Roots(@"C:\Users\me\source\repos", @"D:\work"), new UpdateStatus.UpToDate(), MemorySize.FromMegabytes(35), width: 200)
            .ShouldBe(@"Vantage 0.18.0 (35 MB) · Claude 2.1.302 (below 2.1.309, update Claude Code) · Repository roots C:\Users\me\source\repos; D:\work · Worktree root C:\Users\me\source\worktrees");
    }

    [Fact]
    public void CountSessionsByStateNeedsInputFirst()
    {
        SessionRow[] rows =
        [
            SessionIn(DisplayState.Working, 0), SessionIn(DisplayState.NeedsInput, 1), SessionIn(DisplayState.Idle, 2),
            SessionIn(DisplayState.Completed, 3), SessionIn(DisplayState.NeedsInput, 4), SessionIn(DisplayState.Stopped, 5),
        ];

        Wording.SessionCounts(rows).ShouldBe(
        [
            new CountPart("◐ 2 need input", CountTone.Amber), new CountPart("● 1 working", CountTone.Cyan), new CountPart("○ 1 idle", CountTone.Plain),
            new CountPart("✓ 1 completed", CountTone.Green), new CountPart("■ 1 stopped", CountTone.Grey),
        ]);
        Wording.SessionCounts([SessionIn(DisplayState.NeedsInput, 0)]).ShouldBe([new CountPart("◐ 1 needs input", CountTone.Amber)]);
        Wording.SessionCounts([]).ShouldBeEmpty();
    }

    [Fact]
    public void CountWorktreesByHealthThenTheWorkAtRisk()
    {
        WorktreeRow[] rows =
        [
            .. Enumerable.Range(0, 5).Select(index => WorktreeIn(WorktreeHealthKind.Ok, index, dirty: index < 2 ? 3 : 0, unpushed: index is 1 or 2 ? 4 : 0)),
            WorktreeIn(WorktreeHealthKind.Locked, 5), WorktreeIn(WorktreeHealthKind.Broken, 6),
        ];

        Wording.WorktreeCounts(rows).ShouldBe(
        [
            new CountPart("5 ok", CountTone.Plain), new CountPart("1 locked", CountTone.Amber), new CountPart("1 broken", CountTone.Red),
            new CountPart("2 with changes", CountTone.Amber), new CountPart("2 with unpushed commits", CountTone.Amber),
        ]);
        Wording.WorktreeCounts([WorktreeIn(WorktreeHealthKind.Ok, 0)]).ShouldBe([new CountPart("1 ok", CountTone.Plain)]);
    }

    [Fact]
    public void TitleEachTabWithItsCountOnceLoaded()
    {
        Wording.TabTitleFor(ConsoleTab.Sessions, 6).ShouldBe("Sessions (6)");
        Wording.TabTitleFor(ConsoleTab.Worktrees, 7).ShouldBe("Worktrees (7)");
        Wording.TabTitleFor(ConsoleTab.Worktrees, null).ShouldBe("Worktrees");
        Wording.TabTitleFor(ConsoleTab.Sessions, 0).ShouldBe("Sessions (0)");
    }

    private static WorktreeRow WorktreeIn(WorktreeHealthKind health, int index, int dirty = 0, int unpushed = 0)
    {
        return new WorktreeRow(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), FolderPath.From($@"C:\Users\me\source\worktrees\Northwind.Orders-fix-{index}"), BranchName.From($"fix-{index}"), new WorktreeHealth(health, new HealthReason.None()), dirty, unpushed, null, null, null, 0, OwnerExists: true, FolderExists: true);
    }

    private static SessionRow SessionIn(DisplayState state, int index)
    {
        BackgroundSession session = new(SessionId.From($"sess-{index}"), JobId.From($"81cdb0f{index}"), $"fix-{index}", FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), DateTimeOffset.UnixEpoch, state);

        return new SessionRow(session, "Northwind.Orders", BranchName.From("main"), HasLiveTab: false, HasTranscript: true);
    }

    [Fact]
    public void IgnoreAFilterOfOnlySpaces()
    {
        Wording.Describe(new TableTitle("session", Shown: 3, Total: 3, Filter: "  ", Selected: null)).ShouldBe("3 sessions");
    }
}