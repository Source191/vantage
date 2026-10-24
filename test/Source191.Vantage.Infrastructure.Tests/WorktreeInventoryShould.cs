using Source191.Vantage.Adapters.Files;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>Scans real repositories under a throwaway root with the real git.</summary>
[Trait("Category", "Integration")]
public sealed class WorktreeInventoryShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly TemporaryGitRoot root = new();

    private WorktreeInventory Inventory => new(new GitCli(new ProcessRunner()), new Folders(), [root.Path]);

    private static Session SessionIn(string directory, string name, int? processId)
    {
        return new Session(new SessionId(Guid.NewGuid().ToString()), null, name, new FolderPath(directory), SessionKind.Interactive, Noon, DisplayState.Idle, processId);
    }

    public void Dispose()
    {
        root.Dispose();
    }

    [Fact]
    public async Task ListTheLinkedWorktreesOfEveryRepositoryUnderTheRoots()
    {
        string first = await root.CreateRepositoryAsync("Northwind.First");
        string second = await root.CreateRepositoryAsync("Northwind.Second");
        string firstWorktree = await root.AddSiblingWorktreeAsync(first, "fix-1");
        string secondWorktree = await root.AddSiblingWorktreeAsync(second, "fix-2");

        IReadOnlyList<WorktreeRow> rows = await Inventory.ScanAsync([], TestContext.Current.CancellationToken);

        rows.Select(row => (row.RepositoryName, row.Path, row.Branch, row.Health.Kind)).ShouldBe(
        [
            ("Northwind.First", new FolderPath(firstWorktree), new BranchName("fix-1"), WorktreeHealthKind.Ok),
            ("Northwind.Second", new FolderPath(secondWorktree), new BranchName("fix-2"), WorktreeHealthKind.Ok),
        ]);
    }

    [Fact]
    public async Task ReportAFolderUnderClaudeWorktreesThatGitNoLongerListsAsAnOrphan()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string orphan = Path.Combine(repository, ".claude", "worktrees", "fix-service-tests");
        Directory.CreateDirectory(orphan);
        await File.WriteAllTextAsync(Path.Combine(orphan, "Program.cs"), "// left behind", TestContext.Current.CancellationToken);

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.Path.Value.ShouldBe(orphan);
        row.RepositoryName.ShouldBe("Northwind.Orders");
        row.Health.Kind.ShouldBe(WorktreeHealthKind.Orphan);
        row.LeftoverFileCount.ShouldBe(1);
    }

    [Fact]
    public async Task StopCountingLeftoverFilesAtTheLimit()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string orphan = Path.Combine(repository, ".claude", "worktrees", "fix-service-tests", "node_modules");
        Directory.CreateDirectory(orphan);
        foreach (int number in Enumerable.Range(1, WorktreeRemovalFacts.LeftoverFileLimit + 5))
        {
            await File.WriteAllTextAsync(Path.Combine(orphan, $"{number}.js"), string.Empty, TestContext.Current.CancellationToken);
        }

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.LeftoverFileCount.ShouldBe(WorktreeRemovalFacts.LeftoverFileLimit);
    }

    [Fact]
    public async Task ReportASiblingWhoseRepositoryHasGoneAsBrokenWithNoOwnerToPrune()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Retired");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "republish");
        TemporaryGitRoot.DeleteTree(repository);

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.Path.Value.ShouldBe(worktree);
        row.RepositoryName.ShouldBe("Northwind.Retired");
        row.Health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        row.OwnerExists.ShouldBeFalse();
        row.Branch.ShouldBeNull();
    }

    [Fact]
    public async Task ReportARegistrationWhoseFolderWasDeletedAsBrokenWithAnOwnerToPrune()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");
        TemporaryGitRoot.DeleteTree(worktree);

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.Path.Value.ShouldBe(worktree);
        row.Health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        row.OwnerExists.ShouldBeTrue();
        row.DirtyFiles.ShouldBe(0);
    }

    [Fact]
    public async Task ReportEmptyLeftoverFoldersDirectlyUnderARoot()
    {
        Directory.CreateDirectory(Path.Combine(root.Path, "Northwind.Orders.worktrees"));
        Directory.CreateDirectory(Path.Combine(root.Path, "Archive.worktrees"));
        Directory.CreateDirectory(Path.Combine(root.Path, "just-a-folder"));

        IReadOnlyList<WorktreeRow> rows = await Inventory.ScanAsync([], TestContext.Current.CancellationToken);

        rows.Select(row => (row.Path.Name, row.Health.Kind)).ShouldBe(
        [
            ("Archive.worktrees", WorktreeHealthKind.Empty),
            ("Northwind.Orders.worktrees", WorktreeHealthKind.Empty),
        ]);
    }

    [Fact]
    public async Task MarkALockedWorktreeWithItsReason()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");
        await root.GitAsync(repository, "worktree", "lock", "--reason", "another tool is using it", worktree);

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.Health.ShouldBe(new WorktreeHealth(WorktreeHealthKind.Locked, "another tool is using it"));
    }

    [Fact]
    public async Task CountTheWorkAtRiskInARegisteredWorktree()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");
        await root.CommitAFileAsync(worktree, "second.txt");
        foreach (int number in Enumerable.Range(1, 5))
        {
            await File.WriteAllTextAsync(Path.Combine(worktree, $"dirty-{number}.txt"), "x", TestContext.Current.CancellationToken);
        }

        WorktreeRow row = (await Inventory.ScanAsync([], TestContext.Current.CancellationToken)).Single();

        row.DirtyFiles.ShouldBe(5);
        row.UnpushedCommits.ShouldBe(2);
        row.LastCommitAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task NameTheLiveSessionWorkingInsideAWorktree()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");
        Session live = SessionIn(Path.Combine(worktree, "src"), "fix-the-tests", processId: 4242);
        Session dead = SessionIn(worktree, "finished-earlier", processId: null);
        Session elsewhere = SessionIn(repository, "main-work", processId: 4343);

        WorktreeRow row = (await Inventory.ScanAsync([elsewhere, dead, live], TestContext.Current.CancellationToken)).Single();

        row.SessionName.ShouldBe("fix-the-tests");
    }

    [Fact]
    public async Task ReportTheSessionIdOfTheLiveSession()
    {
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");
        Session live = SessionIn(worktree, "fix-tests", processId: 4242);
        Session sameName = SessionIn(repository, "fix-tests", processId: 4343);

        WorktreeRow row = (await Inventory.ScanAsync([sameName, live], TestContext.Current.CancellationToken)).Single();

        row.SessionId.ShouldBe(live.SessionId);
        row.SessionName.ShouldBe("fix-tests");
    }

    [Fact]
    public async Task KeepScanningWhenOneFolderIsNotAUsableRepository()
    {
        Directory.CreateDirectory(Path.Combine(root.Path, "Corrupt", ".git"));
        string repository = await root.CreateRepositoryAsync("Northwind.Orders");
        string worktree = await root.AddSiblingWorktreeAsync(repository, "fix-1");

        IReadOnlyList<WorktreeRow> rows = await Inventory.ScanAsync([], TestContext.Current.CancellationToken);

        rows.Single().Path.Value.ShouldBe(worktree);
    }
}