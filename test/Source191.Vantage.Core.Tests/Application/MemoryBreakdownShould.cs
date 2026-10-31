using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class MemoryBreakdownShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    /// <summary>The session-listing spec's fix-123: Claude Code, an MCP server, a build shell and a second server, each a short chain.</summary>
    private static readonly FakeProcessRegistry Processes = new FakeProcessRegistry()
        .InTree(100, "claude", 1).Using(100, Noon, MemorySize.FromMegabytes(296))
        .InTree(200, "cmd", 100).Using(200, Noon.AddSeconds(1), MemorySize.FromMegabytes(8), "cmd /c npx -y @azure-devops/mcp m-kopa")
        .InTree(201, "node", 200).Using(201, Noon.AddSeconds(2), MemorySize.FromMegabytes(92))
        .InTree(300, "uvx", 100).Using(300, Noon.AddSeconds(1), MemorySize.FromMegabytes(30), "uvx mcp-grafana@latest")
        .InTree(400, "cmd", 100).Using(400, Noon.AddSeconds(3), MemorySize.FromMegabytes(8), """cmd.exe /d /s /c ""chcp.com" 65001 >nul & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File build.ps1" """)
        .InTree(401, "pwsh", 400).Using(401, Noon.AddSeconds(4), MemorySize.FromMegabytes(70));

    [Fact]
    public void ListClaudeCodeThenEachProgramLargestFirstThenTheTotal()
    {
        ProcessTree tree = ProcessTree.Under(100, Processes.Snapshot(), Processes.UsageOf).ShouldBeOfType<ProcessTreeLookup.Found>().Tree;

        MemoryBreakdown breakdown = MemoryBreakdown.Of("fix-123", tree);

        breakdown.SessionName.ShouldBe("fix-123");
        breakdown.ClaudeCode.ShouldBe(MemorySize.FromMegabytes(296));
        breakdown.Programs.ShouldBe(
        [
            new MemoryShare("@azure-devops/mcp", MemorySize.FromMegabytes(100)),
            new MemoryShare("pwsh", MemorySize.FromMegabytes(78)),
            new MemoryShare("mcp-grafana", MemorySize.FromMegabytes(30)),
        ]);
        breakdown.Total.ShouldBe(MemorySize.FromMegabytes(504));
        breakdown.ProcessCount.ShouldBe(6);
    }

    [Fact]
    public void LabelAProgramThatRefusedToBeMeasuredByItsNameAlone()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry()
            .InTree(100, "claude", 1).Using(100, Noon, MemorySize.FromMegabytes(296))
            .InTree(200, "gk-alpha", 100);
        ProcessTree tree = ProcessTree.Under(100, processes.Snapshot(), processes.UsageOf).ShouldBeOfType<ProcessTreeLookup.Found>().Tree;

        MemoryBreakdown.Of("fix-123", tree).Programs.ShouldBe([new MemoryShare("gk-alpha", MemorySize.Zero)]);
    }
}