using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class StartedShould
{
    private static readonly ToolVersion Vantage = new("0.20.0");
    private static readonly Version Windows = new(10, 0, 26100, 0);
    private static readonly Version Runtime = new(10, 0, 1);

    [Fact]
    public void CarryTheVersionClaudeCodePrinted()
    {
        Started start = Started.From(Vantage, new ClaudeVersionReport.Printed("2.1.310"), Windows, Runtime, 120, 36);

        start.ShouldBe(new Started(Vantage, new Version(2, 1, 310), Windows, Runtime, 120, 36));
    }

    public static TheoryData<ClaudeVersionReport> Unread => [new ClaudeVersionReport.NotRead(), new ClaudeVersionReport.Printed("unknown")];

    [Theory]
    [MemberData(nameof(Unread))]
    public void CarryNoVersionWhenNoneWasRead(ClaudeVersionReport report)
    {
        Started start = Started.From(Vantage, report, Windows, Runtime, 120, 36);

        start.ShouldBe(new Started(Vantage, null, Windows, Runtime, 120, 36));
    }
}
