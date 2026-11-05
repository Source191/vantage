using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.ReadmeScene.Tests;

public sealed class SceneProcessRegistryShould
{
    private const int TabShell = 9_000;

    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeProcessRegistry real = new();
    private readonly SceneListing listing;
    private readonly SceneProcessRegistry registry;

    public SceneProcessRegistryShould()
    {
        Scene scene = SceneFile.Read(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scene.json")));
        listing = new SceneListing(scene, new SceneLayout(@"C:\ReadmeDemo\me"), clock);
        real.InTree(TabShell, "pwsh", 4, clock.GetUtcNow().AddMinutes(-1));
        registry = new SceneProcessRegistry(listing, real);
    }

    [Fact]
    public void AnswerForTheScenesProcesses()
    {
        int processId = listing.ProcessIds()[0];

        registry.StartTimeOf(processId).ShouldBe(clock.GetUtcNow().AddMinutes(-95));
        registry.UsageOf(processId).ShouldBe(new ProcessUsage(clock.GetUtcNow().AddMinutes(-95), MemorySize.FromMegabytes(240), "claude"));
        registry.Snapshot().ShouldContain(new ProcessFacts(processId, 0, "claude"));
    }

    [Fact]
    public void AnswerForRealProcessesFromTheRealRegistry()
    {
        registry.StartTimeOf(TabShell).ShouldBe(clock.GetUtcNow().AddMinutes(-1));
        registry.ParentOf(TabShell).ShouldBe(real.ParentOf(TabShell));
        registry.Snapshot().ShouldContain(new ProcessFacts(TabShell, 4, "pwsh"));
    }

    [Fact]
    public void NeverEndAScenesProcess()
    {
        int processId = listing.ProcessIds()[0];
        ConfirmedProcess.TryConfirm(processId, registry.StartTimeOf(processId), clock.GetUtcNow().AddMinutes(-95), out ConfirmedProcess? process).ShouldBeTrue();

        registry.Terminate(process);
        registry.EndCleanly(process);

        real.Terminated.ShouldBeEmpty();
        real.EndedCleanly.ShouldBeEmpty();
    }
}