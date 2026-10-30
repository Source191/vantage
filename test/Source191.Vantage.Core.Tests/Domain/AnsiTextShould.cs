using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class AnsiTextShould
{
    [Fact]
    public void RemoveColoursTitlesBlocksAndCursorMoves()
    {
        string raw = "\u001b[38;2;248;248;242mNorthwind\u001b[39m \u001b]0;fix-123\u0007Orders\u001bP1$r0m\u001b\\ ready\u001b[49;3H\u001b[K";

        AnsiText.Strip(raw).ShouldBe("Northwind Orders ready");
    }

    [Fact]
    public void LeavePlainTextAlone()
    {
        AnsiText.Strip("● Update(src/Processor.cs)").ShouldBe("● Update(src/Processor.cs)");
    }
}