using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class ChildCommandShould
{
    [Theory]
    [InlineData("git", new[] { "worktree", "list", "--porcelain" }, "git worktree list")]
    [InlineData("git", new[] { "branch", "-d", "fix-123" }, "git branch")]
    [InlineData("git", new[] { "worktree", "add", @"C:\Users\me\source\worktrees\Northwind.Orders-list", "-b", "list" }, "git worktree add")]
    [InlineData(@"C:\Users\me\.local\bin\claude.exe", new[] { "--bg", "-n", "Fix flaky tests" }, "claude --bg")]
    [InlineData(@"C:\Users\me\.local\bin\claude.exe", new[] { "--bg", "--resume", "81cdb0f9-0000", "-n", "Fix" }, "claude --bg --resume")]
    [InlineData("claude", new[] { "agents", "--json", "--all" }, "claude agents --json --all")]
    [InlineData("gh", new[] { "pr", "list", "--repo", "northwind/orders" }, "gh pr list")]
    [InlineData(@"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd", new[] { "repos", "pr", "list", "--org" }, "az repos pr list")]
    [InlineData("wt.exe", new[] { "-w", "0", "new-tab", "--title", "Fix" }, "wt -w 0 new-tab")]
    [InlineData("pwsh.exe", new[] { "-NoProfile", "-NonInteractive", "-EncodedCommand", "QQBCAEMA" }, "pwsh")]
    public void KeepOnlyTheListedLeadingWords(string program, string[] arguments, string expected)
    {
        ChildCommand.For(new ProcessRequest(program, arguments, null)).ToString().ShouldBe(expected);
    }

    [Fact]
    public void NameAProgramItHasNoWordForAsAnotherProgram()
    {
        ProcessRequest request = new(@"C:\Users\me\tools\MyTool.exe", [@"C:\Users\me\source\repos\Northwind.Orders"], null);

        ChildCommand.For(request).ToString().ShouldBe("another program");
    }

    [Fact]
    public void StopAtThreeWords()
    {
        ProcessRequest request = new("git", ["worktree", "list", "worktree", "list"], null);

        ChildCommand.For(request).ToString().ShouldBe("git worktree list worktree");
    }

    [Fact]
    public void EqualTheSameCommand()
    {
        ChildCommand.For(new ProcessRequest("git", ["status", "--porcelain"], "a")).ShouldBe(ChildCommand.For(new ProcessRequest("git.exe", ["status"], "b")));
    }
}
