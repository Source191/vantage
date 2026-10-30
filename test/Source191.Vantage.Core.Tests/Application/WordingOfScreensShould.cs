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
    }

    [Fact]
    public void WriteTheVersionsOnTheOneSettingsRow()
    {
        IReadOnlyList<TextPart> row = Wording.SettingsRow(new ToolVersion("0.18.0"), new ClaudeCodeVersion.Reported("2.1.310"), new UpdateStatus.UpToDate(), MemorySize.FromMegabytes(35));

        row.ShouldBe([new TextPart("Vantage ", TextTone.Orange), new TextPart("0.18.0 (35 MB)", TextTone.Plain), new TextPart(" · ", TextTone.Grey), new TextPart("Claude ", TextTone.Orange), new TextPart("2.1.310", TextTone.Plain)]);
    }

    [Theory]
    [InlineData("0.18.0", "2.1.310", true, false, "Vantage 0.18.0 (35 MB; 0.19.0 available, U to update) · Claude 2.1.310")]
    [InlineData("0.5.0-local", null, false, false, "Vantage 0.5.0-local (35 MB) · Claude unknown")]
    [InlineData("0.18.0", "2.1.302", false, true, "Vantage 0.18.0 (35 MB) · Claude 2.1.302 (below 2.1.309, update Claude Code)")]
    public void KeepTheNoticeAndTheWarningOnTheSettingsRow(string vantage, string? claude, bool updateAvailable, bool belowMinimum, string expected)
    {
        ClaudeCodeVersion version = (claude, belowMinimum) switch
        {
            (null, _) => new ClaudeCodeVersion.Unknown(),
            (_, true) => new ClaudeCodeVersion.BelowMinimum(ReleaseVersion.TryParse(claude)!, ReleaseVersion.TryParse("2.1.309")!),
            _ => new ClaudeCodeVersion.Reported(claude),
        };
        UpdateStatus update = updateAvailable ? new UpdateStatus.Available(ReleaseVersion.TryParse("0.18.0")!, ReleaseVersion.TryParse("0.19.0")!) : new UpdateStatus.UpToDate();

        Text(Wording.SettingsRow(new ToolVersion(vantage), version, update, MemorySize.FromMegabytes(35))).ShouldBe(expected);
    }

    [Fact]
    public void TitleTheTabsWithoutCounts()
    {
        Wording.TabTitleFor(ConsoleTab.Sessions).ShouldBe("Sessions");
        Wording.TabTitleFor(ConsoleTab.Worktrees).ShouldBe("Worktrees");
        Wording.TabTitleFor(ConsoleTab.Config).ShouldBe("Config");
    }

    [Fact]
    public void CountTheSettingsAndTheSetupLeftToDo()
    {
        SetupFacts facts = new(new SetupState.At(@"C:\Users\me\AppData\Local\Vantage\config.json"), new SetupState.NotSetUp(), new SetupState.Enabled(), new SetupState.Disabled(), new SetupState.Never());

        Wording.ConfigCounts(ConfigRows.For(Roots(@"C:\Users\me\source\repos", @"D:\work"), facts)).ShouldBe([new TextPart("6 settings", TextTone.Plain), new TextPart("1 to set up", TextTone.Amber)]);
        Wording.ConfigCounts(ConfigRows.For(Roots(@"C:\Users\me\source\repos"), facts with { TerminalProfiles = new SetupState.Enabled() })).ShouldBe([new TextPart("5 settings", TextTone.Plain)]);
    }

    private static string Text(IReadOnlyList<TextPart> row) => string.Concat(row.Select(part => part.Text));

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
    public void CountSessionsByStateNeedsInputFirst()
    {
        SessionRow[] rows =
        [
            SessionIn(DisplayState.Working, 0), SessionIn(DisplayState.NeedsInput, 1), SessionIn(DisplayState.Idle, 2),
            SessionIn(DisplayState.Completed, 3), SessionIn(DisplayState.NeedsInput, 4), SessionIn(DisplayState.Stopped, 5),
        ];

        Wording.SessionCounts(rows, AutomatedRuns.None).ShouldBe(
        [
            new TextPart("◐ 2 need input", TextTone.Amber), new TextPart("● 1 working", TextTone.Cyan), new TextPart("○ 1 idle", TextTone.Plain),
            new TextPart("✓ 1 completed", TextTone.Green), new TextPart("■ 1 stopped", TextTone.Grey),
        ]);
        Wording.SessionCounts([SessionIn(DisplayState.NeedsInput, 0)], AutomatedRuns.None).ShouldBe([new TextPart("◐ 1 needs input", TextTone.Amber)]);
        Wording.SessionCounts([], AutomatedRuns.None).ShouldBeEmpty();
    }

    [Fact]
    public void EndTheCountsWithTheAutomatedRunsAndTheirMemoryInGrey()
    {
        SessionRow[] rows = [SessionIn(DisplayState.Working, 0), SessionIn(DisplayState.Stopped, 1)];

        Wording.SessionCounts(rows, new AutomatedRuns(1, MemorySize.FromMegabytes(216))).ShouldBe(
        [
            new TextPart("● 1 working", TextTone.Cyan), new TextPart("■ 1 stopped", TextTone.Grey), new TextPart("◇ 1 automated 216 MB", TextTone.Grey),
        ]);
        Wording.SessionCounts(rows, new AutomatedRuns(2, MemorySize.FromMegabytes(431)))[^1].ShouldBe(new TextPart("◇ 2 automated 431 MB", TextTone.Grey));
        Wording.SessionCounts([], new AutomatedRuns(1, MemorySize.FromMegabytes(216))).ShouldBe([new TextPart("◇ 1 automated 216 MB", TextTone.Grey)]);
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
            new TextPart("5 ok", TextTone.Plain), new TextPart("1 locked", TextTone.Amber), new TextPart("1 broken", TextTone.Red),
            new TextPart("2 with changes", TextTone.Amber), new TextPart("2 with unpushed commits", TextTone.Amber),
        ]);
        Wording.WorktreeCounts([WorktreeIn(WorktreeHealthKind.Ok, 0)]).ShouldBe([new TextPart("1 ok", TextTone.Plain)]);
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