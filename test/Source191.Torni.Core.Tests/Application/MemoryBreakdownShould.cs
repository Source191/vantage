using Source191.Torni.Application;
using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application;

public sealed class MemoryBreakdownShould
{
    /// <summary>The session-listing spec's fix-123: Claude Code, an MCP server, a build shell and a second server, each a short chain.</summary>
    private static readonly FakeProcessRegistry Processes = new FakeProcessRegistry()
        .InTree(100, "claude", 1).Using(100, Fixtures.Noon, MemorySize.FromMegabytes(296))
        .InTree(200, "cmd", 100).Using(200, Fixtures.Noon.AddSeconds(1), MemorySize.FromMegabytes(8), "@azure-devops/mcp")
        .InTree(201, "node", 200).Using(201, Fixtures.Noon.AddSeconds(2), MemorySize.FromMegabytes(92))
        .InTree(300, "uvx", 100).Using(300, Fixtures.Noon.AddSeconds(1), MemorySize.FromMegabytes(30), "mcp-grafana")
        .InTree(400, "cmd", 100).Using(400, Fixtures.Noon.AddSeconds(3), MemorySize.FromMegabytes(8), "pwsh")
        .InTree(401, "pwsh", 400).Using(401, Fixtures.Noon.AddSeconds(4), MemorySize.FromMegabytes(70));

    [Fact]
    public void ListClaudeCodeThenEachProgramLargestFirstThenTheTotal()
    {
        ProcessTree tree = ProcessTree.Under(100, Processes.Snapshot(), Processes.UsageOf).ShouldBeOfType<ProcessTreeLookup.Found>().Tree;

        MemoryBreakdown breakdown = MemoryBreakdown.Of(Listed.As("fix-123"), tree);

        breakdown.SessionName.ShouldBe(Listed.As("fix-123"));
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
            .InTree(100, "claude", 1).Using(100, Fixtures.Noon, MemorySize.FromMegabytes(296))
            .InTree(200, "gk-alpha", 100);
        ProcessTree tree = ProcessTree.Under(100, processes.Snapshot(), processes.UsageOf).ShouldBeOfType<ProcessTreeLookup.Found>().Tree;

        MemoryBreakdown.Of(Listed.As("fix-123"), tree).Programs.ShouldBe([new MemoryShare("gk-alpha", MemorySize.Zero)]);
    }
}