using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Application.Actions;
using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application.Actions;

public sealed class OwnTabShould
{
    private static readonly TabColour Green = TabPalette.Green;

    private readonly FakeProcessRegistry registry = new();

    private readonly InMemoryState store = new();

    private OwnTab Tab => new(store, registry, new FakeTimeProvider(Fixtures.Noon));

    [Fact]
    public async Task ColourItselfGreenWhenGreenIsFree()
    {
        registry.Running(100, Fixtures.Noon.AddSeconds(-3));

        OwnTabClaim claim = await Tab.ClaimAsync(processId: 100);

        claim.ShouldBe(new OwnTabClaim.Claimed(Green));
        TabRegistration own = (await store.ReadTabsAsync()).Single();
        own.Owner.ShouldBe(TabOwner.Console);
        own.Colour.ShouldBe(Green);
        own.Claim.ShouldBe(new TabClaim.Claimed(100, Fixtures.Noon.AddSeconds(-3)));
    }

    [Fact]
    public async Task TakeTheFirstFreeColourWhenGreenIsUsed()
    {
        registry.Running(100, Fixtures.Noon);
        await store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), Green, Fixtures.Noon));

        OwnTabClaim claim = await Tab.ClaimAsync(processId: 100);

        claim.ShouldBe(new OwnTabClaim.Claimed(TabPalette.Default[0]));
    }

    [Fact]
    public async Task ClaimNothingWhenItsProcessIsUnknown()
    {
        OwnTabClaim claim = await Tab.ClaimAsync(processId: 100);

        claim.ShouldBe(new OwnTabClaim.Unregistered());
        (await store.ReadTabsAsync()).ShouldBeEmpty();
    }
}