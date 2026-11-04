using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Domain;

public sealed class RemovalDecisionShould
{
    private static readonly WorktreeRemovalFacts Clean = new(
        Health: WorktreeHealthKind.Ok,
        Branch: "fix-123",
        DirtyFileCount: 0,
        UnpushedCommitCount: 0,
        Session: new WorktreeSession.None(),
        BranchCreatedByTorni: true,
        BranchMerged: true,
        LeftoverFileCount: 0);

    [Fact]
    public void RemoveACleanWorktree()
    {
        RemovalDecision decision = RemovalDecision.For(Clean, forced: false);

        decision.ShouldBe(new RemovalDecision.RemoveWorktree(Forced: false, BranchAction.Delete));
    }

    [Fact]
    public void RefuseWhenThereAreUnpushedCommits()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { UnpushedCommitCount = 2 }, forced: false);

        decision.ShouldBe(new RemovalDecision.Refused(new RemovalReason.UnpushedCommits("fix-123", 2), CanBeForced: true));
    }

    [Fact]
    public void RefuseWhenThereAreUncommittedChanges()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { DirtyFileCount = 5 }, forced: false);

        decision.ShouldBe(new RemovalDecision.Refused(new RemovalReason.ChangesWouldBeLost(5), CanBeForced: true));
    }

    [Fact]
    public void RemoveAWorktreeWithWorkInItWhenForced()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { DirtyFileCount = 5, UnpushedCommitCount = 1 }, forced: true);

        decision.ShouldBe(new RemovalDecision.RemoveWorktree(Forced: true, BranchAction.Delete));
    }

    [Fact]
    public void RefuseALockedWorktreeEvenWhenForced()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Locked }, forced: true);

        decision.ShouldBe(new RemovalDecision.Refused(new RemovalReason.Locked(), CanBeForced: false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefuseAnElsewhereWorktreeEvenWhenForced(bool forced)
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Elsewhere, DirtyFileCount = 3 }, forced);

        decision.ShouldBe(new RemovalDecision.Refused(new RemovalReason.RepositoryOutsideRoots(), CanBeForced: false));
    }

    [Fact]
    public void RefuseAWorktreeWithALiveSessionEvenWhenForced()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Session = new WorktreeSession.Running(SessionId.From("sess-1"), Listed.As("remove-sonar")) }, forced: true);

        decision.ShouldBe(new RemovalDecision.Refused(new RemovalReason.LiveSession(Listed.As("remove-sonar")), CanBeForced: false));
    }

    [Fact]
    public void AskAboutTheBranchWhenTorniDidNotCreateIt()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { BranchCreatedByTorni = false }, forced: false);

        decision.ShouldBe(new RemovalDecision.RemoveWorktree(Forced: false, BranchAction.Ask));
    }

    [Fact]
    public void AskAboutTheBranchWhenItIsNotMerged()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { BranchMerged = false }, forced: false);

        decision.ShouldBe(new RemovalDecision.RemoveWorktree(Forced: false, BranchAction.Ask));
    }

    [Theory]
    [InlineData(WorktreeHealthKind.Empty)]
    [InlineData(WorktreeHealthKind.Orphan)]
    public void DeleteALeftoverFolderWithoutGit(WorktreeHealthKind health)
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = health }, forced: false);

        decision.ShouldBe(new RemovalDecision.DeleteFolder(Forced: false, Prune: false));
    }

    [Theory]
    [InlineData(WorktreeHealthKind.Orphan)]
    [InlineData(WorktreeHealthKind.Broken)]
    [InlineData(WorktreeHealthKind.Empty)]
    public void RefuseALeftoverFolderThatStillHasFilesInIt(WorktreeHealthKind health)
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = health, LeftoverFileCount = 12 }, forced: false);

        decision.ShouldBe(new RemovalDecision.Refused(new RemovalReason.LeftoverFilesWouldBeLost(12, AtLeast: false), CanBeForced: true));
    }

    [Fact]
    public void SayAtLeastTheLimitWhenTheCountWasCapped()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Orphan, LeftoverFileCount = WorktreeRemovalFacts.LeftoverFileLimit }, forced: false);

        decision.ShouldBe(new RemovalDecision.Refused(new RemovalReason.LeftoverFilesWouldBeLost(WorktreeRemovalFacts.LeftoverFileLimit, AtLeast: true), CanBeForced: true));
    }

    [Fact]
    public void DeleteALeftoverFolderWithFilesWhenForced()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Orphan, LeftoverFileCount = 12 }, forced: true);

        decision.ShouldBe(new RemovalDecision.DeleteFolder(Forced: true, Prune: false));
    }

    [Fact]
    public void RefuseALeftoverFolderWithALiveSessionEvenWhenForced()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Orphan, Session = new WorktreeSession.Running(SessionId.From("sess-2"), Listed.As("fix-service-tests")) }, forced: true);

        decision.ShouldBe(new RemovalDecision.Refused(new RemovalReason.LiveSession(Listed.As("fix-service-tests")), CanBeForced: false));
    }

    [Fact]
    public void SayWhetherARefusalCanBeForced()
    {
        CanBeForced(Clean with { Health = WorktreeHealthKind.Locked }).ShouldBeFalse();
        CanBeForced(Clean with { Session = new WorktreeSession.Running(SessionId.From("sess-1"), Listed.As("remove-sonar")) }).ShouldBeFalse();
        CanBeForced(Clean with { Health = WorktreeHealthKind.Orphan, Session = new WorktreeSession.Running(SessionId.From("sess-2"), Listed.As("fix-service-tests")) }).ShouldBeFalse();
        CanBeForced(Clean with { DirtyFileCount = 5 }).ShouldBeTrue();
        CanBeForced(Clean with { UnpushedCommitCount = 2 }).ShouldBeTrue();
        CanBeForced(Clean with { Health = WorktreeHealthKind.Orphan, LeftoverFileCount = 12 }).ShouldBeTrue();
        CanBeForced(Clean).ShouldBeFalse();
    }

    [Fact]
    public void DeleteABrokenFolderAndPruneItsRegistration()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Broken }, forced: false);

        decision.ShouldBe(new RemovalDecision.DeleteFolder(Forced: false, Prune: true));
    }

    /// <summary>A worktree whose .git file is gone is broken with files in it: refused, but force-remove deletes and prunes.</summary>
    [Fact]
    public void ForceRemoveABrokenFolderThatStillHasFilesAndPruneItsRegistration()
    {
        WorktreeRemovalFacts gitFileGone = Clean with { Health = WorktreeHealthKind.Broken, LeftoverFileCount = 12 };

        CanBeForced(gitFileGone).ShouldBeTrue();
        RemovalDecision.For(gitFileGone, forced: true).ShouldBe(new RemovalDecision.DeleteFolder(Forced: true, Prune: true));
    }

    private static bool CanBeForced(WorktreeRemovalFacts facts) => RemovalDecision.For(facts, forced: false) is RemovalDecision.Refused { CanBeForced: true };
}