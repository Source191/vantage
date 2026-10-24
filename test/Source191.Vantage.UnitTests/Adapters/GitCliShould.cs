using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Ports;
using Source191.Vantage.UnitTests.TestDoubles;

namespace Source191.Vantage.UnitTests.Adapters;

public sealed class GitCliShould
{
    private const string Repository = @"C:\repos\Northwind.Orders";
    private const string Worktree = @"C:\repos\Northwind.Orders-fix-123";

    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));

    private GitCli Git => new(processes);

    private ProcessRequest Only => processes.CapturedRequests.Single();

    [Fact]
    public async Task ListWorktreesWithPorcelainOutputInTheRepository()
    {
        processes.AnswerAll(new ProcessResult(0, "worktree C:/repos/Northwind.Orders\nHEAD abc\nbranch refs/heads/main\n", string.Empty));

        IReadOnlyList<GitWorktreeEntry> entries = await Git.ListWorktreesAsync(Repository, CancellationToken.None);

        Only.ShouldSatisfyAllConditions(
            request => request.FileName.ShouldBe("git"),
            request => request.WorkingDirectory.ShouldBe(Repository),
            request => request.Arguments.ShouldBe(["worktree", "list", "--porcelain"]));
        entries.Single().Branch.ShouldBe("main");
    }

    [Fact]
    public async Task AddAWorktreeOnANewBranchFromTheBase()
    {
        await Git.AddWorktreeAsync(Repository, Worktree, "fix-123", "origin/main", CancellationToken.None);

        Only.Arguments.ShouldBe(["worktree", "add", Worktree, "-b", "fix-123", "origin/main"]);
    }

    [Fact]
    public async Task ThrowWhenGitRefusesToAddAWorktree()
    {
        processes.AnswerAll(new ProcessResult(128, string.Empty, "fatal: 'fix-123' is already checked out"));

        GitException exception = await Should.ThrowAsync<GitException>(() => Git.AddWorktreeAsync(Repository, Worktree, "fix-123", "origin/main", CancellationToken.None));

        exception.Message.ShouldContain("already checked out");
    }

    [Theory]
    [InlineData(false, new[] { "worktree", "remove", Worktree })]
    [InlineData(true, new[] { "worktree", "remove", "--force", Worktree })]
    public async Task RemoveAWorktreeForcingOnlyWhenAsked(bool force, string[] expected)
    {
        await Git.RemoveWorktreeAsync(Repository, Worktree, force, CancellationToken.None);

        Only.Arguments.ShouldBe(expected);
    }

    [Fact]
    public async Task CountPrunedRegistrationsByComparingTheListingsEitherSide()
    {
        const string two = "worktree C:/repos/Northwind.Orders\nHEAD a\nbranch refs/heads/main\n\nworktree C:/repos/Northwind.Orders-old\nHEAD b\nbranch refs/heads/old\n";
        const string one = "worktree C:/repos/Northwind.Orders\nHEAD a\nbranch refs/heads/main\n";
        processes.AnswerInTurn(
            request => request.Arguments.Contains("list"),
            new ProcessResult(0, two, string.Empty),
            new ProcessResult(0, one, string.Empty));

        int pruned = await Git.PruneWorktreesAsync(Repository, CancellationToken.None);

        processes.CapturedRequests[1].Arguments.ShouldBe(["worktree", "prune", "--expire", "now"]);
        pruned.ShouldBe(1);
    }

    [Fact]
    public async Task CountChangedAndUntrackedFilesFromPorcelainStatus()
    {
        processes.AnswerAll(new ProcessResult(0, " M src/a.cs\n?? src/b.cs\n M src/c.cs\n", string.Empty));

        int changed = await Git.CountChangedFilesAsync(Worktree, CancellationToken.None);

        Only.ShouldSatisfyAllConditions(
            request => request.WorkingDirectory.ShouldBe(Worktree),
            request => request.Arguments.ShouldBe(["status", "--porcelain"]));
        changed.ShouldBe(3);
    }

    [Fact]
    public async Task CountCommitsNotOnAnyRemoteBranch()
    {
        processes.AnswerAll(new ProcessResult(0, "4\n", string.Empty));

        int unpushed = await Git.CountUnpushedCommitsAsync(Worktree, CancellationToken.None);

        Only.Arguments.ShouldBe(["rev-list", "--count", "HEAD", "--not", "--remotes"]);
        unpushed.ShouldBe(4);
    }

    [Fact]
    public async Task ReadTheLastCommitTimeInIsoFormat()
    {
        processes.AnswerAll(new ProcessResult(0, "2026-10-22T09:27:00+01:00\n", string.Empty));

        DateTimeOffset? lastCommit = await Git.LastCommitAtAsync(Worktree, CancellationToken.None);

        Only.Arguments.ShouldBe(["log", "-1", "--format=%cI"]);
        lastCommit.ShouldBe(new DateTimeOffset(2026, 10, 22, 9, 27, 0, TimeSpan.FromHours(1)));
    }

    [Fact]
    public async Task ReturnNoLastCommitForARepositoryWithoutCommits()
    {
        processes.AnswerAll(new ProcessResult(128, string.Empty, "fatal: your current branch 'main' does not have any commits yet"));

        DateTimeOffset? lastCommit = await Git.LastCommitAtAsync(Worktree, CancellationToken.None);

        lastCommit.ShouldBeNull();
    }

    [Theory]
    [InlineData(false, "-d")]
    [InlineData(true, "-D")]
    public async Task DeleteABranchForcingOnlyWhenAsked(bool force, string flag)
    {
        await Git.DeleteBranchAsync(Repository, "fix-123", force, CancellationToken.None);

        Only.Arguments.ShouldBe(["branch", flag, "fix-123"]);
    }

    [Fact]
    public async Task ReadTheDefaultBranchFromOriginsHead()
    {
        processes.AnswerAll(new ProcessResult(0, "origin/main\n", string.Empty));

        string? defaultBranch = await Git.DefaultRemoteBranchAsync(Repository, CancellationToken.None);

        Only.Arguments.ShouldBe(["symbolic-ref", "--short", "refs/remotes/origin/HEAD"]);
        defaultBranch.ShouldBe("origin/main");
    }

    [Fact]
    public async Task ReturnNoDefaultBranchWhenOriginHasNoHead()
    {
        processes.AnswerAll(new ProcessResult(128, string.Empty, "fatal: ref refs/remotes/origin/HEAD is not a symbolic ref"));

        string? defaultBranch = await Git.DefaultRemoteBranchAsync(Repository, CancellationToken.None);

        defaultBranch.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task DecideMergedByWhetherTheBranchIsAnAncestor(int exitCode, bool expected)
    {
        processes.AnswerAll(new ProcessResult(exitCode, string.Empty, string.Empty));

        bool merged = await Git.IsMergedAsync(Repository, "fix-123", "origin/main", CancellationToken.None);

        Only.Arguments.ShouldBe(["merge-base", "--is-ancestor", "fix-123", "origin/main"]);
        merged.ShouldBe(expected);
    }

    [Fact]
    public async Task ReportNoCurrentBranchWhenDetached()
    {
        processes.AnswerAll(new ProcessResult(0, "\n", string.Empty));

        string? branch = await Git.CurrentBranchAsync(Worktree, CancellationToken.None);

        Only.Arguments.ShouldBe(["branch", "--show-current"]);
        branch.ShouldBeNull();
    }

    [Fact]
    public async Task ReportNoRepositoryRootOutsideARepository()
    {
        processes.AnswerAll(new ProcessResult(128, string.Empty, "fatal: not a git repository"));

        string? root = await Git.RepositoryRootAsync(@"C:\temp", CancellationToken.None);

        Only.Arguments.ShouldBe(["rev-parse", "--show-toplevel"]);
        root.ShouldBeNull();
    }

    [Fact]
    public async Task ReportTheRepositoryRootAsAWindowsPath()
    {
        processes.AnswerAll(new ProcessResult(0, "C:/repos/Northwind.Orders\n", string.Empty));

        string? root = await Git.RepositoryRootAsync(@"C:\repos\Northwind.Orders\src", CancellationToken.None);

        root.ShouldBe(@"C:\repos\Northwind.Orders");
    }

    [Fact]
    public async Task ListIgnoredFilesAsRepositoryRelativePaths()
    {
        processes.AnswerAll(new ProcessResult(0, "src/appsettings.local.json\nsecrets/key.txt\n", string.Empty));

        IReadOnlyList<string> ignored = await Git.ListIgnoredFilesAsync(Repository, CancellationToken.None);

        Only.Arguments.ShouldBe(["ls-files", "--others", "--ignored", "--exclude-standard"]);
        ignored.ShouldBe(["src/appsettings.local.json", "secrets/key.txt"]);
    }

    [Fact]
    public async Task FindTheMainCheckoutOfALinkedWorktreeFromTheCommonDir()
    {
        processes.AnswerAll(new ProcessResult(0, "C:/repos/Northwind.Orders/.git\n", string.Empty));

        string? main = await Git.MainCheckoutOfAsync(Worktree, CancellationToken.None);

        Only.Arguments.ShouldBe(["rev-parse", "--path-format=absolute", "--git-common-dir"]);
        main.ShouldBe(@"C:\repos\Northwind.Orders");
    }
}