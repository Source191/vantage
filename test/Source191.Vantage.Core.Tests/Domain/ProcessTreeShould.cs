using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class ProcessTreeShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SumTheRootAndEverythingUnderIt()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry()
            .InTree(100, "claude", 1).Using(100, Noon, MemorySize.FromMegabytes(296))
            .InTree(200, "cmd", 100).Using(200, Noon.AddSeconds(1), MemorySize.FromMegabytes(8))
            .InTree(201, "node", 200).Using(201, Noon.AddSeconds(2), MemorySize.FromMegabytes(92))
            .InTree(300, "uvx", 100).Using(300, Noon.AddSeconds(1), MemorySize.FromMegabytes(30))
            .InTree(999, "explorer", 1).Using(999, Noon.AddHours(-1), MemorySize.FromMegabytes(500));

        ProcessTree? tree = ProcessTree.Under(100, processes.Snapshot(), processes.UsageOf);

        tree.ShouldNotBeNull();
        tree.Process.Name.ShouldBe("claude");
        tree.Total.ShouldBe(MemorySize.FromMegabytes(426));
        tree.Count.ShouldBe(4);
        tree.Children.Select(child => child.Process.ProcessId).ShouldBe([200, 300]);
        tree.Children[0].Total.ShouldBe(MemorySize.FromMegabytes(100));
    }

    [Fact]
    public void BeNothingWhenTheRootCannotBeMeasured()
    {
        FakeProcessRegistry processes = new FakeProcessRegistry().InTree(100, "claude", 1);

        ProcessTree.Under(100, processes.Snapshot(), processes.UsageOf).ShouldBeNull();
        ProcessTree.Under(42, processes.Snapshot(), processes.UsageOf).ShouldBeNull();
    }
}