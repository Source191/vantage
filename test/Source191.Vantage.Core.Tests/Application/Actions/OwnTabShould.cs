using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class OwnTabShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly TabColour Green = TabPalette.Default[4];

    private readonly FakeProcessRegistry registry = new();

    private readonly InMemoryState Store = new();

    private OwnTab Tab => new(Store, registry, new FakeTimeProvider(Noon));

    [Fact]
    public async Task ColourItselfGreenWhenGreenIsFree()
    {
        registry.Running(100, Noon.AddSeconds(-3));

        OwnTabClaim claim = await Tab.ClaimAsync(processId: 100);

        claim.ShouldBe(new OwnTabClaim.Claimed(Green));
        TabRegistration own = (await Store.ReadTabsAsync()).Single();
        own.Owner.ShouldBe(TabOwner.Vantage);
        own.Colour.ShouldBe(Green);
        own.Claim.ShouldBe(new TabClaim.Claimed(100, Noon.AddSeconds(-3)));
    }

    [Fact]
    public async Task TakeTheFirstFreeColourWhenGreenIsUsed()
    {
        registry.Running(100, Noon);
        await Store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), Green, Noon));

        OwnTabClaim claim = await Tab.ClaimAsync(processId: 100);

        claim.ShouldBe(new OwnTabClaim.Claimed(TabPalette.Default[0]));
    }

    [Fact]
    public async Task ClaimNothingWhenItsProcessIsUnknown()
    {
        OwnTabClaim claim = await Tab.ClaimAsync(processId: 100);

        claim.ShouldBe(new OwnTabClaim.Unregistered());
        (await Store.ReadTabsAsync()).ShouldBeEmpty();
    }
}