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

    [Theory]
    [InlineData(48)]
    [InlineData(80)]
    [InlineData(201)]
    public void RebuildTheRowsAtTheSessionsOwnWidth(int width)
    {
        string logs = Screen(width, [.. Output, .. Chrome(width)]);

        SessionScreen.Bottom(logs, rows: 4).ShouldBe(Output[2..]);
    }

    [Fact]
    public void KeepAsManyRowsAsThereAre()
    {
        SessionScreen.Bottom(Screen(80, [.. Output, .. Chrome(80)]), rows: 20).ShouldBe(Output);
    }

    [Fact]
    public void DropBlankRowsAtTheEnd()
    {
        SessionScreen.Bottom(Screen(80, [.. Output, "", "", .. Chrome(80)]), rows: 2).ShouldBe(Output[4..]);
    }

    [Fact]
    public void FindTheWidthPastAnEarlierShorterRule()
    {
        string logs = Screen(80, ["  " + new string('─', 60), .. Output, .. Chrome(80)]);

        SessionScreen.Bottom(logs, rows: 1).ShouldBe([Output[^1]]);
    }

    [Fact]
    public void ReadThroughTheColourCodes()
    {
        string logs = "\u001b[H" + string.Concat(((string[])[.. Output, .. Chrome(60)]).Select(row => "\u001b[38;2;248;248;242m" + row.PadRight(60) + "\u001b[39m\u001b[K")) + "\u001b[49;3H";

        SessionScreen.Bottom(logs, rows: 1).ShouldBe([Output[^1]]);
    }

    /// <summary>A new session's stream is a screen drawn line by line: a clear, rows ended by CR LF, gaps by cursor-forward and erase-characters.</summary>
    [Fact]
    public void ReplayAScreenDrawnLineByLine()
    {
        string[] chrome = Chrome(60);
        string logs = "\u001b[2J\u001b[H\u001b[K"
            + "● Read(src/Northwind.Orders/Processor.cs)\u001b[K\r\n"
            + "\u001b[K\r\n"
            + "Do you want\u001b[1Cto make this edit?\u001b[K\r\n"
            + "\u001b[5X\u001b[2C1. Yes\u001b[K\r\n"
            + string.Concat(chrome.Select(row => row + "\u001b[K\r\n"))
            + "\u001b[47;3H";

        SessionScreen.Bottom(logs, rows: 3).ShouldBe(["", "Do you want to make this edit?", "  1. Yes"]);
    }

    [Fact]
    public void KeepOneBlankRowWhereTheScreenHasSeveral()
    {
        string logs = Screen(80, [Output[0], "", "", "", Output[3], .. Chrome(80)]);

        SessionScreen.Bottom(logs, rows: 5).ShouldBe([Output[0], "", Output[3]]);
    }

    [Fact]
    public void AnswerNothingWithoutTheInputBoxRule()
    {
        SessionScreen.Bottom(Screen(80, Output), rows: 2).ShouldBeEmpty();
        SessionScreen.Bottom("Northwind", rows: 3).ShouldBeEmpty();
        SessionScreen.Bottom(string.Empty, rows: 3).ShouldBeEmpty();
    }

    private static string[] Chrome(int width) => [new string('─', width - 10) + " fix-123 ─", "❯", new string('─', width), "  opus  effort:high  |  ctx:12%", "  ⏵⏵ auto mode on"];

    private static string Screen(int width, string[] rows) => string.Concat(rows.Select(row => row.PadRight(width)));
}