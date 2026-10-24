using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class WorktreesViewShould
{
    private const string Repository = @"C:\repos\Northwind.Orders";

    private static readonly WorktreeRow Clean = Row(@"C:\repos\Northwind.Orders-fix-1", "fix-1", WorktreeHealthKind.Ok);
    private static readonly WorktreeRow Dirty = Row(@"C:\repos\Northwind.Orders-fix-2", "fix-2", WorktreeHealthKind.Ok) with { DirtyFiles = 5, UnpushedCommits = 2 };
    private static readonly WorktreeRow Locked = Row(@"C:\repos\Northwind.Orders-locked", "maintenance", WorktreeHealthKind.Locked, "another tool is using it");
    private static readonly WorktreeRow Orphan = Row(@"C:\repos\Northwind.Orders\.claude\worktrees\fix-service-tests", null, WorktreeHealthKind.Orphan) with { LeftoverFileCount = 12 };
    private static readonly WorktreeRow Empty = Row(@"C:\repos\Northwind.Orders.worktrees", null, WorktreeHealthKind.Empty) with { Repository = @"C:\repos" };
    private static readonly WorktreeRow Gone = Row(@"C:\repos\Northwind.Retired-republish", null, WorktreeHealthKind.Broken) with { Repository = @"C:\repos\Northwind.Retired", OwnerExists = false };
    private static readonly WorktreeRow Busy = Row(@"C:\repos\Northwind.Orders-fix-3", "fix-3", WorktreeHealthKind.Ok) with { SessionId = "sess-fix-3", SessionName = "fix-the-tests" };

    private static readonly WorktreesViewState Loaded = Reduce(WorktreesViewState.Empty, new WorktreeRowsLoaded([Gone, Busy, Empty, Orphan, Locked, Dirty, Clean]));

    [Fact]
    public void OrderRowsByRepositoryThenPathAndSelectTheFirst()
    {
        Loaded.VisibleRows.Select(row => row.FolderName).ShouldBe(["Northwind.Orders.worktrees", "fix-service-tests", "Northwind.Orders-fix-1", "Northwind.Orders-fix-2", "Northwind.Orders-fix-3", "Northwind.Orders-locked", "Northwind.Retired-republish"]);
        Loaded.Selected.ShouldBe(Empty);
        Loaded.Title.ShouldBe("worktrees(all)[7]");
    }

    [Fact]
    public void MoveTheSelectionWithTheArrowKeysAndStopAtTheEnds()
    {
        Press(Loaded, Key.Down, Key.Down).Selected.ShouldBe(Clean);
        Press(Loaded, Key.Up).Selected.ShouldBe(Empty);
        Press(Loaded, Enumerable.Repeat(Key.Down, 10).ToArray()).Selected.ShouldBe(Gone);
    }

    [Fact]
    public void KeepTheSelectionAcrossARefreshAndFallBackToTheNearestRow()
    {
        WorktreesViewState onDirty = Press(Loaded, Key.Down, Key.Down, Key.Down);

        Reduce(onDirty, new WorktreeRowsLoaded([Clean, Dirty, Locked])).Selected.ShouldBe(Dirty);
        Reduce(onDirty, new WorktreeRowsLoaded([Clean, Locked])).Selected.ShouldBe(Locked);
        Reduce(onDirty, new WorktreeRowsLoaded([])).Selected.ShouldBeNull();
    }

    [Fact]
    public void FilterByRepositoryFolderBranchOrHealth()
    {
        WorktreesViewState filtered = Press(Loaded, Key.Slash, Key.Of('o'), Key.Of('r'), Key.Of('p'), Key.Enter);

        filtered.VisibleRows.ShouldBe([Orphan]);
        filtered.Title.ShouldBe("worktrees(orp)[1]");
        Press(filtered, Key.Escape).VisibleRows.Count.ShouldBe(7);
    }

    [Fact]
    public void RemoveACleanWorktreeOnD()
    {
        WorktreesViewState onClean = Press(Loaded, Key.Down, Key.Down);

        WorktreesViewUpdate update = WorktreesView.Reduce(onClean, new KeyPressed(Key.Of('d')));

        update.Effects.ShouldBe([new RemoveWorktree(Clean, Forced: false)]);
    }

    [Fact]
    public void RefuseToRemoveAWorktreeWithUnpushedWorkAndNameTheReason()
    {
        WorktreesViewState onDirty = Press(Loaded, Key.Down, Key.Down, Key.Down);

        WorktreesViewUpdate update = WorktreesView.Reduce(onDirty, new KeyPressed(Key.Of('d')));

        update.Effects.ShouldBeEmpty();
        update.State.Footer.ShouldBe(new Footer.NotRemoved("5 changed or untracked files would be lost", OfferForce: true));
    }

    [Fact]
    public void AskForATypedConfirmationBeforeForcing()
    {
        WorktreesViewState onDirty = Press(Loaded, Key.Down, Key.Down, Key.Down);

        WorktreesViewUpdate update = WorktreesView.Reduce(onDirty, new KeyPressed(Key.Of('D')));

        update.Effects.ShouldBe([new ConfirmForceRemove(Dirty, ConfirmationText: "Northwind.Orders-fix-2")]);
    }

    [Fact]
    public void NeverForceALockedWorktreeOrOneWithALiveSession()
    {
        WorktreesViewState onLocked = Press(Loaded, Key.Down, Key.Down, Key.Down, Key.Down, Key.Down);
        WorktreesViewState onBusy = Press(Loaded, Key.Down, Key.Down, Key.Down, Key.Down);

        WorktreesView.Reduce(onLocked, new KeyPressed(Key.Of('D'))).State.Footer.ShouldBe(new Footer.NotRemoved("the worktree is locked", OfferForce: false));
        WorktreesView.Reduce(onBusy, new KeyPressed(Key.Of('D'))).State.Footer.ShouldBe(new Footer.NotRemoved("session fix-the-tests is using it", OfferForce: false));
        WorktreesView.Reduce(onBusy, new KeyPressed(Key.Of('D'))).Effects.ShouldBeEmpty();
    }

    [Fact]
    public void NotSuggestForceWhenTheRefusalCannotBeForced()
    {
        WorktreesViewState onLocked = Loaded with { SelectedPath = Locked.Path };
        WorktreesViewState onBusy = Loaded with { SelectedPath = Busy.Path };

        WorktreesView.Reduce(onLocked, new KeyPressed(Key.Of('d'))).State.Footer.ShouldBe(new Footer.NotRemoved("the worktree is locked", OfferForce: false));
        WorktreesView.Reduce(onBusy, new KeyPressed(Key.Of('d'))).State.Footer.ShouldBe(new Footer.NotRemoved("session fix-the-tests is using it", OfferForce: false));
        WorktreesView.Reduce(Loaded with { SelectedPath = Dirty.Path }, new KeyPressed(Key.Of('d'))).State.Footer.ShouldBeOfType<Footer.NotRemoved>().OfferForce.ShouldBeTrue();
    }

    [Fact]
    public void DeleteAnEmptyLeftoverWithoutConfirmationAndConfirmAnOrphanWithFilesByFolderName()
    {
        WorktreesViewState onOrphan = Press(Loaded, Key.Down);

        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('d'))).Effects.ShouldBe([new RemoveWorktree(Empty, Forced: false)]);
        WorktreesView.Reduce(onOrphan, new KeyPressed(Key.Of('d'))).State.Footer.ShouldBe(new Footer.NotRemoved("12 files that git no longer tracks would be lost", OfferForce: true));
        WorktreesView.Reduce(onOrphan, new KeyPressed(Key.Of('D'))).Effects.ShouldBe([new ConfirmForceRemove(Orphan, ConfirmationText: "fix-service-tests")]);
    }

    [Fact]
    public void PruneTheSelectedRowsRepositoryUnlessItHasGone()
    {
        WorktreesViewState onGone = Press(Loaded, Enumerable.Repeat(Key.Down, 6).ToArray());

        WorktreesView.Reduce(Press(Loaded, Key.Down, Key.Down), new KeyPressed(Key.Of('p'))).Effects.ShouldBe([new PruneRepository(Repository)]);
        WorktreesViewUpdate refused = WorktreesView.Reduce(onGone, new KeyPressed(Key.Of('p')));
        refused.Effects.ShouldBeEmpty();
        refused.State.Footer.ShouldBe(new Footer.NothingToPrune("Northwind.Retired"));
    }

    [Fact]
    public void StartASessionOrAShellInARealWorktreeOnly()
    {
        WorktreesViewState onClean = Press(Loaded, Key.Down, Key.Down);

        WorktreesView.Reduce(onClean, new KeyPressed(Key.Of('s'))).Effects.ShouldBe([new StartSessionIn(Clean)]);
        WorktreesView.Reduce(onClean, new KeyPressed(Key.Of('o'))).Effects.ShouldBe([new OpenShellTab(Clean)]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('s'))).State.Footer.ShouldBe(new Footer.LeftoverFolder("Northwind.Orders.worktrees"));
    }

    [Fact]
    public void JumpToTheSessionInsideAWorktreeOnEnter()
    {
        WorktreesViewState onBusy = Press(Loaded, Key.Down, Key.Down, Key.Down, Key.Down);

        WorktreesView.Reduce(onBusy, new KeyPressed(Key.Enter)).Effects.ShouldBe([new JumpToSession("sess-fix-3")]);
        WorktreesView.Reduce(Press(Loaded, Key.Down, Key.Down), new KeyPressed(Key.Enter)).State.Footer.ShouldBe(new Footer.NoSessionIn("Northwind.Orders-fix-1"));
    }

    [Fact]
    public void GoBackToTheSessionsViewOnWOrEscapeAndQuitHelpAsUsual()
    {
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('w'))).Effects.ShouldBe([new SwitchToSessions()]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Escape)).Effects.ShouldBe([new SwitchToSessions()]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('q'))).Effects.ShouldBe([new Quit()]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('?'))).Effects.ShouldBe([new ShowHelp()]);
    }

    [Fact]
    public void KnowWhetherTheFirstScanHasArrived()
    {
        WorktreesViewState.Empty.Loaded.ShouldBeFalse();
        Reduce(WorktreesViewState.Empty, new LoadFailed("git is not installed")).Loaded.ShouldBeFalse();
        Loaded.Loaded.ShouldBeTrue();
    }

    [Fact]
    public void ShowALoadFailureAsABanner()
    {
        Reduce(Loaded, new LoadFailed("git is not installed")).Banner.ShouldBe("git is not installed");
    }

    [Fact]
    public void ForceOnlyWhenTheTypedTextMatchesAndSayWhatWasTypedOtherwise()
    {
        WorktreesViewState onDirty = Loaded with { SelectedPath = Dirty.Path };

        WorktreesViewUpdate cancelled = WorktreesView.Reduce(onDirty, new ForceConfirmed(Dirty, "fix-2", Typed: null));
        WorktreesViewUpdate blank = WorktreesView.Reduce(onDirty, new ForceConfirmed(Dirty, "fix-2", Typed: string.Empty));
        WorktreesViewUpdate wrong = WorktreesView.Reduce(onDirty, new ForceConfirmed(Dirty, "fix-2", Typed: "fix-9"));
        WorktreesViewUpdate right = WorktreesView.Reduce(onDirty, new ForceConfirmed(Dirty, "fix-2", Typed: "fix-2"));

        cancelled.State.Footer.ShouldBe(new Footer.ForceCancelled());
        blank.State.Footer.ShouldBe(new Footer.NothingTyped());
        wrong.State.Footer.ShouldBe(new Footer.TypedWrongName("fix-9", "fix-2"));
        cancelled.Effects.Concat(blank.Effects).Concat(wrong.Effects).ShouldBeEmpty();
        right.Effects.ShouldBe([new RemoveWorktree(Dirty, Forced: true)]);
        right.State.Footer.ShouldBe(new Footer.Working(new RemoveWorktree(Dirty, Forced: true)));
    }

    [Fact]
    public void ExplainTheSelectedRowsHealthInTheFooterWhenNothingElseNeedsSaying()
    {
        WorktreesViewState onLocked = Loaded with { SelectedPath = Locked.Path };
        WorktreesViewState onClean = Loaded with { SelectedPath = Clean.Path };

        onLocked.Shown.ShouldBe(new Footer.Unhealthy("Northwind.Orders-locked", WorktreeHealthKind.Locked, "another tool is using it"));
        onClean.Shown.ShouldBe(new Footer.RemoveHint());
        (onLocked with { Footer = new Footer.Kept() }).Shown.ShouldBe(new Footer.Kept());
    }

    [Fact]
    public void ClearTheLastMessageWhenTheSelectionMovesSoTheNewRowsReasonShows()
    {
        WorktreesViewState afterDelete = Loaded with { SelectedPath = Clean.Path, Footer = new Footer.Reported(new RemoveWorktreeOutcome.DeletedFolder("Northwind.Orders.worktrees")) };

        Press(afterDelete, Key.Down).Footer.ShouldBe(Footer.None);
        Press(afterDelete, Key.Up).Footer.ShouldBe(Footer.None);
        Press(afterDelete, Key.Of('/'), Key.Of('k')).Footer.ShouldBe(Footer.None);
    }

    [Fact]
    public void MarkEffectsThatLeaveTheScreenAndThoseThatAskFirst()
    {
        new SwitchToSessions().ShouldBeAssignableTo<ILeavesScreen>();
        new JumpToSession("sess-1").ShouldBeAssignableTo<ILeavesScreen>();
        new ConfirmForceRemove(Dirty, "x").ShouldBeAssignableTo<IDialogEffect>();
        new StartSessionIn(Clean).ShouldBeAssignableTo<IDialogEffect>();
        new RemoveWorktree(Clean, Forced: false).ShouldBeAssignableTo<IDialogEffect>("a removal can end in the branch question");
        new PruneRepository(Repository).ShouldNotBeAssignableTo<ILeavesScreen>();
    }

    private static WorktreesViewState Press(WorktreesViewState state, params Key[] keys)
    {
        return keys.Aggregate(state, (current, key) => WorktreesView.Reduce(current, new KeyPressed(key)).State);
    }

    private static WorktreesViewState Reduce(WorktreesViewState state, ViewInput input)
    {
        return WorktreesView.Reduce(state, input).State;
    }

    private static WorktreeRow Row(string path, string? branch, WorktreeHealthKind health, string reason = "")
    {
        return new WorktreeRow(Repository, path, branch, new WorktreeHealth(health, reason), 0, 0, null, null, null, 0, OwnerExists: true, FolderExists: true);
    }
}