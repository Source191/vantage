using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class RemovalDecisionShould
{
    private static readonly WorktreeRemovalFacts Clean = new(
        Health: WorktreeHealthKind.Ok,
        Branch: "fix-123",
        DirtyFileCount: 0,
        UnpushedCommitCount: 0,
        Session: new WorktreeSession.None(),
        BranchCreatedByVantage: true,
        BranchMerged: true,
        LeftoverFileCount: 0);

    [Fact]
    public void RemoveACleanWorktree()
    {
        RemovalDecision decision = RemovalDecision.For(Clean, forced: false);

        decision.Outcome.ShouldBe(RemovalOutcome.RemoveWorktree);
        decision.Forced.ShouldBeFalse();
        decision.PruneRegistration.ShouldBeFalse();
        decision.Reason.ShouldBe(new RemovalReason.None());
    }

    [Fact]
    public void RefuseWhenThereAreUnpushedCommits()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { UnpushedCommitCount = 2 }, forced: false);

        decision.Outcome.ShouldBe(RemovalOutcome.Refused);
        decision.Reason.ShouldBe(new RemovalReason.UnpushedCommits("fix-123", 2));
        decision.Forced.ShouldBeFalse();
        decision.PruneRegistration.ShouldBeFalse();
        decision.Branch.ShouldBe(BranchAction.None);
    }

    [Fact]
    public void RefuseWhenThereAreUncommittedChanges()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { DirtyFileCount = 5 }, forced: false);

        decision.Outcome.ShouldBe(RemovalOutcome.Refused);
        decision.Reason.ShouldBe(new RemovalReason.ChangesWouldBeLost(5));
    }

    [Fact]
    public void RemoveAWorktreeWithWorkInItWhenForced()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { DirtyFileCount = 5, UnpushedCommitCount = 1 }, forced: true);

        decision.Outcome.ShouldBe(RemovalOutcome.RemoveWorktree);
        decision.Forced.ShouldBeTrue();
    }

    [Fact]
    public void RefuseALockedWorktreeEvenWhenForced()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Locked }, forced: true);

        decision.Outcome.ShouldBe(RemovalOutcome.Refused);
        decision.Reason.ShouldBe(new RemovalReason.Locked());
        decision.Forced.ShouldBeFalse();
        decision.PruneRegistration.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefuseAnElsewhereWorktreeEvenWhenForced(bool forced)
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Elsewhere, DirtyFileCount = 3 }, forced);

        decision.Outcome.ShouldBe(RemovalOutcome.Refused);
        decision.Reason.ShouldBe(new RemovalReason.RepositoryOutsideRoots());
        decision.CanBeForced.ShouldBeFalse();
        decision.Forced.ShouldBeFalse();
        decision.Branch.ShouldBe(BranchAction.None);
    }

    [Fact]
    public void RefuseAWorktreeWithALiveSessionEvenWhenForced()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Session = new WorktreeSession.Running(SessionId.From("sess-1"), "remove-sonar") }, forced: true);

        decision.Outcome.ShouldBe(RemovalOutcome.Refused);
        decision.Reason.ShouldBe(new RemovalReason.LiveSession("remove-sonar"));
    }

    [Fact]
    public void DeleteTheBranchWhenVantageCreatedItAndItIsMerged()
    {
        RemovalDecision decision = RemovalDecision.For(Clean, forced: false);

        decision.Branch.ShouldBe(BranchAction.Delete);
    }

    [Fact]
    public void AskAboutTheBranchWhenVantageDidNotCreateIt()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { BranchCreatedByVantage = false }, forced: false);

        decision.Branch.ShouldBe(BranchAction.Ask);
    }

    [Fact]
    public void AskAboutTheBranchWhenItIsNotMerged()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { BranchMerged = false }, forced: false);

        decision.Branch.ShouldBe(BranchAction.Ask);
    }

    [Theory]
    [InlineData(WorktreeHealthKind.Empty)]
    [InlineData(WorktreeHealthKind.Orphan)]
    public void DeleteALeftoverFolderWithoutGit(WorktreeHealthKind health)
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = health }, forced: false);

        decision.Outcome.ShouldBe(RemovalOutcome.DeleteFolder);
        decision.Branch.ShouldBe(BranchAction.None);
        decision.PruneRegistration.ShouldBeFalse();
        decision.Forced.ShouldBeFalse();
        decision.Reason.ShouldBe(new RemovalReason.LeftoverFolder());
    }

    [Theory]
    [InlineData(WorktreeHealthKind.Orphan)]
    [InlineData(WorktreeHealthKind.Broken)]
    [InlineData(WorktreeHealthKind.Empty)]
    public void RefuseALeftoverFolderThatStillHasFilesInIt(WorktreeHealthKind health)
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = health, LeftoverFileCount = 12 }, forced: false);

        decision.Outcome.ShouldBe(RemovalOutcome.Refused);
        decision.Reason.ShouldBe(new RemovalReason.LeftoverFilesWouldBeLost(12, AtLeast: false));
    }

    [Fact]
    public void SayAtLeastTheLimitWhenTheCountWasCapped()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Orphan, LeftoverFileCount = WorktreeRemovalFacts.LeftoverFileLimit }, forced: false);

        decision.Outcome.ShouldBe(RemovalOutcome.Refused);
        decision.Reason.ShouldBe(new RemovalReason.LeftoverFilesWouldBeLost(WorktreeRemovalFacts.LeftoverFileLimit, AtLeast: true));
    }

    [Fact]
    public void DeleteALeftoverFolderWithFilesWhenForced()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Orphan, LeftoverFileCount = 12 }, forced: true);

        decision.Outcome.ShouldBe(RemovalOutcome.DeleteFolder);
        decision.Forced.ShouldBeTrue();
    }

    [Fact]
    public void RefuseALeftoverFolderWithALiveSessionEvenWhenForced()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Orphan, Session = new WorktreeSession.Running(SessionId.From("sess-2"), "fix-service-tests") }, forced: true);

        decision.Outcome.ShouldBe(RemovalOutcome.Refused);
        decision.Reason.ShouldBe(new RemovalReason.LiveSession("fix-service-tests"));
    }

    [Fact]
    public void SayWhetherARefusalCanBeForced()
    {
        RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Locked }, forced: false).CanBeForced.ShouldBeFalse();
        RemovalDecision.For(Clean with { Session = new WorktreeSession.Running(SessionId.From("sess-1"), "remove-sonar") }, forced: false).CanBeForced.ShouldBeFalse();
        RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Orphan, Session = new WorktreeSession.Running(SessionId.From("sess-2"), "fix-service-tests") }, forced: false).CanBeForced.ShouldBeFalse();
        RemovalDecision.For(Clean with { DirtyFileCount = 5 }, forced: false).CanBeForced.ShouldBeTrue();
        RemovalDecision.For(Clean with { UnpushedCommitCount = 2 }, forced: false).CanBeForced.ShouldBeTrue();
        RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Orphan, LeftoverFileCount = 12 }, forced: false).CanBeForced.ShouldBeTrue();
        RemovalDecision.For(Clean, forced: false).CanBeForced.ShouldBeFalse();
    }

    [Fact]
    public void DeleteABrokenFolderAndPruneItsRegistration()
    {
        RemovalDecision decision = RemovalDecision.For(Clean with { Health = WorktreeHealthKind.Broken }, forced: false);

        decision.Outcome.ShouldBe(RemovalOutcome.DeleteFolder);
        decision.PruneRegistration.ShouldBeTrue();
    }

    /// <summary>A worktree whose .git file is gone is broken with files in it: refused, but force-remove deletes and prunes.</summary>
    [Fact]
    public void ForceRemoveABrokenFolderThatStillHasFilesAndPruneItsRegistration()
    {
        WorktreeRemovalFacts gitFileGone = Clean with { Health = WorktreeHealthKind.Broken, LeftoverFileCount = 12 };

        RemovalDecision.For(gitFileGone, forced: false).CanBeForced.ShouldBeTrue();
        RemovalDecision forced = RemovalDecision.For(gitFileGone, forced: true);
        forced.Outcome.ShouldBe(RemovalOutcome.DeleteFolder);
        forced.Forced.ShouldBeTrue();
        forced.PruneRegistration.ShouldBeTrue();
    }
}