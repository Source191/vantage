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
    private readonly GitCli git = new(new ProcessRunner());

    private string Repository => Path.Combine(root, "Northwind.Orders");

    private string Worktree => Path.Combine(root, "Northwind.Orders-fix-123");

    [Fact]
    public async Task ListOnlyTheMainCheckoutOfAFreshRepository()
    {
        await CreateRepositoryWithOneCommitAsync();

        IReadOnlyList<GitWorktreeEntry> entries = await git.ListWorktreesAsync(Repository, TestContext.Current.CancellationToken);

        entries.Single().ShouldSatisfyAllConditions(
            entry => entry.IsMain.ShouldBeTrue(),
            entry => entry.Branch.ShouldBe(BranchName.From("main")),
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
        entries[1].Branch.ShouldBe(BranchName.From("fix-123"));
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
        ProcessResult result = await new ProcessRunner().RunAsync(new ProcessRequest("git", arguments, Repository), TestContext.Current.CancellationToken);
        result.ExitCode.ShouldBe(0, result.StandardError);
    }
}