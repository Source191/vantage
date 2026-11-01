using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>
/// Drives the real git against throwaway repositories under the temp folder.
/// </summary>
[Trait("Category", "Integration")]
public sealed class GitCliIntegrationShould : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vantage-git-" + Guid.NewGuid().ToString("N"));
    private readonly GitCli git = new(new ProcessRunner(), "git");

    private string Repository => Path.Combine(root, "Northwind.Orders");

    private string Worktree => Path.Combine(root, "Northwind.Orders-fix-123");

    [Fact]
    public async Task ListOnlyTheMainCheckoutOfAFreshRepository()
    {
        await CreateRepositoryWithOneCommitAsync();

        IReadOnlyList<GitWorktreeEntry> entries = await git.ListWorktreesAsync(Repository, TestContext.Current.CancellationToken);

        entries.Single().ShouldSatisfyAllConditions(
            entry => entry.IsMain.ShouldBeTrue(),
            entry => entry.Head.ShouldBe(new Head.OnBranch(BranchName.From("main"))),
            entry => entry.Path.ShouldBe(Repository, StringCompareShould.IgnoreCase));
    }

    [Fact]
    public async Task AddListInspectAndRemoveASiblingWorktree()
    {
        await CreateRepositoryWithOneCommitAsync();

        await git.AddWorktreeAsync(Repository, Worktree, "fix-123", "main", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(Worktree, "new.txt"), "untracked", TestContext.Current.CancellationToken);

        IReadOnlyList<GitWorktreeEntry> entries = await git.ListWorktreesAsync(Repository, TestContext.Current.CancellationToken);
        entries.Count.ShouldBe(2);
        entries[1].Head.ShouldBe(new Head.OnBranch(BranchName.From("fix-123")));
        (await git.CountChangedFilesAsync(Worktree, TestContext.Current.CancellationToken)).ShouldBe(1);
        (await git.CountUnpushedCommitsAsync(Worktree, TestContext.Current.CancellationToken)).ShouldBe(1);
        (await git.LastCommitAtAsync(Worktree, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        (await git.CurrentBranchAsync(Worktree, TestContext.Current.CancellationToken)).ShouldBe("fix-123");
        (await git.MainCheckoutOfAsync(Worktree, TestContext.Current.CancellationToken)).ShouldBe(Repository, StringCompareShould.IgnoreCase);
        (await git.IsMergedAsync(Repository, "fix-123", "main", TestContext.Current.CancellationToken)).ShouldBeTrue();

        await git.RemoveWorktreeAsync(Repository, Worktree, force: true, TestContext.Current.CancellationToken);
        await git.DeleteBranchAsync(Repository, "fix-123", force: false, TestContext.Current.CancellationToken);

        Directory.Exists(Worktree).ShouldBeFalse();
        (await git.ListWorktreesAsync(Repository, TestContext.Current.CancellationToken)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task ListTheUnpushedCommitsNewestFirstUpToTheLimit()
    {
        await CreateRepositoryWithOneCommitAsync();
        foreach (string subject in new[] { "Add the refund rule", "Retry the refund twice" })
        {
            await File.AppendAllTextAsync(Path.Combine(Repository, "README.md"), subject, TestContext.Current.CancellationToken);
            await RunGitAsync("commit", "-q", "-am", subject);
        }

        IReadOnlyList<UnpushedCommit> commits = await git.UnpushedCommitsAsync(Repository, 2, TestContext.Current.CancellationToken);

        commits.Select(commit => commit.Subject).ShouldBe(["Retry the refund twice", "Add the refund rule"]);
        commits.ShouldAllBe(commit => commit.Hash.Length >= 7);
        commits[0].At.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task ListTheChangedAndUntrackedFilesUpToTheLimit()
    {
        await CreateRepositoryWithOneCommitAsync();
        await File.AppendAllTextAsync(Path.Combine(Repository, "README.md"), "changed", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(Repository, "Refunds.cs"), "new", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(Repository, "Retry.cs"), "new", TestContext.Current.CancellationToken);

        IReadOnlyList<string> files = await git.ChangedFilesAsync(Repository, 2, TestContext.Current.CancellationToken);

        files.ShouldBe(["README.md", "Refunds.cs"]);
    }

    [Fact]
    public async Task PruneARegistrationWhoseFolderWasDeletedByHand()
    {
        await CreateRepositoryWithOneCommitAsync();
        await git.AddWorktreeAsync(Repository, Worktree, "fix-123", "main", TestContext.Current.CancellationToken);
        Directory.Delete(Worktree, recursive: true);

        int pruned = await git.PruneWorktreesAsync(Repository, TestContext.Current.CancellationToken);

        pruned.ShouldBe(1);
        (await git.ListWorktreesAsync(Repository, TestContext.Current.CancellationToken)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task AnswerTheOriginUrlOnlyOnceARemoteIsAdded()
    {
        await CreateRepositoryWithOneCommitAsync();

        (await git.OriginUrlAsync(Repository, TestContext.Current.CancellationToken)).ShouldBeNull();
        await RunGitAsync("remote", "add", "origin", "https://github.com/northwind/Northwind.Orders.git");
        (await git.OriginUrlAsync(Repository, TestContext.Current.CancellationToken)).ShouldBe("https://github.com/northwind/Northwind.Orders.git");
    }

    [Fact]
    public async Task AnswerRepositoryQuestionsOutsideAndInsideARepository()
    {
        await CreateRepositoryWithOneCommitAsync();
        string nested = Path.Combine(Repository, "src");
        Directory.CreateDirectory(nested);

        (await git.RepositoryRootAsync(nested, TestContext.Current.CancellationToken)).ShouldBe(Repository, StringCompareShould.IgnoreCase);
        (await git.RepositoryRootAsync(root, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await git.DefaultRemoteBranchAsync(Repository, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task AddAWorktreeUnderAFolderThatDoesNotExistYet()
    {
        await CreateRepositoryWithOneCommitAsync();
        string worktree = Path.Combine(root, "worktrees", "Northwind.Orders-fix-123");

        await git.AddWorktreeAsync(Repository, worktree, "fix-123", "main", TestContext.Current.CancellationToken);

        File.Exists(Path.Combine(worktree, "README.md")).ShouldBeTrue("git creates the missing worktree root");
    }

    [Fact]
    public async Task AnswerNothingForAFolderThatWasDeleted()
    {
        await CreateRepositoryWithOneCommitAsync();
        await git.AddWorktreeAsync(Repository, Worktree, "fix-123", "main", TestContext.Current.CancellationToken);
        Directory.Delete(Worktree, recursive: true);

        (await git.RepositoryRootAsync(Worktree, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await git.MainCheckoutOfAsync(Worktree, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await git.CurrentBranchAsync(Worktree, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await git.OriginUrlAsync(Worktree, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await git.DefaultRemoteBranchAsync(Worktree, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await git.LastCommitAtAsync(Worktree, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await git.IsMergedAsync(Worktree, "fix-123", "main", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task ThrowAGitExceptionForACommandInAFolderThatWasDeleted()
    {
        await CreateRepositoryWithOneCommitAsync();
        await git.AddWorktreeAsync(Repository, Worktree, "fix-123", "main", TestContext.Current.CancellationToken);
        Directory.Delete(Worktree, recursive: true);

        await Should.ThrowAsync<GitException>(() => git.CountChangedFilesAsync(Worktree, TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(root, recursive: true);
    }

    private async Task CreateRepositoryWithOneCommitAsync()
    {
        Directory.CreateDirectory(Repository);
        await RunGitAsync("init", "-q", "-b", "main");
        await RunGitAsync("config", "user.email", "vantage@example.test");
        await RunGitAsync("config", "user.name", "Vantage Tests");
        await File.WriteAllTextAsync(Path.Combine(Repository, "README.md"), "# test");
        await RunGitAsync("add", "-A");
        await RunGitAsync("commit", "-q", "-m", "init");
    }

    private async Task RunGitAsync(params string[] arguments)
    {
        ProcessResult result = await new ProcessRunner().RunAsync(new ProcessRequest("git", arguments, new WorkingFolder.At(Repository)), TestContext.Current.CancellationToken);
        result.ExitCode.ShouldBe(0, result.StandardError);
    }
}