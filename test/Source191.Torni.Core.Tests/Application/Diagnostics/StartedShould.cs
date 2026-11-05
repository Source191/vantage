using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Domain;

namespace Source191.Torni.Core.Tests.Application.Diagnostics;

public sealed class StartedShould
{
    private static readonly ToolVersion Tool = new("0.20.0");
    private static readonly Version Windows = new(10, 0, 26100, 0);
    private static readonly Version Runtime = new(10, 0, 1);

    [Fact]
    public void CarryTheVersionClaudeCodePrinted()
    {
        Started start = Started.From(Tool, new ClaudeVersionReport.Printed("2.1.310"), Windows, Runtime, 120, 36);

        start.ShouldBe(new Started(Tool, new Version(2, 1, 310), Windows, Runtime, 120, 36));
    }

    public static TheoryData<ClaudeVersionReport> Unread => [new ClaudeVersionReport.NotRead(), new ClaudeVersionReport.Printed("unknown")];

    [Theory]
    [MemberData(nameof(Unread))]
    public void CarryNoVersionWhenNoneWasRead(ClaudeVersionReport report)
    {
        Started start = Started.From(Tool, report, Windows, Runtime, 120, 36);

        start.ShouldBe(new Started(Tool, null, Windows, Runtime, 120, 36));
    }
}