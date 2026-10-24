using Source191.Vantage.HeadlessTerminal;

namespace Source191.Vantage.UnitTests.HeadlessTerminal;

public sealed class TerminalQueriesShould
{
    [Fact]
    public void AnswerTheKittyKeyboardQueryAndTheVersionQueryWithTheConfiguredDelay()
    {
        TerminalQueries queries = new(xtversionDelayMs: 250);

        IReadOnlyList<TerminalReply> replies = queries.RepliesTo("\x1b[?u\x1b[>0q plain text");

        replies.ShouldBe([new TerminalReply("\x1bP>|Windows Terminal 1.25.2733.0\x1b\\", 250), new TerminalReply("\x1b[?0u", 0)]);
    }

    [Fact]
    public void AnswerEachOccurrenceAndNothingForPlainOutput()
    {
        TerminalQueries queries = new(xtversionDelayMs: 0);

        queries.RepliesTo("\x1b[6n\x1b[6n").Count.ShouldBe(2);
        queries.RepliesTo("sessions(all)[4]").ShouldBeEmpty();
    }
}