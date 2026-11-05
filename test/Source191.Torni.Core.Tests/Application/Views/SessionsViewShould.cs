using Source191.Torni.Application;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application.Views;

public sealed class SessionsViewShould
{
    private static readonly SessionRow Working = Row("remove-sonar", DisplayState.Working, "Northwind.Templates", jobId: "aaaa1111");
    private static readonly Session.Background WaitingJob = Job("change-feed", DisplayState.NeedsInput, "Northwind.Shipping", "bbbb2222");
    private static readonly SessionRow Waiting = Row(WaitingJob);
    private static readonly SessionRow Stopped = Row("old-spike", DisplayState.Stopped, "Northwind.Archive", jobId: "cccc3333");
    private static readonly Session.Interactive InteractiveTerminal = Terminal("repos-93", DisplayState.Idle, "repos");
    private static readonly SessionRow Interactive = Row(InteractiveTerminal);

    private static readonly SessionsViewState Loaded = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([Stopped, Working, Interactive, Waiting])).State;

    [Fact]
    public void ShowLoadedRowsOrderedByAttention()
    {
        Loaded.VisibleRows.Select(row => row.Session.Name).ShouldBe([Listed.As("change-feed"), Listed.As("remove-sonar"), Listed.As("repos-93"), Listed.As("old-spike")]);
    }

    [Fact]
    public void KnowWhetherTheFirstListingHasArrived()
    {
        SessionsViewState.Empty.List.Loaded.ShouldBeFalse();
        Reduce(SessionsViewState.Empty, new LoadFailed("claude is not installed")).List.Loaded.ShouldBeFalse();
        Loaded.List.Loaded.ShouldBeTrue();
        Reduce(Loaded, new RowsLoaded([])).List.Loaded.ShouldBeTrue();
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

        refreshed.Selected.ShouldNotBeNull().Session.SessionId.ShouldBe(Working.Session.SessionId);
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

        typing.List.FilterEditing.ShouldBeTrue();
        typing.VisibleRows.Select(row => row.Session.Name).ShouldBe([Listed.As("old-spike")]);
        typing.Title.ShouldBe(new TableTitle("session", Shown: 1, Total: 4, Filter: "ol", Selected: "old-spike"));
    }

    [Fact]
    public void CommitTheFilterWithEnterAndClearItWithEscape()
    {
        SessionsViewState committed = Press(Loaded, Key.Slash, Key.Of('n'), Key.Of('o'), Key.Of('r'), Key.Enter);
        committed.List.FilterEditing.ShouldBeFalse();
        committed.VisibleRows.Count.ShouldBe(3);

        SessionsViewState cleared = Press(committed, Key.Escape);
        cleared.VisibleRows.Count.ShouldBe(4);
        cleared.Title.ShouldBe(new TableTitle("session", Shown: 4, Total: 4, Filter: string.Empty, Selected: "change-feed"));
    }

    [Fact]
    public void StopCarriesTheSelectedRow()
    {
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('x'))).Effects.ShouldBe([new StopSession(WaitingJob)]);
    }

    [Fact]
    public void OpenTheSelectedBackgroundSessionInItsOwnTabOnEnter()
    {
        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Enter));

        update.Effects.ShouldBe([new OpenTabForSession(WaitingJob, new LiveTab.None())]);
    }

    [Fact]
    public void RefuseActionsOnAnInteractiveRow()
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);
        onInteractive.Selected.ShouldBe(Interactive);

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Enter));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.RunsInOwnTerminal(Listed.As("repos-93"), HasTranscript: true));
    }

    [Theory]
    [InlineData('x')]
    [InlineData('r')]
    public void RefuseToStopOrRenameAnInteractiveRow(char key)
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);
        onInteractive.Selected.ShouldBe(Interactive);

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Of(key)));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.RunsInOwnTerminal(Listed.As("repos-93"), HasTranscript: true));
    }

    [Theory]
    [InlineData("Enter")]
    [InlineData("x")]
    [InlineData("d")]
    [InlineData("r")]
    [InlineData("b")]
    public void SayNoSessionIsSelectedOnARowKeyOnAnEmptyTable(string key)
    {
        Key pressed = key == "Enter" ? Key.Enter : Key.Of(key[0]);

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(SessionsViewState.Empty, new KeyPressed(pressed));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.NoSessionSelected());
    }

    [Fact]
    public void AdoptAnIdleInteractiveSessionOnB()
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Of('b')));

        update.Effects.ShouldBe([new AdoptSession(InteractiveTerminal)]);
    }

    [Fact]
    public void RefuseToAdoptAnInteractiveSessionWhileItIsWorking()
    {
        SessionRow busy = Row("busy-terminal", DisplayState.Working, "repos", jobId: null, interactive: true);
        SessionsViewState state = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([busy])).State;

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(state, new KeyPressed(Key.Of('b')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.WorkingSoAdoptWaits(Listed.As("busy-terminal")));
    }

    [Fact]
    public void ExplainThatABackgroundSessionNeedsNoAdoption()
    {
        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('b')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.AlreadyInBackground(Listed.As("change-feed")));
    }

    [Fact]
    public void RefuseToOpenAStoppedSessionThatWasNeverSentAMessageBecauseNothingCanResume()
    {
        Session.Background stubJob = Job("repos-f6", DisplayState.Failed, "repos", "5a4a7350");
        SessionRow stub = Row(stubJob) with { HasTranscript = false };
        SessionsViewState withStub = Reduce(Loaded, new RowsLoaded([stub]));

        Key[] keys = [Key.Enter, Key.Of('r')];
        foreach (Key key in keys)
        {
            ViewUpdate<SessionsViewState> update = SessionsView.Reduce(withStub, new KeyPressed(key));

            update.Effects.ShouldBeEmpty();
            update.State.Message.ShouldBe(new StatusMessage.NeverSentAMessage(Listed.As("repos-f6")));
        }

        SessionsView.Reduce(withStub, new KeyPressed(Key.Of('d'))).Effects.ShouldBe([new DeleteSession(stubJob)]);
    }

    [Fact]
    public void OfferToCloseATerminalSessionThatHasNoMessagesAndCloseItOnX()
    {
        Session.Interactive untouchedTerminal = Terminal("repos-f6", DisplayState.Idle, "repos");
        SessionRow untouched = Row(untouchedTerminal) with { HasTranscript = false };
        SessionsViewState onUntouched = Reduce(SessionsViewState.Empty, new RowsLoaded([untouched]));

        ViewUpdate<SessionsViewState> enter = SessionsView.Reduce(onUntouched, new KeyPressed(Key.Enter));
        ViewUpdate<SessionsViewState> closed = SessionsView.Reduce(onUntouched, new KeyPressed(Key.Of('d')));

        enter.Effects.ShouldBeEmpty();
        enter.State.Message.ShouldBe(new StatusMessage.RunsInOwnTerminal(Listed.As("repos-f6"), HasTranscript: false));
        closed.Effects.Single().ShouldBeOfType<CloseEmptyTerminals>().Sessions.ShouldBe([untouchedTerminal]);
    }

    [Fact]
    public void CloseEveryTerminalSessionWithNoMessagesOnCAndSayWhenThereAreNone()
    {
        Session.Interactive firstTerminal = Terminal("repos-f6", DisplayState.Idle, "repos");
        SessionRow first = Row(firstTerminal) with { HasTranscript = false };
        Session.Interactive secondTerminal = Terminal("repos-f7", DisplayState.Idle, "repos");
        SessionRow second = Row(secondTerminal) with { HasTranscript = false };
        SessionRow withMessages = Row("repos-31", DisplayState.Idle, "repos", jobId: null, interactive: true);
        SessionsViewState mixed = Reduce(SessionsViewState.Empty, new RowsLoaded([withMessages, first, Waiting, second]));

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(mixed, new KeyPressed(Key.Of('C')));
        ViewUpdate<SessionsViewState> none = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('C')));

        update.Effects.Single().ShouldBeOfType<CloseEmptyTerminals>().Sessions.ShouldBe([firstTerminal, secondTerminal], ignoreOrder: true);
        none.Effects.ShouldBeEmpty();
        none.State.Message.ShouldBe(new StatusMessage.NoEmptyTerminals());
    }

    [Fact]
    public void KeepRefusingToDeleteATerminalSessionThatHasAConversation()
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Of('d')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBeOfType<StatusMessage.RunsInOwnTerminal>().HasTranscript.ShouldBeTrue();
    }

    [Fact]
    public void SwitchToTheLiveTabThatShowsTheSessionOnEnter()
    {
        SessionsViewState withTab = Reduce(Loaded, new RowsLoaded([Waiting with { Tab = new LiveTab.Open(TabPalette.Default[0]) }]));

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(withTab, new KeyPressed(Key.Enter));

        update.Effects.ShouldBe([new OpenTabForSession(WaitingJob, new LiveTab.Open(TabPalette.Default[0]))]);
        update.State.Message.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void DoNothingOnOBecauseEnterOpensTheTab()
    {
        SessionsViewState withTab = Reduce(Loaded, new RowsLoaded([Waiting with { Tab = new LiveTab.Open(TabPalette.Default[0]) }]));

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(withTab, new KeyPressed(Key.Of('o')));

        update.Effects.ShouldBeEmpty();
        update.State.ShouldBe(withTab);
    }

    [Fact]
    public void AskForANewNameOnRAndRenameOnceOneIsGiven()
    {
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('r'))).Effects.ShouldBe([new AskForNewName(WaitingJob)]);

        ViewUpdate<SessionsViewState> renamed = SessionsView.Reduce(Loaded, new RenameConfirmed(WaitingJob, "fix the tests"));
        ViewUpdate<SessionsViewState> blank = SessionsView.Reduce(Loaded, new RenameConfirmed(WaitingJob, "   "));
        ViewUpdate<SessionsViewState> semicolon = SessionsView.Reduce(Loaded, new RenameConfirmed(WaitingJob, "fix a;b"));
        ViewUpdate<SessionsViewState> cancelled = SessionsView.Reduce(Loaded, new RenameCancelled(WaitingJob));

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

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(onWorking, new KeyPressed(Key.Of('r')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.WorkingSoRenameWaits(Listed.As("remove-sonar")));
    }

    [Theory]
    [InlineData('x', typeof(StopSession))]
    [InlineData('d', typeof(DeleteSession))]
    [InlineData('r', typeof(AskForNewName))]
    [InlineData('n', typeof(OpenLaunchWizard))]
    [InlineData('R', typeof(ResumeSessions))]
    [InlineData('q', typeof(Quit))]
    [InlineData('?', typeof(ShowHelp))]
    [InlineData('U', typeof(SelfUpdate))]
    [InlineData('!', typeof(ShowProblemReport))]
    public void EmitTheEffectForTheKey(char key, Type effect)
    {
        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of(key)));

        update.Effects.Single().ShouldBeOfType(effect);
    }

    [Fact]
    public void ChooseATabWithTheTabKeys()
    {
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('2'))).Effects.OfType<SelectTab>().ShouldBeEmpty();
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Left)).Effects.ShouldBe([new PreviousTab()]);
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Right)).Effects.ShouldBe([new NextTab()]);
    }

    [Theory]
    [InlineData('w')]
    [InlineData(',')]
    [InlineData('e')]
    [InlineData('v')]
    public void IgnoreTheRetiredKeys(char key)
    {
        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of(key)));

        update.Effects.ShouldBeEmpty();
        update.State.ShouldBe(Loaded);
    }

    [Fact]
    public void ShowTheMemoryOfARunningSessionOnM()
    {
        Session.Background running = WaitingJob with { Process = new JobProcess.Running(5151) };
        SessionsViewState state = Reduce(SessionsViewState.Empty, new RowsLoaded([Row(running)]));

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(state, new KeyPressed(Key.Of('m')));

        update.Effects.ShouldBe([new ShowMemory(running, 5151)]);
        update.State.Message.ShouldBe(StatusMessage.None);
        new ShowMemory(running, 5151).ShouldBeAssignableTo<IDialogEffect>();
    }

    [Fact]
    public void ShowTheMemoryOfATerminalSessionOnM()
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);
        onInteractive.Selected.ShouldBe(Interactive);

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Of('m')));

        update.Effects.ShouldBe([new ShowMemory(InteractiveTerminal, 4242)]);
    }

    [Fact]
    public void SayThereIsNothingToMeasureForAStoppedSession()
    {
        SessionsViewState onStopped = Press(Press(Press(Loaded, Key.Down), Key.Down), Key.Down);
        onStopped.Selected.ShouldBe(Stopped);

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(onStopped, new KeyPressed(Key.Of('m')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.NothingToMeasure(Listed.As("old-spike")));
    }

    [Fact]
    public void SayNoSessionIsSelectedOnM()
    {
        SessionsView.Reduce(SessionsViewState.Empty, new KeyPressed(Key.Of('m'))).State.Message.ShouldBe(new StatusMessage.NoSessionSelected());
    }

    [Fact]
    public void IgnoreTheRetiredAgentViewKey()
    {
        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('a')));

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

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(typing, new KeyPressed(Key.Of('q')));

        update.Effects.ShouldBeEmpty();
        update.State.List.Filter.Text.ShouldBe("q");
    }

    [Fact]
    public void ShowNothingUntilThereIsAMessage()
    {
        Loaded.Message.ShouldBe(StatusMessage.None);
        (Loaded with { Message = new StatusMessage.Kept() }).Message.ShouldBe(new StatusMessage.Kept());
    }

    [Fact]
    public void ClearAMessageWhenTheSelectionMovesOrTheFilterChanges()
    {
        SessionsViewState said = Loaded with { Message = new StatusMessage.Kept() };

        Press(said, Key.Down).Message.ShouldBe(StatusMessage.None);
        Press(said, Key.Up).Message.ShouldBe(StatusMessage.None);
        Press(said, Key.Of('/')).Message.ShouldBe(StatusMessage.None);
        Press(Press(said, Key.Of('/')) with { Message = new StatusMessage.Kept() }, Key.Of('k')).Message.ShouldBe(StatusMessage.None);

        SessionsViewState filtered = Press(Loaded, Key.Slash, Key.Of('n'), Key.Enter) with { Message = new StatusMessage.Kept() };
        SessionsViewState cleared = Press(filtered, Key.Escape);
        cleared.List.Filter.Text.ShouldBeEmpty();
        cleared.Message.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void DoNothingOnEscapeWithNoFilter()
    {
        SessionsViewState said = Loaded with { Message = new StatusMessage.Kept() };

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(said, new KeyPressed(Key.Escape));

        update.State.ShouldBe(said);
        update.Effects.ShouldBeEmpty();
    }

    [Fact]
    public void ResumeEveryShownStoppedOrFailedSessionInTableOrder()
    {
        SessionRow failed = Row("broken-build", DisplayState.Failed, "Northwind.Billing", jobId: "dddd4444");
        SessionsViewState loaded = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([Stopped, Working, Interactive, failed])).State;
        List<Session.Background> expected = [.. loaded.VisibleRows.Where(row => row == Stopped || row == failed).Select(row => (Session.Background)row.Session)];

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(loaded, new KeyPressed(Key.Of('R')));

        expected.Count.ShouldBe(2);
        update.Effects.Single().ShouldBeOfType<ResumeSessions>().Sessions.ShouldBe(expected, "the working and terminal rows are left alone, and the rest go in the order the table shows them");
    }

    [Fact]
    public void ResumeOnlyTheSessionsTheFilterShows()
    {
        SessionRow otherStopped = Row("spare-spike", DisplayState.Stopped, "Northwind.Billing", jobId: "eeee5555");
        SessionsViewState loaded = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([Stopped, otherStopped, Working])).State;
        SessionsViewState filtered = Press(loaded, Key.Slash, Key.Of('o'), Key.Of('l'), Key.Enter);

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(filtered, new KeyPressed(Key.Of('R')));

        update.Effects.Single().ShouldBeOfType<ResumeSessions>().Sessions.ShouldBe([(Session.Background)Stopped.Session]);
    }

    [Fact]
    public void SayThereIsNothingToResume()
    {
        SessionsViewState loaded = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([Working, Interactive, Waiting])).State;

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(loaded, new KeyPressed(Key.Of('R')));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.NothingToResume());
    }

    [Fact]
    public void OpenTheSelectedRowsPullRequest()
    {
        PullRequest active = new(PullRequestHost.AzureDevOps, 4521, new Uri("https://dev.azure.com/northwind/Trading/_git/Northwind.Shipping/pullrequest/4521"), PullRequestState.Active, Fixtures.Noon);
        SessionsViewState state = Reduce(SessionsViewState.Empty, new RowsLoaded([Waiting with { PullRequest = new PullRequestLookup.Found(active) }]));

        SessionsView.Reduce(state, new KeyPressed(Key.Of('p'))).Effects.ShouldBe([new OpenPullRequest(active)]);
    }

    [Fact]
    public void ExplainAMissingPullRequest()
    {
        SessionsViewState state = Reduce(SessionsViewState.Empty, new RowsLoaded([Waiting with { PullRequest = new PullRequestLookup.Missing(PullRequestGap.NotSignedIn, new LookupHost.Known(PullRequestHost.GitHub)) }]));

        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(state, new KeyPressed(Key.Of('p')));

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
        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('c')));

        update.Effects.ShouldBe([new CopyFolderPath(Waiting.Session.WorkingDirectory)]);
    }

    [Fact]
    public void OpenTheSelectedSessionsFolderInTheEditorWhateverKindOfSessionItIs()
    {
        SessionsViewState onTerminal = Press(Loaded, Key.Down, Key.Down);
        SessionsViewState onStopped = Press(Loaded, Key.Down, Key.Down, Key.Down);

        onTerminal.Selected.ShouldBe(Interactive);
        SessionsView.Reduce(onTerminal, new KeyPressed(Key.Of('f'))).Effects.ShouldBe([new OpenFolderInEditor(Interactive.Session.WorkingDirectory)]);
        onStopped.Selected.ShouldBe(Stopped);
        SessionsView.Reduce(onStopped, new KeyPressed(Key.Of('f'))).Effects.ShouldBe([new OpenFolderInEditor(Stopped.Session.WorkingDirectory)]);
    }

    [Fact]
    public void FindTheSelectedSessionsSolutionByItsFolderAndRepository()
    {
        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('s')));

        update.Effects.ShouldBe([new FindSolution(Waiting.Session.WorkingDirectory, "Northwind.Shipping")]);
    }

    [Theory]
    [InlineData('c')]
    [InlineData('f')]
    [InlineData('s')]
    public void SayNoSessionIsSelectedWhenAFolderKeyIsPressedOnAnEmptyTable(char key)
    {
        ViewUpdate<SessionsViewState> update = SessionsView.Reduce(SessionsViewState.Empty, new KeyPressed(Key.Of(key)));

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
        new OpenLaunchWizard().ShouldBeAssignableTo<IDialogEffect>();
        new StopSession(WaitingJob).ShouldNotBeAssignableTo<ILeavesScreen>();
        new StopSession(WaitingJob).ShouldNotBeAssignableTo<IDialogEffect>();
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

    private static Session.Background Job(string name, DisplayState state, string repository, string jobId)
    {
        return new Session.Background(SessionId.From(Guid.NewGuid().ToString()), JobId.From(jobId), Listed.As(name), FolderPath.From($@"C:\repos\{repository}"), Fixtures.Noon, state, new JobProcess.None());
    }

    private static Session.Interactive Terminal(string name, DisplayState state, string repository)
    {
        return new Session.Interactive(SessionId.From(Guid.NewGuid().ToString()), Listed.As(name), FolderPath.From($@"C:\repos\{repository}"), Fixtures.Noon, state, ProcessId: 4242);
    }

    private static SessionsViewState Press(SessionsViewState state, params Key[] keys)
    {
        return keys.Aggregate(state, (current, key) => SessionsView.Reduce(current, new KeyPressed(key)).State);
    }
}