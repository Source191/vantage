using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class WorktreesViewShould
{
    private const string Repository = @"C:\repos\Northwind.Orders";

    private static readonly WorktreeRow Clean = Row(@"C:\repos\Northwind.Orders-fix-1", On("fix-1"), WorktreeHealthKind.Ok);
    private static readonly WorktreeRow Dirty = Row(@"C:\repos\Northwind.Orders-fix-2", On("fix-2"), WorktreeHealthKind.Ok) with { DirtyFiles = 5, UnpushedCommits = 2 };
    private static readonly WorktreeRow Locked = Row(@"C:\repos\Northwind.Orders-locked", On("maintenance"), WorktreeHealthKind.Locked, new HealthReason.Locked("another tool is using it"));
    private static readonly WorktreeRow Orphan = Row(@"C:\repos\Northwind.Orders\.claude\worktrees\fix-service-tests", new Head.Unknown(), WorktreeHealthKind.Orphan) with { LeftoverFileCount = 12 };
    private static readonly WorktreeRow Empty = Row(@"C:\repos\Northwind.Orders.worktrees", new Head.Unknown(), WorktreeHealthKind.Empty) with { Repository = FolderPath.From(@"C:\repos") };
    private static readonly WorktreeRow Gone = Row(@"C:\repos\Northwind.Retired-republish", new Head.Unknown(), WorktreeHealthKind.Broken) with { Repository = FolderPath.From(@"C:\repos\Northwind.Retired"), OwnerExists = false };
    private static readonly WorktreeRow Busy = Row(@"C:\repos\Northwind.Orders-fix-3", On("fix-3"), WorktreeHealthKind.Ok) with { Session = new WorktreeSession.Running(SessionId.From("sess-fix-3"), "fix-the-tests") };
    private static readonly WorktreeRow Elsewhere = Row(@"C:\Users\me\source\worktrees\Northwind.Ledger-fix", On("fix"), WorktreeHealthKind.Elsewhere, new HealthReason.RepositoryOutsideRoots(FolderPath.From(@"D:\other\Northwind.Ledger"))) with { Repository = FolderPath.From(@"D:\other\Northwind.Ledger"), DirtyFiles = 3 };

    private static readonly WorktreesViewState Loaded = Reduce(WorktreesViewState.Empty, new WorktreeRowsLoaded([Gone, Busy, Empty, Orphan, Locked, Dirty, Clean]));

    [Fact]
    public void ChooseATabWithTheTabKeys()
    {
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('1'))).Effects.ShouldBe([new SelectTab(ConsoleTab.Sessions)]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('2'))).Effects.ShouldBe([new SelectTab(ConsoleTab.Worktrees)]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('3'))).Effects.ShouldBe([new SelectTab(ConsoleTab.Config)]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Left)).Effects.ShouldBe([new PreviousTab()]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Right)).Effects.ShouldBe([new NextTab()]);
    }

    [Fact]
    public void OrderRowsByRepositoryThenPathAndSelectTheFirst()
    {
        Loaded.VisibleRows.Select(row => row.FolderName).ShouldBe(["Northwind.Orders.worktrees", "fix-service-tests", "Northwind.Orders-fix-1", "Northwind.Orders-fix-2", "Northwind.Orders-fix-3", "Northwind.Orders-locked", "Northwind.Retired-republish"]);
        Loaded.Selected.ShouldBe(Empty);
        Loaded.Title.ShouldBe(new TableTitle("worktree", Shown: 7, Total: 7, Filter: string.Empty, Selected: "Northwind.Orders.worktrees"));
    }

    [Fact]
    public void MoveTheSelectionWithTheArrowKeysAndStopAtTheEnds()
    {
        Press(Loaded, Key.Down, Key.Down).Selected.ShouldBe(Clean);
        Press(Loaded, Key.Up).Selected.ShouldBe(Empty);
        Press(Loaded, [.. Enumerable.Repeat(Key.Down, 10)]).Selected.ShouldBe(Gone);
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
    public void FilterByABranchAndFindNoneInADetachedRow()
    {
        WorktreeRow detached = Row(@"C:\repos\Northwind.Orders-spike", new Head.Detached(), WorktreeHealthKind.Ok);
        WorktreesViewState state = Reduce(WorktreesViewState.Empty, new WorktreeRowsLoaded([Locked, detached]));

        Press(state, Key.Slash, Key.Of('m'), Key.Of('a'), Key.Of('i'), Key.Of('n'), Key.Of('t'), Key.Enter).VisibleRows.ShouldBe([Locked]);
        Press(state, Key.Slash, Key.Of('d'), Key.Of('e'), Key.Of('t'), Key.Of('a'), Key.Of('c'), Key.Enter).VisibleRows.ShouldBeEmpty();
    }

    [Fact]
    public void FilterByRepositoryFolderBranchOrHealth()
    {
        WorktreesViewState filtered = Press(Loaded, Key.Slash, Key.Of('o'), Key.Of('r'), Key.Of('p'), Key.Enter);

        filtered.VisibleRows.ShouldBe([Orphan]);
        filtered.Title.ShouldBe(new TableTitle("worktree", Shown: 1, Total: 7, Filter: "orp", Selected: "fix-service-tests"));
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
        update.State.Message.ShouldBe(new StatusMessage.NotRemoved(new RemovalReason.ChangesWouldBeLost(5), OfferForce: true));
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

        WorktreesView.Reduce(onLocked, new KeyPressed(Key.Of('D'))).State.Message.ShouldBe(new StatusMessage.NotRemoved(new RemovalReason.Locked(), OfferForce: false));
        WorktreesView.Reduce(onBusy, new KeyPressed(Key.Of('D'))).State.Message.ShouldBe(new StatusMessage.NotRemoved(new RemovalReason.LiveSession("fix-the-tests"), OfferForce: false));
        WorktreesView.Reduce(onBusy, new KeyPressed(Key.Of('D'))).Effects.ShouldBeEmpty();
    }

    [Fact]
    public void NotSuggestForceWhenTheRefusalCannotBeForced()
    {
        WorktreesViewState onLocked = Loaded with { SelectedPath = Locked.Path.Key };
        WorktreesViewState onBusy = Loaded with { SelectedPath = Busy.Path.Key };

        WorktreesView.Reduce(onLocked, new KeyPressed(Key.Of('d'))).State.Message.ShouldBe(new StatusMessage.NotRemoved(new RemovalReason.Locked(), OfferForce: false));
        WorktreesView.Reduce(onBusy, new KeyPressed(Key.Of('d'))).State.Message.ShouldBe(new StatusMessage.NotRemoved(new RemovalReason.LiveSession("fix-the-tests"), OfferForce: false));
        WorktreesView.Reduce(Loaded with { SelectedPath = Dirty.Path.Key }, new KeyPressed(Key.Of('d'))).State.Message.ShouldBeOfType<StatusMessage.NotRemoved>().OfferForce.ShouldBeTrue();
    }

    [Fact]
    public void DeleteAnEmptyLeftoverWithoutConfirmationAndConfirmAnOrphanWithFilesByFolderName()
    {
        WorktreesViewState onOrphan = Press(Loaded, Key.Down);

        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('d'))).Effects.ShouldBe([new RemoveWorktree(Empty, Forced: false)]);
        WorktreesView.Reduce(onOrphan, new KeyPressed(Key.Of('d'))).State.Message.ShouldBe(new StatusMessage.NotRemoved(new RemovalReason.LeftoverFilesWouldBeLost(12, AtLeast: false), OfferForce: true));
        WorktreesView.Reduce(onOrphan, new KeyPressed(Key.Of('D'))).Effects.ShouldBe([new ConfirmForceRemove(Orphan, ConfirmationText: "fix-service-tests")]);
    }

    [Fact]
    public void PruneWithCapitalPUnlessTheRepositoryHasGone()
    {
        WorktreesViewState onGone = Press(Loaded, [.. Enumerable.Repeat(Key.Down, 6)]);

        WorktreesView.Reduce(Press(Loaded, Key.Down, Key.Down), new KeyPressed(Key.Of('P'))).Effects.ShouldBe([new PruneRepository(FolderPath.From(Repository))]);
        WorktreesViewUpdate refused = WorktreesView.Reduce(onGone, new KeyPressed(Key.Of('P')));
        refused.Effects.ShouldBeEmpty();
        refused.State.Message.ShouldBe(new StatusMessage.NothingToPrune("Northwind.Retired"));
    }

    [Fact]
    public void NotPruneWithLowerP()
    {
        WorktreesView.Reduce(Press(Loaded, Key.Down, Key.Down), new KeyPressed(Key.Of('p'))).Effects.ShouldNotContain(effect => effect is PruneRepository);
    }

    [Fact]
    public void OpenTheSelectedRowsPullRequest()
    {
        PullRequest open = new(PullRequestHost.GitHub, 123, new Uri("https://github.com/northwind/Northwind.Orders/pull/123"), PullRequestState.Open, DateTimeOffset.UnixEpoch);
        WorktreesViewState state = Reduce(WorktreesViewState.Empty, new WorktreeRowsLoaded([Clean with { PullRequest = new PullRequestLookup.Found(open) }]));

        WorktreesView.Reduce(state, new KeyPressed(Key.Of('p'))).Effects.ShouldBe([new OpenPullRequest(open)]);
    }

    [Fact]
    public void ExplainAMissingPullRequest()
    {
        WorktreesViewState state = Reduce(WorktreesViewState.Empty, new WorktreeRowsLoaded([Clean with { PullRequest = new PullRequestLookup.Missing(PullRequestGap.Looking) }]));

        WorktreesViewUpdate update = WorktreesView.Reduce(state, new KeyPressed(Key.Of('p')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.NoPullRequest("Northwind.Orders-fix-1", PullRequestGap.Looking, new LookupHost.NotRead()));
    }

    [Fact]
    public void NeedASelectionToOpenAPullRequest()
    {
        WorktreesView.Reduce(WorktreesViewState.Empty, new KeyPressed(Key.Of('p'))).State.Message.ShouldBe(new StatusMessage.NoWorktreeSelected());
    }

    [Fact]
    public void StartASessionOrAShellInARealWorktreeOnly()
    {
        WorktreesViewState onClean = Press(Loaded, Key.Down, Key.Down);

        WorktreesView.Reduce(onClean, new KeyPressed(Key.Of('s'))).Effects.ShouldBe([new StartSessionIn(Clean)]);
        WorktreesView.Reduce(onClean, new KeyPressed(Key.Of('o'))).Effects.ShouldBe([new OpenShellTab(Clean)]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('s'))).State.Message.ShouldBe(new StatusMessage.LeftoverFolder("Northwind.Orders.worktrees"));
    }

    [Fact]
    public void TreatAnElsewhereWorktreeAsRealForSessionsAndShells()
    {
        WorktreesViewState onElsewhere = Reduce(WorktreesViewState.Empty, new WorktreeRowsLoaded([Clean, Elsewhere])) with { SelectedPath = Elsewhere.Path.Key };

        WorktreesView.Reduce(onElsewhere, new KeyPressed(Key.Of('s'))).Effects.ShouldBe([new StartSessionIn(Elsewhere)]);
        WorktreesView.Reduce(onElsewhere, new KeyPressed(Key.Of('o'))).Effects.ShouldBe([new OpenShellTab(Elsewhere)]);
    }

    [Fact]
    public void NeverRemoveAnElsewhereWorktreeNorOfferForce()
    {
        WorktreesViewState onElsewhere = Reduce(WorktreesViewState.Empty, new WorktreeRowsLoaded([Clean, Elsewhere])) with { SelectedPath = Elsewhere.Path.Key };

        WorktreesView.Reduce(onElsewhere, new KeyPressed(Key.Of('d'))).State.Message.ShouldBe(new StatusMessage.NotRemoved(new RemovalReason.RepositoryOutsideRoots(), OfferForce: false));
        WorktreesView.Reduce(onElsewhere, new KeyPressed(Key.Of('D'))).State.Message.ShouldBe(new StatusMessage.NotRemoved(new RemovalReason.RepositoryOutsideRoots(), OfferForce: false));
        WorktreesView.Reduce(onElsewhere, new KeyPressed(Key.Of('D'))).Effects.ShouldBeEmpty();
    }

    [Fact]
    public void JumpToTheSessionInsideAWorktreeOnEnter()
    {
        WorktreesViewState onBusy = Press(Loaded, Key.Down, Key.Down, Key.Down, Key.Down);

        WorktreesView.Reduce(onBusy, new KeyPressed(Key.Enter)).Effects.ShouldBe([new JumpToSession(SessionId.From("sess-fix-3"))]);
        WorktreesView.Reduce(Press(Loaded, Key.Down, Key.Down), new KeyPressed(Key.Enter)).State.Message.ShouldBe(new StatusMessage.NoSessionIn("Northwind.Orders-fix-1"));
    }

    [Fact]
    public void AskToUpdateOnCapitalU()
    {
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('U'))).Effects.ShouldBe([new UpdateVantage()]);
    }

    [Fact]
    public void QuitOnceTheUpdateTabIsOpen()
    {
        WorktreesView.Reduce(Loaded, new UpdateTabOpened()).Effects.ShouldBe([new Quit()]);
    }

    [Fact]
    public void IgnoreTheRetiredKeys()
    {
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of(','))).Effects.ShouldBeEmpty();
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('w'))).Effects.ShouldBeEmpty();
    }

    [Fact]
    public void GoBackToTheSessionsViewOnWOrEscapeAndQuitHelpAsUsual()
    {
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Escape)).Effects.ShouldBe([new SelectTab(ConsoleTab.Sessions)]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('q'))).Effects.ShouldBe([new Quit()]);
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('?'))).Effects.ShouldBe([new ShowHelp()]);
    }

    [Fact]
    public void ReportAProblemOnBang()
    {
        WorktreesView.Reduce(Loaded, new KeyPressed(Key.Of('!'))).Effects.ShouldBe([new ShowProblemReport()]);
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
        WorktreesViewState onDirty = Loaded with { SelectedPath = Dirty.Path.Key };

        WorktreesViewUpdate cancelled = WorktreesView.Reduce(onDirty, new ForceCancelled(Dirty));
        WorktreesViewUpdate blank = WorktreesView.Reduce(onDirty, new ForceConfirmed(Dirty, "fix-2", Typed: string.Empty));
        WorktreesViewUpdate wrong = WorktreesView.Reduce(onDirty, new ForceConfirmed(Dirty, "fix-2", Typed: "fix-9"));
        WorktreesViewUpdate right = WorktreesView.Reduce(onDirty, new ForceConfirmed(Dirty, "fix-2", Typed: "fix-2"));

        cancelled.State.Message.ShouldBe(new StatusMessage.ForceCancelled());
        blank.State.Message.ShouldBe(new StatusMessage.NothingTyped());
        wrong.State.Message.ShouldBe(new StatusMessage.TypedWrongName("fix-9", "fix-2"));
        cancelled.Effects.Concat(blank.Effects).Concat(wrong.Effects).ShouldBeEmpty();
        right.Effects.ShouldBe([new RemoveWorktree(Dirty, Forced: true)]);
        right.State.Message.ShouldBe(new StatusMessage.Working(new RemoveWorktree(Dirty, Forced: true)));
    }

    [Fact]
    public void ShowOnlyMessagesTheHealthBeingThePanes()
    {
        WorktreesViewState onLocked = Loaded with { SelectedPath = Locked.Path.Key };
        WorktreesViewState onClean = Loaded with { SelectedPath = Clean.Path.Key };

        onLocked.Shown.ShouldBe(StatusMessage.None);
        onClean.Shown.ShouldBe(StatusMessage.None);
        (onLocked with { Message = new StatusMessage.Kept() }).Shown.ShouldBe(new StatusMessage.Kept());
    }

    [Fact]
    public void ClearTheLastMessageWhenTheSelectionMovesSoTheNewRowsReasonShows()
    {
        WorktreesViewState afterDelete = Loaded with { SelectedPath = Clean.Path.Key, Message = new StatusMessage.Reported(new RemoveWorktreeOutcome.DeletedFolder("Northwind.Orders.worktrees")) };

        Press(afterDelete, Key.Down).Message.ShouldBe(StatusMessage.None);
        Press(afterDelete, Key.Up).Message.ShouldBe(StatusMessage.None);
        Press(afterDelete, Key.Of('/'), Key.Of('k')).Message.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void MarkEffectsThatLeaveTheScreenAndThoseThatAskFirst()
    {
        new SelectTab(ConsoleTab.Sessions).ShouldBeAssignableTo<ISwitchesTab>();
        new PreviousTab().ShouldNotBeAssignableTo<ILeavesScreen>();
        new JumpToSession(SessionId.From("sess-1")).ShouldBeAssignableTo<ISwitchesTab>();
        new Quit().ShouldBeAssignableTo<ILeavesScreen>();
        new ConfirmForceRemove(Dirty, "x").ShouldBeAssignableTo<IDialogEffect>();
        new StartSessionIn(Clean).ShouldBeAssignableTo<IDialogEffect>();
        new RemoveWorktree(Clean, Forced: false).ShouldBeAssignableTo<IDialogEffect>("a removal can end in the branch question");
        new PruneRepository(FolderPath.From(Repository)).ShouldNotBeAssignableTo<ILeavesScreen>();
    }

    [Fact]
    public void CopyTheSelectedWorktreesFolder()
    {
        WorktreesViewState onClean = Press(Loaded, Key.Down, Key.Down);

        WorktreesView.Reduce(onClean, new KeyPressed(Key.Of('c'))).Effects.ShouldBe([new CopyFolderPath(Clean.Path)]);
    }

    [Fact]
    public void OpenTheSelectedWorktreeInTheEditorAndFindItsSolution()
    {
        WorktreesViewState onClean = Press(Loaded, Key.Down, Key.Down);

        WorktreesView.Reduce(onClean, new KeyPressed(Key.Of('e'))).Effects.ShouldBe([new OpenFolderInEditor(Clean.Path)]);
        WorktreesView.Reduce(onClean, new KeyPressed(Key.Of('v'))).Effects.ShouldBe([new FindSolution(Clean.Path, "Northwind.Orders")]);
    }

    [Theory]
    [InlineData('c')]
    [InlineData('e')]
    [InlineData('v')]
    public void RefuseAFolderKeyOnALeftoverWhoseFolderIsGone(char key)
    {
        WorktreeRow vanished = Orphan with { FolderExists = false };
        WorktreesViewState state = Reduce(WorktreesViewState.Empty, new WorktreeRowsLoaded([vanished]));

        WorktreesViewUpdate update = WorktreesView.Reduce(state, new KeyPressed(Key.Of(key)));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.LeftoverFolder("fix-service-tests"));
    }

    [Fact]
    public void ActOnALeftoverWhoseFolderStillExists()
    {
        WorktreesViewState state = Reduce(WorktreesViewState.Empty, new WorktreeRowsLoaded([Orphan]));

        WorktreesView.Reduce(state, new KeyPressed(Key.Of('e'))).Effects.ShouldBe([new OpenFolderInEditor(Orphan.Path)]);
    }

    [Theory]
    [InlineData('c')]
    [InlineData('e')]
    [InlineData('v')]
    public void SayNoWorktreeIsSelectedWhenAFolderKeyIsPressedOnAnEmptyTable(char key)
    {
        WorktreesViewUpdate update = WorktreesView.Reduce(WorktreesViewState.Empty, new KeyPressed(Key.Of(key)));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.NoWorktreeSelected());
    }
    private static WorktreesViewState Press(WorktreesViewState state, params Key[] keys)
    {
        return keys.Aggregate(state, (current, key) => WorktreesView.Reduce(current, new KeyPressed(key)).State);
    }

    [Fact]
    public void ClearTheMessageItWasAskedToClear()
    {
        StatusMessage message = new StatusMessage.NoPathHere();
        WorktreesViewState shown = Loaded with { Message = message };

        Reduce(shown, new MessageDismissed(message)).Message.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void KeepANewerMessageWithTheSameWords()
    {
        StatusMessage older = new StatusMessage.NoPathHere();
        StatusMessage newer = new StatusMessage.NoPathHere();
        WorktreesViewState shown = Loaded with { Message = newer };

        Reduce(shown, new MessageDismissed(older)).Message.ShouldBeSameAs(newer);
    }
    private static WorktreesViewState Reduce(WorktreesViewState state, ViewInput input)
    {
        return WorktreesView.Reduce(state, input).State;
    }

    private static Head.OnBranch On(string branch) => new(BranchName.From(branch));

    private static WorktreeRow Row(string path, Head head, WorktreeHealthKind health, HealthReason? reason = null)
    {
        return new WorktreeRow(FolderPath.From(Repository), FolderPath.From(path), head, new WorktreeHealth(health, reason ?? new HealthReason.None()), 0, 0, new LastCommit.Unknown(), new WorktreeSession.None(), 0, OwnerExists: true, FolderExists: true);
    }
}