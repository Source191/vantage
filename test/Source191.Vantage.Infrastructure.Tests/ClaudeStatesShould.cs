using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>The listing's words (status busy, waiting, idle; state working, blocked, done, failed) as the six display states.</summary>
public sealed class ClaudeStatesShould
{
    [Fact]
    public void ShowWorkingForABusySession()
    {
        ClaudeStates.From(processId: 100, status: "busy", state: "working").ShouldBe(DisplayState.Working);
    }

    [Fact]
    public void ShowNeedsInputWhenWaitingOnAPermissionPrompt()
    {
        ClaudeStates.From(processId: 100, status: "waiting", state: "blocked").ShouldBe(DisplayState.NeedsInput);
    }

    [Fact]
    public void ShowNeedsInputForAnIdleSessionThatHasNotStarted()
    {
        ClaudeStates.From(processId: 100, status: "idle", state: "blocked").ShouldBe(DisplayState.NeedsInput);
    }

    [Fact]
    public void ShowCompletedForAFinishedSessionWithALiveProcess()
    {
        ClaudeStates.From(processId: 100, status: "idle", state: "done").ShouldBe(DisplayState.Completed);
    }

    [Fact]
    public void ShowIdleForAnInteractiveSessionWithNoState()
    {
        ClaudeStates.From(processId: 100, status: "idle", state: null).ShouldBe(DisplayState.Idle);
    }

    [Fact]
    public void ShowIdleForABackgroundSessionBetweenTurns()
    {
        ClaudeStates.From(processId: 100, status: "idle", state: "working").ShouldBe(DisplayState.Idle);
    }

    [Fact]
    public void ShowFailedWhenTheListingSaysSo()
    {
        ClaudeStates.From(processId: null, status: null, state: "failed").ShouldBe(DisplayState.Failed);
    }

    [Fact]
    public void ShowStoppedWhenThereIsNoProcess()
    {
        ClaudeStates.From(processId: null, status: null, state: "done").ShouldBe(DisplayState.Stopped);
    }

    [Fact]
    public void ShowTheRawValuesForAnUnknownCombination()
    {
        DisplayState state = ClaudeStates.From(processId: 100, status: "paused", state: "dreaming");

        state.Kind.ShouldBe(DisplayStateKind.Unknown);
        state.Label.ShouldBe("paused/dreaming");
    }
}