using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Core.Tests.Application.Views;

/// <summary>How a status line message is set out in rows under the settings row: broken at spaces, at most three rows.</summary>
public sealed class StatusLayoutShould
{
    [Fact]
    public void TakeNoRowsForNoWords()
    {
        StatusLayout.Lines(string.Empty, 20).ShouldBeEmpty();
    }

    [Fact]
    public void TakeOneRowForWordsThatFit()
    {
        StatusLayout.Lines("Settings saved.", 20).ShouldBe(["Settings saved."]);
        StatusLayout.Lines("abc def", 7).ShouldBe(["abc def"]);
    }

    [Fact]
    public void BreakLongerWordsAtSpaces()
    {
        StatusLayout.Lines("one two three four", 9).ShouldBe(["one two", "three", "four"]);
    }

    [Fact]
    public void BreakAWordWiderThanARowAtItsEnd()
    {
        StatusLayout.Lines(@"Copied C:\Users\me\source", 10).ShouldBe(["Copied", @"C:\Users\m", @"e\source"]);
    }

    [Fact]
    public void EndTheThirdRowWithAnEllipsisWhenMoreWasLeft()
    {
        StatusLayout.Lines("aa bb cc dd", 3).ShouldBe(["aa", "bb", "cc…"]);
        StatusLayout.Lines("aa bb cc dd", 2).ShouldBe(["aa", "bb", "c…"]);
    }

    [Fact]
    public void KeepThreeRowsWhenNothingWasLeft()
    {
        StatusLayout.Lines("aa bb cc", 2).ShouldBe(["aa", "bb", "cc"]);
    }

    [Fact]
    public void ClearASuccessAfterTenSeconds()
    {
        StatusLayout.ClearsAfter(StatusTone.Success).ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData(StatusTone.Notice)]
    [InlineData(StatusTone.Failure)]
    [InlineData(StatusTone.Working)]
    [InlineData(StatusTone.Hint)]
    public void KeepEveryOtherToneUntilItIsCleared(StatusTone tone)
    {
        StatusLayout.ClearsAfter(tone).ShouldBeNull();
    }

    [Fact]
    public void OfferTheCloseMarkForAMessage()
    {
        StatusLayout.Closes(new StatusMessage.NoSessionSelected(), running: false).ShouldBeTrue();
        StatusLayout.Closes(new StatusMessage.Failed("no claude"), running: false).ShouldBeTrue();
    }

    [Fact]
    public void OfferNoCloseMarkWhileRunningOrForAStandingDescription()
    {
        StatusLayout.Closes(new StatusMessage.Working(new Quit()), running: true).ShouldBeFalse();
        StatusLayout.Closes(new StatusMessage.NoSessionSelected(), running: true).ShouldBeFalse();
        StatusLayout.Closes(StatusMessage.None, running: false).ShouldBeFalse();
        StatusLayout.Closes(new StatusMessage.SettingHint(new ConfigRow.CheckForUpdates(On: true)), running: false).ShouldBeFalse();
    }
}