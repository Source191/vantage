using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class SessionScreenShould
{
    private static readonly string[] Output =
    [
        "● Read(src/Northwind.Orders/Processor.cs)",
        "",
        "● Update(src/Northwind.Orders/Processor.cs)",
        "Do you want to make this edit to Processor.cs?",
        "❯ 1. Yes",
        "  2. No, and tell Claude what to do differently",
    ];

    private static readonly string[] Chrome = ["────────────────────────────────", "❯", "────────────────────────────────", "  opus  effort:high  |  ctx:12%", "  ⏵⏵ auto mode on"];

    [Theory]
    [InlineData(48)]
    [InlineData(80)]
    [InlineData(201)]
    public void RebuildTheRowsAtTheSessionsOwnWidth(int width)
    {
        string logs = Screen(width, [.. Output, .. Chrome]);

        SessionScreen.Bottom(logs, rows: 4).ShouldBe(Output[2..]);
    }

    [Fact]
    public void KeepAsManyRowsAsThereAre()
    {
        SessionScreen.Bottom(Screen(80, [.. Output, .. Chrome]), rows: 20).ShouldBe(Output);
    }

    [Fact]
    public void DropBlankRowsAtTheEnd()
    {
        SessionScreen.Bottom(Screen(80, [.. Output, "", "", .. Chrome]), rows: 2).ShouldBe(Output[4..]);
    }

    [Fact]
    public void KeepTheRowsWhenThereIsNoInputBox()
    {
        SessionScreen.Bottom(Screen(80, Output), rows: 2).ShouldBe(Output[4..]);
    }

    [Fact]
    public void ReadThroughTheColourCodes()
    {
        string logs = "\u001b[H" + string.Concat(Output.Select(row => "\u001b[38;2;248;248;242m" + row.PadRight(60) + "\u001b[39m\u001b[K")) + "\u001b[49;3H";

        SessionScreen.Bottom(logs, rows: 1).ShouldBe([Output[^1]]);
    }

    [Fact]
    public void AnswerNothingWhenNoWidthFits()
    {
        SessionScreen.Bottom("Northwind", rows: 3).ShouldBeEmpty();
        SessionScreen.Bottom(string.Empty, rows: 3).ShouldBeEmpty();
    }

    private static string Screen(int width, string[] rows) => string.Concat(rows.Select(row => row.PadRight(width)));
}
