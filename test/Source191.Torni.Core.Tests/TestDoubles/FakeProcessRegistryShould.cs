using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.TestDoubles;

public sealed class FakeProcessRegistryShould
{
    [Fact]
    public void EndTheTreeUnderATerminatedProcess()
    {
        FakeProcessRegistry registry = new FakeProcessRegistry()
            .InTree(100, "pwsh", 1, Fixtures.Noon)
            .InTree(101, "claude", 100, Fixtures.Noon)
            .InTree(102, "node", 101, Fixtures.Noon);

        ConfirmedProcess.TryConfirmClaim(100, Fixtures.Noon, Fixtures.Noon, out ConfirmedProcess? process).ShouldBeTrue();
        registry.Terminate(process);

        registry.Terminated.ShouldBe([100]);
        registry.StartTimeOf(101).ShouldBeNull();
        registry.StartTimeOf(102).ShouldBeNull();
        registry.Snapshot().Select(facts => facts.ProcessId).ShouldNotContain(102);
    }

    [Fact]
    public void ListARunningProcessNotInTheTree()
    {
        FakeProcessRegistry registry = new FakeProcessRegistry().Running(4242, Fixtures.Noon);

        ProcessFacts facts = registry.Snapshot().Single();

        (facts.ProcessId, facts.ParentId, facts.Name).ShouldBe((4242, 0, string.Empty));
    }
}