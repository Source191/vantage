using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Sessions = Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;
using Worktrees = Source191.Vantage.Core.Tests.Application.Actions.WorktreeTestData;

namespace Source191.Vantage.Core.Tests.Application.Effects;

public sealed class WorkingTextShould
{
    private static readonly SessionRow Session = Sessions.Background(Sessions.Repository, isWorktree: false);
    private static readonly WorktreeRow Worktree = Worktrees.Row(WorktreeHealthKind.Ok);

    [Fact]
    public void SayWhatEachSessionEffectIsDoingWhileItRuns()
    {
        WorkingText.For(new OpenTabForSession(Session)).ShouldBe("Opening a tab for fix-123...");
        WorkingText.For(new StopSession(Session)).ShouldBe("Stopping...");
        WorkingText.For(new AdoptSession(Session)).ShouldBe("Moving fix-123 under Vantage...");
        WorkingText.For(new RenameSession(Session, SessionName.Parse("fix-124"))).ShouldBe("Renaming fix-123 to fix-124...");
        WorkingText.For(new CloseEmptyTerminals([Session])).ShouldBe("Closing...");
        WorkingText.For(new ResumeSessions([Session, Session])).ShouldBe("Resuming 2...");
        WorkingText.For(new DeleteSession(Session)).ShouldBe("Deleting fix-123...");
        WorkingText.For(new DeleteBranch(new BranchToConfirm(Sessions.Repository, "fix-123"))).ShouldBe("Deleting branch fix-123...");
    }

    [Fact]
    public void SayWhatEachWorktreeEffectIsDoingWhileItRuns()
    {
        WorkingText.For(new RemoveWorktree(Worktree, Forced: false)).ShouldBe("Removing Northwind.Orders-fix-123...");
        WorkingText.For(new PruneRepository(Worktrees.Repository)).ShouldBe("Pruning...");
        WorkingText.For(new OpenShellTab(Worktree)).ShouldBe("Opening a shell in Northwind.Orders-fix-123...");
    }

    [Fact]
    public void NameTheSessionBeingStarted()
    {
        LaunchPlan plan = new(
            Sessions.Repository, SessionName.Parse("Fix flaky tests"), Description: null, Worktree: null,
            ReuseExistingWorktree: false, WorktreeBase.RemoteDefault, OpeningPrompt: null, new RepositoryChoice("Northwind.Orders", Sessions.Repository, IsRepository: true));

        WorkingText.For(new StartSession(plan, OpenInTab: true)).ShouldBe("Starting Fix flaky tests...");
    }

    [Fact]
    public void FallBackToAPlainWordForAnyOtherEffect()
    {
        WorkingText.For(new Quit()).ShouldBe("Working...");
    }
}