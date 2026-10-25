using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WordingOfScreensShould
{
    [Fact]
    public void TitleTheTabWithTheVersion()
    {
        Wording.TabTitle(new ToolVersion("0.5.0")).ShouldBe("Vantage (0.5.0)");
        Wording.TabTitle(new ToolVersion("0.5.0-local")).ShouldBe("Vantage (0.5.0-local)");
    }

    [Fact]
    public void ShowVersionsAndRootsInTheHeader()
    {
        Wording.HeaderValues(new ToolVersion("0.5.0"), "2.1.310", Roots(@"C:\Users\me\source\repos", @"D:\work"), new UpdateStatus.UpToDate())
            .ShouldBe("0.5.0\n2.1.310\nC:\\Users\\me\\source\\repos; D:\\work\nC:\\Users\\me\\source\\worktrees");
    }

    [Fact]
    public void NameAnAvailableUpdateInTheHeader()
    {
        UpdateStatus available = new UpdateStatus.Available(ReleaseVersion.TryParse("0.9.0")!, ReleaseVersion.TryParse("0.10.0")!);

        Wording.HeaderValues(new ToolVersion("0.9.0"), "2.1.310", Roots(@"C:\Users\me\source\repos"), available)
            .ShouldStartWith("0.9.0 (0.10.0 available, U to update)\n2.1.310\n");
    }

    [Fact]
    public void ShowAnUnreadableClaudeVersionAsUnknown()
    {
        Wording.HeaderValues(new ToolVersion("0.5.0-local"), claudeVersion: null, Roots(@"C:\Users\me\source\repos"), new UpdateStatus.Off())
            .ShouldBe("0.5.0-local\nunknown\nC:\\Users\\me\\source\\repos\nC:\\Users\\me\\source\\worktrees");
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