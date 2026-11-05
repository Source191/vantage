using Source191.Torni.Domain;

namespace Source191.Torni.Core.Tests.Domain;

/// <summary>The six known display states, the starting state and Unknown, by kind; their words are <c>WordingOfScreensShould</c>'s and mapping the CLI's words to them is <c>ClaudeStatesShould</c>'s.</summary>
public sealed class DisplayStateShould
{
    [Fact]
    public void GiveEachKnownStateItsKind()
    {
        DisplayState.Working.Kind.ShouldBe(DisplayStateKind.Working);
        DisplayState.NeedsInput.Kind.ShouldBe(DisplayStateKind.NeedsInput);
        DisplayState.Idle.Kind.ShouldBe(DisplayStateKind.Idle);
        DisplayState.Completed.Kind.ShouldBe(DisplayStateKind.Completed);
        DisplayState.Failed.Kind.ShouldBe(DisplayStateKind.Failed);
        DisplayState.Stopped.Kind.ShouldBe(DisplayStateKind.Stopped);
    }

    [Fact]
    public void KeepOnlyTheRawWordsOfAnUnknownCombination()
    {
        DisplayState.Unknown state = new("paused/dreaming");

        state.Kind.ShouldBe(DisplayStateKind.Unknown);
        state.RawWords.ShouldBe("paused/dreaming");
    }

    [Fact]
    public void DistinguishStartingFromWorkingWhileSharingItsKind()
    {
        DisplayState starting = new DisplayState.Starting();

        starting.Kind.ShouldBe(DisplayStateKind.Working);
        starting.ShouldNotBe(DisplayState.Working);
    }
}