using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.UnitTests.Application.Views;

public sealed class SessionsViewShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private static readonly SessionRow Working = Row("remove-sonar", DisplayState.Working, "Northwind.Templates", jobId: "aaaa1111");
    private static readonly SessionRow Waiting = Row("change-feed", DisplayState.NeedsInput, "Northwind.Shipping", jobId: "bbbb2222");
    private static readonly SessionRow Stopped = Row("old-spike", DisplayState.Stopped, "Source191.Vantage", jobId: "cccc3333");
    private static readonly SessionRow Interactive = Row("repos-93", DisplayState.Idle, "repos", jobId: null, kind: SessionKind.Interactive);

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
        failed.Banner.ShouldNotBeNull();
        failed.Banner.ShouldContain("exited with code 1");
    }

    [Fact]
    public void ClearTheBannerWhenTheNextLoadSucceeds()
    {
        SessionsViewState failed = Reduce(Loaded, new LoadFailed("boom"));

        Reduce(failed, new RowsLoaded([Working])).Banner.ShouldBeNull();
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
        typing.Title.ShouldBe("sessions(ol)[1]");
    }

    [Fact]
    public void CommitTheFilterWithEnterAndClearItWithEscape()
    {
        SessionsViewState committed = Press(Loaded, Key.Slash, Key.Of('n'), Key.Of('o'), Key.Of('r'), Key.Enter);
        committed.FilterEditing.ShouldBeFalse();
        committed.VisibleRows.Count.ShouldBe(2);

        SessionsViewState cleared = Press(committed, Key.Escape);
        cleared.VisibleRows.Count.ShouldBe(4);
        cleared.Title.ShouldBe("sessions(all)[4]");
    }

    [Fact]
    public void CountSessionsNeedingInputForTheHeader()
    {
        Loaded.NeedsInputCount.ShouldBe(1);
    }

    [Fact]
    public void SelectASessionByIdWhenAskedTo()
    {
        Reduce(Loaded, new SelectSession(Stopped.Session.SessionId)).Selected.ShouldBe(Stopped);
        Reduce(Loaded, new SelectSession("no-such-session")).Selected.ShouldBe(Waiting);
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
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('x'))).Effects.ShouldBe([new StopSession(Waiting)]);
    }

    [Fact]
    public void AttachToTheSelectedBackgroundSessionOnEnter()
    {
        ViewUpdate update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Enter));

        update.Effects.ShouldBe([new AttachToSession("bbbb2222")]);
    }

    [Fact]
    public void RefuseActionsOnAnInteractiveRow()
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);
        onInteractive.Selected.ShouldBe(Interactive);

        ViewUpdate update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Enter));

        update.Effects.ShouldBeEmpty();
        update.State.Footer.ShouldBe("repos-93 runs in its own terminal. Press b to move it under Vantage (its tab becomes a plain shell), or type /bg there.");
    }

    [Fact]
    public void AdoptAnIdleInteractiveSessionOnB()
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);

        ViewUpdate update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Of('b')));

        update.Effects.ShouldBe([new AdoptSession(Interactive)]);
    }

    [Fact]
    public void RefuseToAdoptAnInteractiveSessionWhileItIsWorking()
    {
        SessionRow busy = Row("busy-terminal", DisplayState.Working, "repos", jobId: null, kind: SessionKind.Interactive);
        SessionsViewState state = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([busy])).State;

        ViewUpdate update = SessionsView.Reduce(state, new KeyPressed(Key.Of('b')));

        update.Effects.ShouldBeEmpty();
        update.State.Footer.ShouldBe("busy-terminal is working; wait until it is idle or needs input, so nothing in flight is lost.");
    }

    [Fact]
    public void ExplainThatABackgroundSessionNeedsNoAdoption()
    {
        ViewUpdate update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('b')));

        update.Effects.ShouldBeEmpty();
        update.State.Footer.ShouldBe("change-feed already runs in the background under Vantage.");
    }

    [Fact]
    public void RefuseToOpenAStoppedSessionThatWasNeverSentAMessageBecauseNothingCanResume()
    {
        SessionRow stub = Row("repos-f6", DisplayState.Failed, "repos", "5a4a7350") with { HasTranscript = false };
        SessionsViewState withStub = Reduce(Loaded, new RowsLoaded([stub]));

        foreach (Key key in new[] { Key.Enter, Key.Of('o'), Key.Of('r') })
        {
            ViewUpdate update = SessionsView.Reduce(withStub, new KeyPressed(key));

            update.Effects.ShouldBeEmpty();
            update.State.Footer.ShouldBe("repos-f6 has nothing to resume: it was never sent a message. d deletes it.");
        }

        SessionsView.Reduce(withStub, new KeyPressed(Key.Of('d'))).Effects.ShouldBe([new DeleteSession(stub)]);
    }

    [Fact]
    public void OfferToCloseATerminalSessionThatHasNoMessagesAndCloseItOnX()
    {
        SessionRow untouched = Row("repos-f6", DisplayState.Idle, "repos", jobId: null, kind: SessionKind.Interactive) with { HasTranscript = false };
        SessionsViewState onUntouched = Reduce(SessionsViewState.Empty, new RowsLoaded([untouched]));

        ViewUpdate enter = SessionsView.Reduce(onUntouched, new KeyPressed(Key.Enter));
        ViewUpdate closed = SessionsView.Reduce(onUntouched, new KeyPressed(Key.Of('d')));

        enter.Effects.ShouldBeEmpty();
        enter.State.Footer.ShouldBe("repos-f6 runs in its own terminal and has no messages yet. d closes its tab, C closes every such tab, or send it something there and press b.");
        closed.Effects.Single().ShouldBeOfType<CloseEmptyTerminals>().Rows.ShouldBe([untouched]);
    }

    [Fact]
    public void CloseEveryTerminalSessionWithNoMessagesOnCAndSayWhenThereAreNone()
    {
        SessionRow first = Row("repos-f6", DisplayState.Idle, "repos", jobId: null, kind: SessionKind.Interactive) with { HasTranscript = false };
        SessionRow second = Row("repos-f7", DisplayState.Idle, "repos", jobId: null, kind: SessionKind.Interactive) with { HasTranscript = false };
        SessionRow withMessages = Row("repos-31", DisplayState.Idle, "repos", jobId: null, kind: SessionKind.Interactive);
        SessionsViewState mixed = Reduce(SessionsViewState.Empty, new RowsLoaded([withMessages, first, Waiting, second]));

        ViewUpdate update = SessionsView.Reduce(mixed, new KeyPressed(Key.Of('C')));
        ViewUpdate none = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('C')));

        update.Effects.Single().ShouldBeOfType<CloseEmptyTerminals>().Rows.ShouldBe([first, second], ignoreOrder: true);
        none.Effects.ShouldBeEmpty();
        none.State.Footer.ShouldBe("No terminal sessions without messages.");
    }

    [Fact]
    public void KeepRefusingToDeleteATerminalSessionThatHasAConversation()
    {
        SessionsViewState onInteractive = Press(Press(Loaded, Key.Down), Key.Down);

        ViewUpdate update = SessionsView.Reduce(onInteractive, new KeyPressed(Key.Of('d')));

        update.Effects.ShouldBeEmpty();
        update.State.Footer.ShouldContain("Press b to move it under Vantage");
    }

    [Fact]
    public void RefuseEnterWhenALiveTabAlreadyShowsTheSessionAndNameTheWayThere()
    {
        SessionsViewState withTab = Reduce(Loaded, new RowsLoaded([Waiting with { HasLiveTab = true }]));

        ViewUpdate update = SessionsView.Reduce(withTab, new KeyPressed(Key.Enter));

        update.Effects.ShouldBeEmpty();
        update.State.Footer.ShouldBe("change-feed is open in another tab; press o to switch to it.");
    }

    [Fact]
    public void LetOSwitchToTheLiveTabThatShowsTheSession()
    {
        SessionRow shown = Waiting with { HasLiveTab = true };
        SessionsViewState withTab = Reduce(Loaded, new RowsLoaded([shown]));

        SessionsView.Reduce(withTab, new KeyPressed(Key.Of('o'))).Effects.ShouldBe([new OpenTabForSession(shown)]);
    }

    [Fact]
    public void AskForANewNameOnRAndRenameOnceOneIsGiven()
    {
        SessionsView.Reduce(Loaded, new KeyPressed(Key.Of('r'))).Effects.ShouldBe([new AskForNewName(Waiting)]);

        ViewUpdate renamed = SessionsView.Reduce(Loaded, new RenameConfirmed(Waiting, "fix the tests"));
        ViewUpdate blank = SessionsView.Reduce(Loaded, new RenameConfirmed(Waiting, "   "));
        ViewUpdate cancelled = SessionsView.Reduce(Loaded, new RenameConfirmed(Waiting, null));

        renamed.Effects.ShouldBe([new RenameSession(Waiting, SessionName.Parse("fix the tests"))]);
        renamed.State.Footer.ShouldBe("Renaming change-feed to fix the tests...");
        blank.Effects.ShouldBeEmpty();
        blank.State.Footer.ShouldBe("A session name is required.");
        cancelled.Effects.ShouldBeEmpty();
        cancelled.State.Footer.ShouldBe("Rename cancelled.");
    }

    [Fact]
    public void RefuseToRenameASessionWhileItIsWorkingBecauseRenamingRestartsIt()
    {
        SessionsViewState onWorking = Press(Loaded, Key.Down);

        ViewUpdate update = SessionsView.Reduce(onWorking, new KeyPressed(Key.Of('r')));

        update.Effects.ShouldBeEmpty();
        update.State.Footer.ShouldBe("remove-sonar is working; wait until it is idle or needs input, because renaming restarts it.");
    }

    [Theory]
    [InlineData('o', typeof(OpenTabForSession))]
    [InlineData('x', typeof(StopSession))]
    [InlineData('d', typeof(DeleteSession))]
    [InlineData('r', typeof(AskForNewName))]
    [InlineData('n', typeof(OpenLaunchWizard))]
    [InlineData('R', typeof(ResumeStoppedSessions))]
    [InlineData('a', typeof(OpenAgentView))]
    [InlineData('w', typeof(SwitchToWorktrees))]
    [InlineData('q', typeof(Quit))]
    [InlineData('?', typeof(ShowHelp))]
    public void EmitTheEffectForTheKey(char key, Type effect)
    {
        ViewUpdate update = SessionsView.Reduce(Loaded, new KeyPressed(Key.Of(key)));

        update.Effects.Single().ShouldBeOfType(effect);
    }

    [Fact]
    public void TreatLettersAsFilterTextWhileEditingTheFilter()
    {
        SessionsViewState typing = Press(Loaded, Key.Slash);

        ViewUpdate update = SessionsView.Reduce(typing, new KeyPressed(Key.Of('q')));

        update.Effects.ShouldBeEmpty();
        update.State.Filter.Text.ShouldBe("q");
    }

    private static SessionsViewState Press(SessionsViewState state, params Key[] keys)
    {
        return keys.Aggregate(state, (current, key) => SessionsView.Reduce(current, new KeyPressed(key)).State);
    }

    private static SessionsViewState Reduce(SessionsViewState state, ViewInput input)
    {
        return SessionsView.Reduce(state, input).State;
    }

    private static SessionRow Row(string name, DisplayState state, string repository, string? jobId, SessionKind kind = SessionKind.Background)
    {
        Session session = new(
            SessionId: Guid.NewGuid().ToString(),
            JobId: jobId,
            Name: name,
            WorkingDirectory: $@"C:\repos\{repository}",
            Kind: kind,
            StartedAt: Noon,
            State: state,
            ProcessId: kind == SessionKind.Interactive ? 4242 : null);

        return new SessionRow(session, repository, "main", IsWorktree: false, Description: null, HasLiveTab: false, HasTranscript: true);
    }
}