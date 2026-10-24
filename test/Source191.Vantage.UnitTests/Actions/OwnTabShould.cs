using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Actions;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Domain;
using Source191.Vantage.UnitTests.TestDoubles;

namespace Source191.Vantage.UnitTests.Actions;

public sealed class OwnTabShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly TabColour Green = TabPalette.Default[4];

    private readonly string stateFolder = Path.Combine(Path.GetTempPath(), "vantage-owntab-" + Guid.NewGuid().ToString("N"));
    private readonly FakeProcessRegistry registry = new();

    private StateStore Store => new(stateFolder, TimeProvider.System);

    private OwnTab Tab => new(Store, registry, new FakeTimeProvider(Noon));

    [Fact]
    public async Task ColourItselfGreenWhenGreenIsFree()
    {
        registry.Running(100, Noon.AddSeconds(-3));

        TabColour? colour = await Tab.ClaimAsync(processId: 100);

        colour.ShouldBe(Green);
        TabRegistration own = (await Store.ReadTabsAsync()).Single();
        own.SessionId.ShouldBe("vantage");
        own.Colour.ShouldBe(Green);
        own.ProcessId.ShouldBe(100);
        own.ProcessStartedAt.ShouldBe(Noon.AddSeconds(-3));
    }

    [Fact]
    public async Task TakeTheFirstFreeColourWhenGreenIsUsed()
    {
        registry.Running(100, Noon);
        await Store.WriteTabAsync(TabRegistration.Pending("sess-1", Green, Noon));

        TabColour? colour = await Tab.ClaimAsync(processId: 100);

        colour.ShouldBe(TabPalette.Default[0]);
    }

    [Fact]
    public async Task ClaimNothingWhenItsProcessIsUnknown()
    {
        TabColour? colour = await Tab.ClaimAsync(processId: 100);

        colour.ShouldBeNull();
        (await Store.ReadTabsAsync()).ShouldBeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(stateFolder))
        {
            Directory.Delete(stateFolder, recursive: true);
        }
    }
}