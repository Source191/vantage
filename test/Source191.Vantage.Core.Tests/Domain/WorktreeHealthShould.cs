using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class WorktreeHealthShould
{
    private static readonly WorktreeObservation Registered = new(
        RegisteredWithGit: true,
        FolderExists: true,
        Lock: new WorktreeLock.Unlocked(),
        UnderClaudeWorktrees: false,
        GitFile: new GitPointer.TargetExists(),
        EmptyLeftoverFolder: false);

    [Fact]
    public void BeOkForARegisteredWorktreeWhoseFolderExistsWithNothingToExplain()
    {
        WorktreeHealth health = WorktreeHealth.From(Registered);

        health.Kind.ShouldBe(WorktreeHealthKind.Ok);
        health.Reason.ShouldBe(new HealthReason.None());
    }

    [Fact]
    public void BeLockedWithTheReasonGitGives()
    {
        WorktreeHealth health = WorktreeHealth.From(Registered with { Lock = new WorktreeLock.Locked("maintenance") });

        health.Kind.ShouldBe(WorktreeHealthKind.Locked);
        health.Reason.ShouldBe(new HealthReason.Locked("maintenance"));
    }

    [Fact]
    public void BeLockedEvenWhenGitGivesNoReason()
    {
        WorktreeHealth health = WorktreeHealth.From(Registered with { Lock = new WorktreeLock.Locked(string.Empty) });

        health.Kind.ShouldBe(WorktreeHealthKind.Locked);
    }

    [Fact]
    public void BeBrokenWhenARegisteredWorktreeFolderIsGone()
    {
        WorktreeHealth health = WorktreeHealth.From(Registered with { FolderExists = false });

        health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        health.Reason.ShouldBe(new HealthReason.FolderGone());
    }

    [Fact]
    public void BeBrokenWhenAFolderPointsAtARegistrationThatNoLongerExists()
    {
        WorktreeHealth health = WorktreeHealth.From(Registered with { RegisteredWithGit = false, GitFile = new GitPointer.TargetGone() });

        health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        health.Reason.ShouldBe(new HealthReason.RegistrationGone());
    }

    [Fact]
    public void BeOrphanForAFolderUnderClaudeWorktreesThatGitNoLongerLists()
    {
        WorktreeHealth health = WorktreeHealth.From(Registered with { RegisteredWithGit = false, UnderClaudeWorktrees = true, GitFile = new GitPointer.NoGitFile() });

        health.Kind.ShouldBe(WorktreeHealthKind.Orphan);
        health.Reason.ShouldBe(new HealthReason.UnderClaudeWorktrees());
    }

    [Fact]
    public void BeEmptyForALeftoverFolderWithNothingInIt()
    {
        WorktreeHealth health = WorktreeHealth.From(Registered with { RegisteredWithGit = false, GitFile = new GitPointer.NoGitFile(), EmptyLeftoverFolder = true });

        health.Kind.ShouldBe(WorktreeHealthKind.Empty);
        health.Reason.ShouldBe(new HealthReason.EmptyLeftover());
    }

    [Fact]
    public void BeElsewhereForAWorktreeOfARepositoryOutsideTheRoots()
    {
        FolderPath ledger = FolderPath.From(@"D:\other\Northwind.Ledger");

        WorktreeHealth health = WorktreeHealth.From(Registered with { RegisteredWithGit = false, GitFile = new GitPointer.RegisteredElsewhere(ledger) });

        health.Kind.ShouldBe(WorktreeHealthKind.Elsewhere);
        health.Reason.ShouldBe(new HealthReason.RepositoryOutsideRoots(ledger));
    }

    [Fact]
    public void BeOrphanWhenTheRegistrationExistsButNoOwningRepositoryCanBeNamed()
    {
        WorktreeHealth health = WorktreeHealth.From(Registered with { RegisteredWithGit = false, GitFile = new GitPointer.TargetExists() });

        health.Kind.ShouldBe(WorktreeHealthKind.Orphan);
        health.Reason.ShouldBe(new HealthReason.NotRegistered());
    }

    [Fact]
    public void BeOrphanForAnyOtherFolderGitDoesNotRegister()
    {
        WorktreeHealth health = WorktreeHealth.From(Registered with { RegisteredWithGit = false, GitFile = new GitPointer.NoGitFile() });

        health.Kind.ShouldBe(WorktreeHealthKind.Orphan);
        health.Reason.ShouldBe(new HealthReason.NotRegistered());
    }
}