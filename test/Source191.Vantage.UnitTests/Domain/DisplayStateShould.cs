using Source191.Vantage.Domain;

namespace Source191.Vantage.UnitTests.Domain;

public sealed class DisplayStateShould
{
    [Fact]
    public void ShowWorkingForABusySession()
    {
        SessionListingEntry entry = new(ProcessId: 100, Status: "busy", WaitingFor: null, State: "working");

        DisplayState.From(entry).ShouldBe(DisplayState.Working);
    }

    [Fact]
    public void ShowNeedsInputWhenWaitingOnAPermissionPrompt()
    {
        SessionListingEntry entry = new(ProcessId: 100, Status: "waiting", WaitingFor: "permission prompt", State: "blocked");

        DisplayState.From(entry).ShouldBe(DisplayState.NeedsInput);
    }

    [Fact]
    public void ShowNeedsInputForAnIdleSessionThatHasNotStarted()
    {
        SessionListingEntry entry = new(ProcessId: 100, Status: "idle", WaitingFor: null, State: "blocked");

        DisplayState.From(entry).ShouldBe(DisplayState.NeedsInput);
    }

    [Fact]
    public void ShowCompletedForAFinishedSessionWithALiveProcess()
    {
        SessionListingEntry entry = new(ProcessId: 100, Status: "idle", WaitingFor: null, State: "done");

        DisplayState.From(entry).ShouldBe(DisplayState.Completed);
    }

    [Fact]
    public void ShowIdleForAnInteractiveSessionWithNoState()
    {
        SessionListingEntry entry = new(ProcessId: 100, Status: "idle", WaitingFor: null, State: null);

        DisplayState.From(entry).ShouldBe(DisplayState.Idle);
    }

    [Fact]
    public void ShowIdleForABackgroundSessionBetweenTurns()
    {
        SessionListingEntry entry = new(ProcessId: 100, Status: "idle", WaitingFor: null, State: "working");

        DisplayState.From(entry).ShouldBe(DisplayState.Idle);
    }

    [Fact]
    public void ShowFailedWhenTheListingSaysSo()
    {
        SessionListingEntry entry = new(ProcessId: null, Status: null, WaitingFor: null, State: "failed");

        DisplayState.From(entry).ShouldBe(DisplayState.Failed);
    }

    [Fact]
    public void ShowStoppedWhenThereIsNoProcess()
    {
        SessionListingEntry entry = new(ProcessId: null, Status: null, WaitingFor: null, State: "done");

        DisplayState.From(entry).ShouldBe(DisplayState.Stopped);
    }

    [Fact]
    public void LabelEachStateInPlainLowerCaseWordsForTheStateColumn()
    {
        DisplayState.Working.Label.ShouldBe("working");
        DisplayState.NeedsInput.Label.ShouldBe("needs input");
        DisplayState.Idle.Label.ShouldBe("idle");
        DisplayState.Completed.Label.ShouldBe("completed");
        DisplayState.Failed.Label.ShouldBe("failed");
        DisplayState.Stopped.Label.ShouldBe("stopped");
    }

    [Fact]
    public void ShowTheRawValuesForAnUnknownCombination()
    {
        SessionListingEntry entry = new(ProcessId: 100, Status: "paused", WaitingFor: null, State: "dreaming");

        DisplayState state = DisplayState.From(entry);

        state.Kind.ShouldBe(DisplayStateKind.Unknown);
        state.Label.ShouldBe("paused/dreaming");
    }
}