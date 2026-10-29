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
    [InlineData(DialogName.ProblemReport, "Vantage (Report a problem)")]
    public void NameEachDialogInTheTabTitle(DialogName dialog, string title)
    {
        Wording.TabTitleDuring(dialog).ShouldBe(title);
    }

    [Fact]
    public void HaveATabTitleForEveryDialog()
    {
        Enum.GetValues<DialogName>().Length.ShouldBe(12, "a new dialog needs its own NameEachDialogInTheTabTitle case");
    }

    [Fact]
    public void ShowVersionsAndRootsInTheHeader()
    {
        Wording.HeaderValues(new ToolVersion("0.5.0"), new ClaudeCodeVersion.Reported("2.1.310"), Roots(@"C:\Users\me\source\repos", @"D:\work"), new UpdateStatus.UpToDate())
            .ShouldBe("0.5.0\n2.1.310\nC:\\Users\\me\\source\\repos; D:\\work\nC:\\Users\\me\\source\\worktrees");
    }

    [Fact]
    public void NameAnAvailableUpdateInTheHeader()
    {
        UpdateStatus available = new UpdateStatus.Available(ReleaseVersion.TryParse("0.9.0")!, ReleaseVersion.TryParse("0.10.0")!);

        Wording.HeaderValues(new ToolVersion("0.9.0"), new ClaudeCodeVersion.Reported("2.1.310"), Roots(@"C:\Users\me\source\repos"), available)
            .ShouldStartWith("0.9.0 (0.10.0 available, U to update)\n2.1.310\n");
    }

    [Fact]
    public void ShowAnUnreadableClaudeVersionAsUnknown()
    {
        Wording.HeaderValues(new ToolVersion("0.5.0-local"), new ClaudeCodeVersion.Unknown(), Roots(@"C:\Users\me\source\repos"), new UpdateStatus.Off())
            .ShouldBe("0.5.0-local\nunknown\nC:\\Users\\me\\source\\repos\nC:\\Users\\me\\source\\worktrees");
    }

    [Fact]
    public void ShowAClaudeVersionBelowTheMinimumWithTheWarning()
    {
        ClaudeCodeVersion old = new ClaudeCodeVersion.BelowMinimum(ReleaseVersion.TryParse("2.1.302")!, ReleaseVersion.TryParse("2.1.309")!);

        Wording.HeaderValues(new ToolVersion("0.13.0"), old, Roots(@"C:\Users\me\source\repos"), new UpdateStatus.UpToDate())
            .ShouldStartWith("0.13.0\n2.1.302 (below 2.1.309, update Claude Code)\n");
    }

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
    public void IgnoreAFilterOfOnlySpaces()
    {
        Wording.Describe(new TableTitle("session", Shown: 3, Total: 3, Filter: "  ", Selected: null)).ShouldBe("3 sessions");
    }
}