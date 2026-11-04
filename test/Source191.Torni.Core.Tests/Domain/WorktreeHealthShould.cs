using Source191.Torni.Domain;

namespace Source191.Torni.Core.Tests.Domain;

public sealed class WorktreeHealthShould
{
    private static readonly WorktreeObservation.Registered ListedByGit = new(
        FolderExists: true,
        Lock: new WorktreeLock.Unlocked(),
        Prunable: false);

    private static readonly WorktreeObservation.Unregistered NotListed = new(
        GitFile: new GitPointer.NoGitFile(),
        UnderClaudeWorktrees: false,
        EmptyLeftover: false);

    [Fact]
    public void BeOkForARegisteredWorktreeWhoseFolderExistsWithNothingToExplain()
    {
        WorktreeHealth health = WorktreeHealth.From(ListedByGit);

        health.Kind.ShouldBe(WorktreeHealthKind.Ok);
        health.Reason.ShouldBe(new HealthReason.None());
    }

    [Fact]
    public void BeLockedWithTheReasonGitGives()
    {
        WorktreeHealth health = WorktreeHealth.From(ListedByGit with { Lock = new WorktreeLock.Locked("maintenance") });

        health.Kind.ShouldBe(WorktreeHealthKind.Locked);
        health.Reason.ShouldBe(new HealthReason.Locked("maintenance"));
    }

    [Fact]
    public void BeLockedEvenWhenGitGivesNoReason()
    {
        WorktreeHealth health = WorktreeHealth.From(ListedByGit with { Lock = new WorktreeLock.Locked(string.Empty) });

        health.Kind.ShouldBe(WorktreeHealthKind.Locked);
    }

    [Fact]
    public void BeBrokenWhenARegisteredWorktreeFolderIsGone()
    {
        WorktreeHealth health = WorktreeHealth.From(ListedByGit with { FolderExists = false });

        health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        health.Reason.ShouldBe(new HealthReason.FolderGone());
    }

    [Fact]
    public void BeBrokenWithItsGitFileGoneWhenAPrunableFolderExists()
    {
        WorktreeHealth health = WorktreeHealth.From(ListedByGit with { Prunable = true });

        health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        health.Reason.ShouldBe(new HealthReason.GitFileGone());
    }

    [Fact]
    public void BeBrokenWithTheFolderGoneWhenAPrunableFolderIsGone()
    {
        WorktreeHealth health = WorktreeHealth.From(ListedByGit with { Prunable = true, FolderExists = false });

        health.Reason.ShouldBe(new HealthReason.FolderGone());
    }

    [Fact]
    public void BeBrokenWhenAFolderPointsAtARegistrationThatNoLongerExists()
    {
        WorktreeHealth health = WorktreeHealth.From(NotListed with { GitFile = new GitPointer.TargetGone() });

        health.Kind.ShouldBe(WorktreeHealthKind.Broken);
        health.Reason.ShouldBe(new HealthReason.RegistrationGone());
    }

    [Fact]
    public void BeOrphanForAFolderUnderClaudeWorktreesThatGitNoLongerLists()
    {
        WorktreeHealth health = WorktreeHealth.From(NotListed with { UnderClaudeWorktrees = true });

        health.Kind.ShouldBe(WorktreeHealthKind.Orphan);
        health.Reason.ShouldBe(new HealthReason.UnderClaudeWorktrees());
    }

    [Fact]
    public void BeEmptyForALeftoverFolderWithNothingInIt()
    {
        WorktreeHealth health = WorktreeHealth.From(NotListed with { EmptyLeftover = true });

        health.Kind.ShouldBe(WorktreeHealthKind.Empty);
        health.Reason.ShouldBe(new HealthReason.EmptyLeftover());
    }

    [Fact]
    public void BeElsewhereForAWorktreeOfARepositoryOutsideTheRoots()
    {
        FolderPath ledger = FolderPath.From(@"D:\other\Northwind.Ledger");

        WorktreeHealth health = WorktreeHealth.From(NotListed with { GitFile = new GitPointer.RegisteredElsewhere(ledger) });

        health.Kind.ShouldBe(WorktreeHealthKind.Elsewhere);
        health.Reason.ShouldBe(new HealthReason.RepositoryOutsideRoots(ledger));
    }

    [Fact]
    public void KeepAnOrphanWhosePointerTargetExistsAnOrphan()
    {
        WorktreeHealth health = WorktreeHealth.From(NotListed with { GitFile = new GitPointer.TargetExists(), UnderClaudeWorktrees = true });

        health.Kind.ShouldBe(WorktreeHealthKind.Orphan);
        health.Reason.ShouldBe(new HealthReason.UnderClaudeWorktrees());
    }

    [Fact]
    public void BeOrphanForAnyOtherFolderGitDoesNotRegister()
    {
        WorktreeHealth health = WorktreeHealth.From(NotListed);

        health.Kind.ShouldBe(WorktreeHealthKind.Orphan);
        health.Reason.ShouldBe(new HealthReason.NotRegistered());
    }
}