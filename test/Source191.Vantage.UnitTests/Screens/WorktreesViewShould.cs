using Source191.Vantage.Domain;
using Source191.Vantage.Screens;

namespace Source191.Vantage.UnitTests.Screens;

public sealed class WorktreesViewShould
{
    private const string Repository = @"C:\repos\Northwind.Orders";

    private static readonly WorktreeRow Clean = Row(@"C:\repos\Northwind.Orders-fix-1", "fix-1", WorktreeHealthKind.Ok);
    private static readonly WorktreeRow Dirty = Row(@"C:\repos\Northwind.Orders-fix-2", "fix-2", WorktreeHealthKind.Ok) with { DirtyFiles = 5, UnpushedCommits = 2 };
    private static readonly WorktreeRow Locked = Row(@"C:\repos\Northwind.Orders-locked", "maintenance", WorktreeHealthKind.Locked, "another tool is using it");
    private static readonly WorktreeRow Orphan = Row(@"C:\repos\Northwind.Orders\.claude\worktrees\fix-service-tests", null, WorktreeHealthKind.Orphan) with { LeftoverFileCount = 12 };
    private static readonly WorktreeRow Empty = Row(@"C:\repos\Northwind.Orders.worktrees", null, WorktreeHealthKind.Empty) with { Repository = @"C:\repos" };
    private static readonly WorktreeRow Gone = Row(@"C:\repos\Northwind.Retired-republish", null, WorktreeHealthKind.Broken) with { Repository = @"C:\repos\Northwind.Retired", OwnerExists = false };
    private static readonly WorktreeRow Busy = Row(@"C:\repos\Northwind.Orders-fix-3", "fix-3", WorktreeHealthKind.Ok) with { SessionName = "fix-the-tests" };

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
        update.State.Footer.ShouldBe("Not removed: 5 changed or untracked files would be lost. D forces after a typed confirmation.");
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

        WorktreesView.Reduce(onLocked, new KeyPressed(Key.Of('D'))).State.Footer.ShouldBe("Not removed: the worktree is locked.");
        WorktreesView.Reduce(onBusy, new KeyPressed(Key.Of('D'))).State.Footer.ShouldBe("Not removed: session fix-the-tests is using it.");
        WorktreesView.Reduce(onBusy, new KeyPressed(Key.Of('D'))).Effects.ShouldBeEmpty();
    }

    [Fact]
    public void DeleteAnEmptyLeftoverWithoutConfirmationAndConfirmAnOrphanWithFilesByFolderName()
    {
        WorktreesViewState onOrphan = Press(Loaded, Key.Down);

        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('d'))).Effects.ShouldBe([new RemoveWorktree(Empty, Forced: false)]);
        WorktreesView.Reduce(onOrphan, new KeyPressed(Key.Of('d'))).State.Footer.ShouldBe("Not removed: 12 files that git no longer tracks would be lost. D forces after a typed confirmation.");
        WorktreesView.Reduce(onOrphan, new KeyPressed(Key.Of('D'))).Effects.ShouldBe([new ConfirmForceRemove(Orphan, ConfirmationText: "fix-service-tests")]);
    }

    [Fact]
    public void PruneTheSelectedRowsRepositoryUnlessItHasGone()
    {
        WorktreesViewState onGone = Press(Loaded, Enumerable.Repeat(Key.Down, 6).ToArray());

        WorktreesView.Reduce(Press(Loaded, Key.Down, Key.Down), new KeyPressed(Key.Of('p'))).Effects.ShouldBe([new PruneRepository(Repository)]);
        WorktreesViewUpdate refused = WorktreesView.Reduce(onGone, new KeyPressed(Key.Of('p')));
        refused.Effects.ShouldBeEmpty();
        refused.State.Footer.ShouldBe("Nothing to prune: the repository Northwind.Retired no longer exists.");
    }

    [Fact]
    public void StartASessionOrAShellInARealWorktreeOnly()
    {
        WorktreesViewState onClean = Press(Loaded, Key.Down, Key.Down);

        WorktreesView.Reduce(onClean, new KeyPressed(Key.Of('s'))).Effects.ShouldBe([new StartSessionIn(Clean)]);
        WorktreesView.Reduce(onClean, new KeyPressed(Key.Of('o'))).Effects.ShouldBe([new OpenShellTab(Clean)]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('s'))).State.Footer.ShouldBe("Northwind.Orders.worktrees is a leftover folder, not a worktree.");
    }

    [Fact]
    public void JumpToTheSessionInsideAWorktreeOnEnter()
    {
        WorktreesViewState onBusy = Press(Loaded, Key.Down, Key.Down, Key.Down, Key.Down);

        WorktreesView.Reduce(onBusy, new KeyPressed(Key.Enter)).Effects.ShouldBe([new JumpToSession("fix-the-tests")]);
        WorktreesView.Reduce(Press(Loaded, Key.Down, Key.Down), new KeyPressed(Key.Enter)).State.Footer.ShouldBe("No session is running in Northwind.Orders-fix-1.");
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

        cancelled.State.Footer.ShouldBe("Cancelled; nothing removed.");
        blank.State.Footer.ShouldBe("Nothing was typed; nothing removed.");
        wrong.State.Footer.ShouldBe("You typed fix-9, not fix-2; nothing removed.");
        cancelled.Effects.Concat(blank.Effects).Concat(wrong.Effects).ShouldBeEmpty();
        right.Effects.ShouldBe([new RemoveWorktree(Dirty, Forced: true)]);
        right.State.Footer.ShouldBe("Removing Northwind.Orders-fix-2...");
    }

    [Fact]
    public void ExplainTheSelectedRowsHealthInTheFooterWhenNothingElseNeedsSaying()
    {
        WorktreesViewState onLocked = Loaded with { SelectedPath = Locked.Path };
        WorktreesViewState onClean = Loaded with { SelectedPath = Clean.Path };

        onLocked.FooterText.ShouldBe("Northwind.Orders-locked is locked: another tool is using it.");
        onClean.FooterText.ShouldBe("d removes only what is safe to remove");
        (onLocked with { Footer = "Pruning..." }).FooterText.ShouldBe("Pruning...");
    }

    [Fact]
    public void ClearTheLastMessageWhenTheSelectionMovesSoTheNewRowsReasonShows()
    {
        WorktreesViewState afterDelete = Loaded with { SelectedPath = Clean.Path, Footer = "Deleted the folder Northwind.Orders.worktrees." };

        Press(afterDelete, Key.Down).Footer.ShouldBe(string.Empty);
        Press(afterDelete, Key.Up).Footer.ShouldBe(string.Empty);
        Press(afterDelete, Key.Of('/'), Key.Of('k')).Footer.ShouldBe(string.Empty);
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
        return new WorktreeRow(Repository, path, branch, new WorktreeHealth(health, reason), 0, 0, null, null, 0, OwnerExists: true, FolderExists: true);
    }
}