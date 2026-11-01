using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class SessionsViewShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private static readonly SessionRow Working = Row("remove-sonar", DisplayState.Working, "Northwind.Templates", jobId: "aaaa1111");
    private static readonly BackgroundSession WaitingJob = Job("change-feed", DisplayState.NeedsInput, "Northwind.Shipping", "bbbb2222");
    private static readonly SessionRow Waiting = Row(WaitingJob);
    private static readonly SessionRow Stopped = Row("old-spike", DisplayState.Stopped, "Source191.Vantage", jobId: "cccc3333");
    private static readonly InteractiveSession InteractiveTerminal = Terminal("repos-93", DisplayState.Idle, "repos");
    private static readonly SessionRow Interactive = Row(InteractiveTerminal);

    private static readonly SessionsViewState Loaded = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([Stopped, Working, Interactive, Waiting])).State;

    [Fact]
    public void ShowLoadedRowsOrderedByAttention()
    {
        Loaded.VisibleRows.Select(row => row.Session.Name).ShouldBe(["change-feed", "remove-sonar", "repos-93", "old-spike"]);
    }

    [Fact]
    public void KnowWhetherTheFirstListingHasArrived()
    {
        SessionsViewState.Empty.Loaded.ShouldBeFalse();
        Reduce(SessionsViewState.Empty, new LoadFailed("claude is not installed")).Loaded.ShouldBeFalse();
        Loaded.Loaded.ShouldBeTrue();
        Reduce(Loaded, new RowsLoaded([])).Loaded.ShouldBeTrue();
    }

    [Fact]
    public void KeepTheAutomatedRunsEachListingCarries()
    {
        AutomatedRuns one = new(1, MemorySize.FromMegabytes(216));

        SessionsViewState withRun = Reduce(Loaded, new RowsLoaded([Working]) { AutomatedRuns = one });

        withRun.AutomatedRuns.ShouldBe(one);
        withRun.Title.ShouldBe(Reduce(Loaded, new RowsLoaded([Working])).Title, "a run is not a row the title counts");
        Reduce(withRun, new RowsLoaded([Working])).AutomatedRuns.ShouldBe(AutomatedRuns.None);
        SessionsViewState.Empty.AutomatedRuns.ShouldBe(AutomatedRuns.None);
    }

    [Fact]
    public void KeepTheLastAutomatedRunsWhenAListingFails()
    {
        SessionsViewState withRun = Reduce(Loaded, new RowsLoaded([Working]) { AutomatedRuns = new AutomatedRuns(1, MemorySize.FromMegabytes(216)) });

        Reduce(withRun, new LoadFailed("claude is not installed")).AutomatedRuns.ShouldBe(new AutomatedRuns(1, MemorySize.FromMegabytes(216)));
    }

    [Fact]
    public void SelectTheFirstRowOnFirstLoad()
    {
        Loaded.Selected.ShouldBe(Waiting);
    }

    [Fact]
    public void KeepTheSelectedSessionAcrossARefreshThatReordersRows()
    {
        SessionsViewState selectedWorking = Press(Loaded, Key.Down);
        selectedWorking.Selected.ShouldBe(Working);

        SessionsViewState refreshed = Reduce(selectedWorking, new RowsLoaded([Working with { Session = Working.Session with { State = DisplayState.NeedsInput } }, Waiting, Stopped]));

        refreshed.Selected!.Session.SessionId.ShouldBe(Working.Session.SessionId);
    }

    [Fact]
    public void MoveTheSelectionToTheNearestRowWhenTheSelectedSessionDisappears()
    {
        SessionsViewState last = Press(Press(Press(Loaded, Key.Down), Key.Down), Key.Down);
        last.Selected.ShouldBe(Stopped);

        SessionsViewState refreshed = Reduce(last, new RowsLoaded([Waiting, Working, Interactive]));

        refreshed.Selected.ShouldBe(Interactive);
    }

    [Fact]
    public void KeepTheOldRowsAndShowABannerWhenALoadFails()
    {
        SessionsViewState failed = Reduce(Loaded, new LoadFailed("claude exited with code 1"));

        failed.VisibleRows.Count.ShouldBe(4);
        failed.Banner.ShouldBe(new LoadBanner.Failed("claude exited with code 1"));
    }

    [Fact]
    public void ClearTheBannerWhenTheNextLoadSucceeds()
    {
        SessionsViewState failed = Reduce(Loaded, new LoadFailed("boom"));

        Reduce(failed, new RowsLoaded([Working])).Banner.ShouldBe(new LoadBanner.None());
    }

    [Fact]
    public void NotMoveTheSelectionPastEitherEnd()
    {
        Press(Loaded, Key.Up).Selected.ShouldBe(Waiting);
        SessionsViewState bottom = Press(Press(Press(Press(Press(Loaded, Key.Down), Key.Down), Key.Down), Key.Down), Key.Down);
        bottom.Selected.ShouldBe(Stopped);
    }

    [Fact]
    public void FilterRowsAsTheFilterTextIsTyped()
    {
        SessionsViewState typing = Press(Press(Press(Loaded, Key.Slash), Key.Of('o'), Key.Of('l')));

        typing.FilterEditing.ShouldBeTrue();
        typing.VisibleRows.Select(row => row.Session.Name).ShouldBe(["old-spike"]);
        typing.Title.ShouldBe(new TableTitle("session", Shown: 1, Total: 4, Filter: "ol", Selected: "old-spike"));
    }

    [Fact]
    public void CommitTheFilterWithEnterAndClearItWithEscape()
    {
        SessionsViewState committed = Press(Loaded, Key.Slash, Key.Of('n'), Key.Of('o'), Key.Of('r'), Key.Enter);
        committed.FilterEditing.ShouldBeFalse();
        committed.VisibleRows.Count.ShouldBe(2);

        SessionsViewState cleared = Press(committed, Key.Escape);
        cleared.VisibleRows.Count.ShouldBe(4);
        cleared.Title.ShouldBe(new TableTitle("session", Shown: 4, Total: 4, Filter: string.Empty, Selected: "change-feed"));
    }

    [Fact]
    public void SelectASessionByIdWhenAskedTo()
    {
        Reduce(Loaded, new SelectSession(Stopped.Session.SessionId)).Selected.ShouldBe(Stopped);
        Reduce(Loaded, new SelectSession(SessionId.From("no-such-session"))).Selected.ShouldBe(Waiting);
    }

    [Fact]
    public void ClearTheFilterWhenTheSessionToSelectIsHiddenByIt()
    {
        SessionsViewState filtered = Press(Loaded, Key.Slash, Key.Of('o'), Key.Of('l'), Key.Enter);
        filtered.VisibleRows.ShouldBe([Stopped]);

        SessionsViewState selected = Reduce(filtered, new SelectSession(Working.Session.SessionId));

        selected.Selected.ShouldBe(Working);
        selected.Filter.Text.ShouldBeEmpty();
    }

    [Fact]
    public void StopCarriesTheSelectedRow()
    {
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('x'))).Effects.ShouldBe([new StopSession(WaitingJob)]);
    }

    [Fact]
    public void OpenTheSelectedBackgroundSessionInItsOwnTabOnEnter()
    {
        ViewUpdate update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Enter));

        update.Effects.ShouldBe([new OpenTabForSession(WaitingJob, new LiveTab.None())]);
    }

    [Fact]
    public void RefuseActionsOnAnInteractiveRow()
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);
        onInteractive.Selected.ShouldBe(Interactive);

        ViewUpdate update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Enter));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.RunsInOwnTerminal("repos-93", HasTranscript: true));
    }

    [Fact]
    public void AdoptAnIdleInteractiveSessionOnB()
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);

        ViewUpdate update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Of('b')));

        update.Effects.ShouldBe([new AdoptSession(InteractiveTerminal)]);
    }

    [Fact]
    public void RefuseToAdoptAnInteractiveSessionWhileItIsWorking()
    {
        SessionRow busy = Row("busy-terminal", DisplayState.Working, "repos", jobId: null, interactive: true);
        SessionsViewState state = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([busy])).State;

        ViewUpdate update = SessionsView.Reduce(state, new KeyPressed(Key.Of('b')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.WorkingSoAdoptWaits("busy-terminal"));
    }

    [Fact]
    public void ExplainThatABackgroundSessionNeedsNoAdoption()
    {
        ViewUpdate update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('b')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.AlreadyUnderVantage("change-feed"));
    }

    [Fact]
    public void RefuseToOpenAStoppedSessionThatWasNeverSentAMessageBecauseNothingCanResume()
    {
        BackgroundSession stubJob = Job("repos-f6", DisplayState.Failed, "repos", "5a4a7350");
        SessionRow stub = Row(stubJob) with { HasTranscript = false };
        SessionsViewState withStub = Reduce(Loaded, new RowsLoaded([stub]));

        Key[] keys = [Key.Enter, Key.Of('r')];
        foreach (Key key in keys)
        {
            ViewUpdate update = SessionsView.Reduce(withStub, new KeyPressed(key));

            update.Effects.ShouldBeEmpty();
            update.State.Message.ShouldBe(new StatusMessage.NeverSentAMessage("repos-f6"));
        }

        SessionsView.Reduce(withStub, new KeyPressed(Key.Of('d'))).Effects.ShouldBe([new DeleteSession(stubJob)]);
    }

    [Fact]
    public void OfferToCloseATerminalSessionThatHasNoMessagesAndCloseItOnX()
    {
        InteractiveSession untouchedTerminal = Terminal("repos-f6", DisplayState.Idle, "repos");
        SessionRow untouched = Row(untouchedTerminal) with { HasTranscript = false };
        SessionsViewState onUntouched = Reduce(SessionsViewState.Empty, new RowsLoaded([untouched]));

        ViewUpdate enter = SessionsView.Reduce(onUntouched, new KeyPressed(Key.Enter));
        ViewUpdate closed = SessionsView.Reduce(onUntouched, new KeyPressed(Key.Of('d')));

        enter.Effects.ShouldBeEmpty();
        enter.State.Message.ShouldBe(new StatusMessage.RunsInOwnTerminal("repos-f6", HasTranscript: false));
        closed.Effects.Single().ShouldBeOfType<CloseEmptyTerminals>().Sessions.ShouldBe([untouchedTerminal]);
    }

    [Fact]
    public void CloseEveryTerminalSessionWithNoMessagesOnCAndSayWhenThereAreNone()
    {
        InteractiveSession firstTerminal = Terminal("repos-f6", DisplayState.Idle, "repos");
        SessionRow first = Row(firstTerminal) with { HasTranscript = false };
        InteractiveSession secondTerminal = Terminal("repos-f7", DisplayState.Idle, "repos");
        SessionRow second = Row(secondTerminal) with { HasTranscript = false };
        SessionRow withMessages = Row("repos-31", DisplayState.Idle, "repos", jobId: null, interactive: true);
        SessionsViewState mixed = Reduce(SessionsViewState.Empty, new RowsLoaded([withMessages, first, Waiting, second]));

        ViewUpdate update = SessionsView.Reduce(mixed, new KeyPressed(Key.Of('C')));
        ViewUpdate none = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('C')));

        update.Effects.Single().ShouldBeOfType<CloseEmptyTerminals>().Sessions.ShouldBe([firstTerminal, secondTerminal], ignoreOrder: true);
        none.Effects.ShouldBeEmpty();
        none.State.Message.ShouldBe(new StatusMessage.NoEmptyTerminals());
    }

    [Fact]
    public void KeepRefusingToDeleteATerminalSessionThatHasAConversation()
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);

        ViewUpdate update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Of('d')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBeOfType<StatusMessage.RunsInOwnTerminal>().HasTranscript.ShouldBeTrue();
    }

    [Fact]
    public void SwitchToTheLiveTabThatShowsTheSessionOnEnter()
    {
        SessionsViewState withTab = Reduce(Loaded, new RowsLoaded([Waiting with { Tab = new LiveTab.Open(TabPalette.Default[0]) }]));

        ViewUpdate update = SessionsView.Reduce(withTab, new KeyPressed(Key.Enter));

        update.Effects.ShouldBe([new OpenTabForSession(WaitingJob, new LiveTab.Open(TabPalette.Default[0]))]);
        update.State.Message.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void DoNothingOnOBecauseEnterOpensTheTab()
    {
        SessionsViewState withTab = Reduce(Loaded, new RowsLoaded([Waiting with { Tab = new LiveTab.Open(TabPalette.Default[0]) }]));

        ViewUpdate update = SessionsView.Reduce(withTab, new KeyPressed(Key.Of('o')));

        update.Effects.ShouldBeEmpty();
        update.State.ShouldBe(withTab);
    }

    [Fact]
    public void AskForANewNameOnRAndRenameOnceOneIsGiven()
    {
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('r'))).Effects.ShouldBe([new AskForNewName(WaitingJob)]);

        ViewUpdate renamed = SessionsView.Reduce(Loaded, new RenameConfirmed(WaitingJob, "fix the tests"));
        ViewUpdate blank = SessionsView.Reduce(Loaded, new RenameConfirmed(WaitingJob, "   "));
        ViewUpdate semicolon = SessionsView.Reduce(Loaded, new RenameConfirmed(WaitingJob, "fix a;b"));
        ViewUpdate cancelled = SessionsView.Reduce(Loaded, new RenameCancelled(WaitingJob));

        renamed.Effects.ShouldBe([new RenameSession(WaitingJob, SessionName.From("fix the tests"))]);
        renamed.State.Message.ShouldBe(new StatusMessage.Working(new RenameSession(WaitingJob, SessionName.From("fix the tests"))));
        blank.Effects.ShouldBeEmpty();
        blank.State.Message.ShouldBe(new StatusMessage.InvalidName(NameProblem.Missing));
        semicolon.Effects.ShouldBe([new RenameSession(WaitingJob, SessionName.From("fix a;b"))]);
        cancelled.Effects.ShouldBeEmpty();
        cancelled.State.Message.ShouldBe(new StatusMessage.RenameCancelled());
    }

    [Fact]
    public void RefuseToRenameASessionWhileItIsWorkingBecauseRenamingRestartsIt()
    {
        SessionsViewState onWorking = Press(Loaded, Key.Down);

        ViewUpdate update = SessionsView.Reduce(onWorking, new KeyPressed(Key.Of('r')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.WorkingSoRenameWaits("remove-sonar"));
    }

    [Theory]
    [InlineData('x', typeof(StopSession))]
    [InlineData('d', typeof(DeleteSession))]
    [InlineData('r', typeof(AskForNewName))]
    [InlineData('n', typeof(OpenLaunchWizard))]
    [InlineData('R', typeof(ResumeStoppedSessions))]
    [InlineData('q', typeof(Quit))]
    [InlineData('?', typeof(ShowHelp))]
    [InlineData('U', typeof(UpdateVantage))]
    [InlineData('!', typeof(ShowProblemReport))]
    public void EmitTheEffectForTheKey(char key, Type effect)
    {
        ViewUpdate update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of(key)));

        update.Effects.Single().ShouldBeOfType(effect);
    }

    [Fact]
    public void ChooseATabWithTheTabKeys()
    {
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('1'))).Effects.ShouldBe([new SelectTab(ConsoleTab.Sessions)]);
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('2'))).Effects.ShouldBe([new SelectTab(ConsoleTab.Worktrees)]);
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('3'))).Effects.ShouldBe([new SelectTab(ConsoleTab.Config)]);
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Left)).Effects.ShouldBe([new PreviousTab()]);
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Right)).Effects.ShouldBe([new NextTab()]);
    }

    [Theory]
    [InlineData('w')]
    [InlineData(',')]
    public void IgnoreTheRetiredKeys(char key)
    {
        ViewUpdate update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of(key)));

        update.Effects.ShouldBeEmpty();
        update.State.ShouldBe(Loaded);
    }

    [Fact]
    public void ShowTheMemoryOfARunningSessionOnM()
    {
        BackgroundSession running = WaitingJob with { Process = new JobProcess.Running(5151) };
        SessionsViewState state = Reduce(SessionsViewState.Empty, new RowsLoaded([Row(running)]));

        ViewUpdate update = SessionsView.Reduce(state, new KeyPressed(Key.Of('m')));

        update.Effects.ShouldBe([new ShowMemory(running, 5151)]);
        update.State.Message.ShouldBe(StatusMessage.None);
        new ShowMemory(running, 5151).ShouldBeAssignableTo<IDialogEffect>();
    }

    [Fact]
    public void ShowTheMemoryOfATerminalSessionOnM()
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);
        onInteractive.Selected.ShouldBe(Interactive);

        ViewUpdate update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Of('m')));

        update.Effects.ShouldBe([new ShowMemory(InteractiveTerminal, 4242)]);
    }

    [Fact]
    public void SayThereIsNothingToMeasureForAStoppedSession()
    {
        SessionsViewState onStopped = Press(Press(Press(Loaded, Key.Down), Key.Down), Key.Down);
        onStopped.Selected.ShouldBe(Stopped);

        ViewUpdate update = SessionsView.Reduce(onStopped, new KeyPressed(Key.Of('m')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.NothingToMeasure("old-spike"));
    }

    [Fact]
    public void SayNoSessionIsSelectedOnM()
    {
        SessionsView.Reduce(SessionsViewState.Empty, new KeyPressed(Key.Of('m'))).State.Message.ShouldBe(new StatusMessage.NoSessionSelected());
    }

    [Fact]
    public void IgnoreTheRetiredAgentViewKey()
    {
        ViewUpdate update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('a')));

        update.Effects.ShouldBeEmpty();
        update.State.ShouldBe(Loaded);
    }

    [Fact]
    public void QuitOnceTheUpdateTabIsOpen()
    {
        SessionsView.Reduce(Loaded, new UpdateTabOpened()).Effects.ShouldBe([new Quit()]);
    }

    [Fact]
    public void TreatLettersAsFilterTextWhileEditingTheFilter()
    {
        SessionsViewState typing = Press(Loaded, Key.Slash);

        ViewUpdate update = SessionsView.Reduce(typing, new KeyPressed(Key.Of('q')));

        update.Effects.ShouldBeEmpty();
        update.State.Filter.Text.ShouldBe("q");
    }

    [Fact]
    public void ShowNothingUntilThereIsAMessage()
    {
        Loaded.Shown.ShouldBe(StatusMessage.None);
        (Loaded with { Message = new StatusMessage.Kept() }).Shown.ShouldBe(new StatusMessage.Kept());
    }

    [Fact]
    public void ClearAMessageWhenTheSelectionMovesOrTheFilterChanges()
    {
        SessionsViewState said = Loaded with { Message = new StatusMessage.Kept() };

        Press(said, Key.Down).Message.ShouldBe(StatusMessage.None);
        Press(said, Key.Up).Message.ShouldBe(StatusMessage.None);
        Press(said, Key.Of('/')).Message.ShouldBe(StatusMessage.None);
        Press(Press(said, Key.Of('/')) with { Message = new StatusMessage.Kept() }, Key.Of('k')).Message.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void HandTheResumeChecklistTheRowsInTheOrderTheTableShowsThem()
    {
        ViewUpdate update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('R')));

        update.Effects.Single().ShouldBeOfType<ResumeStoppedSessions>().Rows.ShouldBe(Loaded.VisibleRows);
    }

    [Fact]
    public void OpenTheSelectedRowsPullRequest()
    {
        PullRequest active = new(PullRequestHost.AzureDevOps, 4521, new Uri("https://dev.azure.com/northwind/Trading/_git/Northwind.Shipping/pullrequest/4521"), PullRequestState.Active, Noon);
        SessionsViewState state = Reduce(SessionsViewState.Empty, new RowsLoaded([Waiting with { PullRequest = new PullRequestLookup.Found(active) }]));

        SessionsView.Reduce(state, new KeyPressed(Key.Of('p'))).Effects.ShouldBe([new OpenPullRequest(active)]);
    }

    [Fact]
    public void ExplainAMissingPullRequest()
    {
        SessionsViewState state = Reduce(SessionsViewState.Empty, new RowsLoaded([Waiting with { PullRequest = new PullRequestLookup.Missing(PullRequestGap.NotSignedIn, new LookupHost.Known(PullRequestHost.GitHub)) }]));

        ViewUpdate update = SessionsView.Reduce(state, new KeyPressed(Key.Of('p')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.NoPullRequest("change-feed", PullRequestGap.NotSignedIn, new LookupHost.Known(PullRequestHost.GitHub)));
    }

    [Fact]
    public void NeedASelectionToOpenAPullRequest()
    {
        SessionsView.Reduce(SessionsViewState.Empty, new KeyPressed(Key.Of('p'))).State.Message.ShouldBe(new StatusMessage.NoSessionSelected());
    }

    [Fact]
    public void CopyTheSelectedSessionsFolder()
    {
        ViewUpdate update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('c')));

        update.Effects.ShouldBe([new CopyFolderPath(Waiting.Session.WorkingDirectory)]);
    }

    [Fact]
    public void OpenTheSelectedSessionsFolderInTheEditorWhateverKindOfSessionItIs()
    {
        SessionsViewState onTerminal = Press(Loaded, Key.Down, Key.Down);
        SessionsViewState onStopped = Press(Loaded, Key.Down, Key.Down, Key.Down);

        onTerminal.Selected.ShouldBe(Interactive);
        SessionsView.Reduce(onTerminal, new KeyPressed(Key.Of('e'))).Effects.ShouldBe([new OpenFolderInEditor(Interactive.Session.WorkingDirectory)]);
        onStopped.Selected.ShouldBe(Stopped);
        SessionsView.Reduce(onStopped, new KeyPressed(Key.Of('e'))).Effects.ShouldBe([new OpenFolderInEditor(Stopped.Session.WorkingDirectory)]);
    }

    [Fact]
    public void FindTheSelectedSessionsSolutionByItsFolderAndRepository()
    {
        ViewUpdate update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('v')));

        update.Effects.ShouldBe([new FindSolution(Waiting.Session.WorkingDirectory, "Northwind.Shipping")]);
    }

    [Theory]
    [InlineData('c')]
    [InlineData('e')]
    [InlineData('v')]
    public void SayNoSessionIsSelectedWhenAFolderKeyIsPressedOnAnEmptyTable(char key)
    {
        ViewUpdate update = SessionsView.Reduce(SessionsViewState.Empty, new KeyPressed(Key.Of(key)));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.NoSessionSelected());
    }

    [Fact]
    public void ClearTheLastMessageWhenAFolderKeyActs()
    {
        SessionsViewState withMessage = Loaded with { Message = new StatusMessage.RenameCancelled() };

        SessionsView.Reduce(withMessage, new KeyPressed(Key.Of('c'))).State.Message.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void MarkTheSolutionSearchAsAskingFirstAndTheOtherFolderEffectsAsBackground()
    {
        new FindSolution(Waiting.Session.WorkingDirectory, "Northwind.Shipping").ShouldBeAssignableTo<IDialogEffect>();
        new CopyFolderPath(Waiting.Session.WorkingDirectory).ShouldNotBeAssignableTo<IDialogEffect>();
        new OpenFolderInEditor(Waiting.Session.WorkingDirectory).ShouldNotBeAssignableTo<IDialogEffect>();
        new OpenSolution(@"C:\repos\Northwind.Shipping\Northwind.Shipping.sln").ShouldNotBeAssignableTo<IDialogEffect>();
    }
    [Fact]
    public void MarkEffectsThatLeaveTheScreenAndThoseThatAskFirst()
    {
        new AttachToSession(JobId.From("81cdb0f9")).ShouldBeAssignableTo<ILeavesScreen>();
        new NextTab().ShouldBeAssignableTo<ISwitchesTab>();
        new NextTab().ShouldNotBeAssignableTo<ILeavesScreen>();
        new Quit().ShouldBeAssignableTo<ILeavesScreen>();
        new AskForNewName(WaitingJob).ShouldBeAssignableTo<IDialogEffect>();
        new DeleteSession(WaitingJob).ShouldBeAssignableTo<IDialogEffect>();
        new ResumeStoppedSessions([]).ShouldBeAssignableTo<IDialogEffect>();
        new OpenLaunchWizard().ShouldBeAssignableTo<IDialogEffect>();
        new StopSession(WaitingJob).ShouldNotBeAssignableTo<ILeavesScreen>();
        new StopSession(WaitingJob).ShouldNotBeAssignableTo<IDialogEffect>();
    }

    private static SessionsViewState Press(SessionsViewState state, params Key[] keys)
    {
        return keys.Aggregate(state, (current, key) => SessionsView.Reduce(current, new KeyPressed(key)).State);
    }

    [Fact]
    public void ClearTheMessageItWasAskedToClear()
    {
        StatusMessage message = new StatusMessage.NoPathHere();
        SessionsViewState shown = Loaded with { Message = message };

        Reduce(shown, new MessageDismissed(message)).Message.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void KeepANewerMessageWithTheSameWords()
    {
        StatusMessage older = new StatusMessage.NoPathHere();
        StatusMessage newer = new StatusMessage.NoPathHere();
        SessionsViewState shown = Loaded with { Message = newer };

        Reduce(shown, new MessageDismissed(older)).Message.ShouldBeSameAs(newer);
    }
    private static SessionsViewState Reduce(SessionsViewState state, ViewInput input)
    {
        return SessionsView.Reduce(state, input).State;
    }

    /// <summary>A background job with the given job id, or plain claude in a tab (process 4242) when <paramref name="interactive"/>.</summary>
    private static SessionRow Row(string name, DisplayState state, string repository, string? jobId, bool interactive = false)
    {
        return interactive
            ? Row(Terminal(name, state, repository))
            : Row(Job(name, state, repository, jobId ?? throw new ArgumentNullException(nameof(jobId))));
    }

    /// <summary>The session in its repository's folder, on main.</summary>
    private static SessionRow Row(Session session)
    {
        return new SessionRow(session, new SessionLocation.InRepository(session.WorkingDirectory.Name, session.WorkingDirectory), new Head.OnBranch(BranchName.From("main")), new LiveTab.None(), HasTranscript: true);
    }

    private static BackgroundSession Job(string name, DisplayState state, string repository, string jobId)
    {
        return new BackgroundSession(SessionId.From(Guid.NewGuid().ToString()), JobId.From(jobId), name, FolderPath.From($@"C:\repos\{repository}"), Noon, state, new JobProcess.None());
    }

    private static InteractiveSession Terminal(string name, DisplayState state, string repository)
    {
        return new InteractiveSession(SessionId.From(Guid.NewGuid().ToString()), name, FolderPath.From($@"C:\repos\{repository}"), Noon, state, ProcessId: 4242);
    }
}