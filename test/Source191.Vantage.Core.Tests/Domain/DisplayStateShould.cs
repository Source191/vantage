using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

/// <summary>The six named display states and Unknown, and their labels; mapping the CLI's words to them is <c>ClaudeStatesShould</c>'s.</summary>
public sealed class DisplayStateShould
{
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
    public void KeepTheRawWordsOfAnUnknownCombinationAsItsLabel()
    {
        DisplayState state = DisplayState.Unknown("paused/dreaming");

        state.Kind.ShouldBe(DisplayStateKind.Unknown);
        state.Label.ShouldBe("paused/dreaming");
    }
}