using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class OpenTabHandlerShould
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Vantage = @"C:\Users\me\.dotnet\tools\vantage.exe";

    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeWindowsTerminal terminal = new();
    private readonly FakeProcessRegistry registry = new();

    private readonly InMemoryState Store = new();

    private OpenTabHandler Handler => new(Store, registry, terminal, new FakeTimeProvider(Noon), new TabScripts(Claude, Vantage));

    private static BackgroundSession Job() => new(SessionId.From("sess-1"), JobId.From("81cdb0f9"), "Refund retry policy", FolderPath.From(@"C:\repos\Northwind.Orders"), Noon, DisplayState.Working);

    [Fact]
    public async Task OpenATabTitledWithTheSessionNameThatClaimsItselfThenAttaches()
    {
        Outcome outcome = await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.None()), CancellationToken.None);

        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Refund retry policy");
        tab.StartingDirectory.Value.ShouldBe(@"C:\repos\Northwind.Orders");
        tab.Colour.ShouldBe(TabPalette.Default[0]);
        tab.Script.ShouldContain($"& '{Vantage}' 'claim-tab' '");
        tab.Script.ShouldContain("' $PID", customMessage: "the tab shell must pass its own process id, unquoted, so the registration outlives claim-tab");
        tab.Script.ShouldContain("Clear-Host", customMessage: "profile output must be cleared before the full-screen attach");
        tab.Script.ShouldContain($"& '{Claude}' 'attach' '81cdb0f9'");
        outcome.ShouldBeOfType<OpenTabOutcome.Opened>();
    }

    [Fact]
    public async Task OpenATabForAJustStartedSessionThatIsNotARowYet()
    {
        OpenTabOutcome outcome = await Handler.OpenAsync(Job(), CancellationToken.None);

        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Refund retry policy");
        tab.StartingDirectory.Value.ShouldBe(@"C:\repos\Northwind.Orders");
        tab.Script.ShouldContain($"& '{Claude}' 'attach' '81cdb0f9'");
        (await Store.ReadTabsAsync()).Single().Owner.ShouldBe(new TabOwner.ForSession(SessionId.From("sess-1")));
        outcome.ShouldBeOfType<OpenTabOutcome.Opened>().Name.ShouldBe("Refund retry policy");
    }

    [Fact]
    public async Task RecordAPendingRegistrationForTheTab()
    {
        await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.None()), CancellationToken.None);

        TabRegistration pending = (await Store.ReadTabsAsync()).Single();
        pending.Owner.ShouldBe(new TabOwner.ForSession(SessionId.From("sess-1")));
        pending.Claim.ShouldBe(new TabClaim.Pending());
        pending.CreatedAt.ShouldBe(Noon);
        terminal.Opened.Single().Script.ShouldContain(pending.Id.ToString("N"));
    }

    [Fact]
    public async Task ChooseAColourNoLiveTabIsUsing()
    {
        await Store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("other")), TabPalette.Default[0], Noon.AddSeconds(-5)));

        await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.None()), CancellationToken.None);

        terminal.Opened.Single().Colour.ShouldBe(TabPalette.Default[1]);
    }

    [Fact]
    public async Task SkipTheColourOfTheVantageTabItself()
    {
        registry.Running(100, Noon);
        await Store.WriteTabAsync(TabRegistration.Pending(TabOwner.Vantage, TabPalette.Default[0], Noon).ClaimedBy(100, Noon));

        await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.None()), CancellationToken.None);

        terminal.Opened.Single().Colour.ShouldBe(TabPalette.Default[1]);
    }

    [Fact]
    public async Task SwitchToTheLiveTabInsteadOfOpeningAnother()
    {
        await Store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Noon).ClaimedBy(4242, Noon));
        registry.Running(4242, Noon).InTree(4242, "pwsh", 777).InTree(777, "WindowsTerminal", 1);

        Outcome outcome = await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.Open(TabPalette.Default[0])), CancellationToken.None);

        terminal.Opened.ShouldBeEmpty();
        terminal.Focused.Single().ShouldBe((777, "Refund retry policy"));
        outcome.ShouldBe(new OpenTabOutcome.SwitchedTo("Refund retry policy"));
    }

    [Fact]
    public async Task OpenANewTabWhenTheRegisteredShellHasGone()
    {
        TabRegistration stale = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Noon.AddMinutes(-10)).ClaimedBy(4242, Noon.AddMinutes(-9));
        await Store.WriteTabAsync(stale);

        Outcome outcome = await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.Open(TabPalette.Default[0])), CancellationToken.None);

        terminal.Focused.ShouldBeEmpty();
        terminal.Opened.Single().Title.ShouldBe("Refund retry policy");
        (await Store.ReadTabsAsync()).ShouldNotContain(registration => registration.Id == stale.Id);
        outcome.ShouldBeOfType<OpenTabOutcome.Opened>();
    }

    [Fact]
    public async Task DropEarlierRegistrationsForTheSession()
    {
        TabRegistration earlier = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Noon.AddMinutes(-10)).ClaimedBy(4242, Noon.AddMinutes(-9));
        registry.Running(4242, Noon.AddMinutes(-9));
        await Store.WriteTabAsync(earlier);

        await Handler.OpenAsync(Job(), CancellationToken.None);

        TabRegistration only = (await Store.ReadTabsAsync()).ShouldHaveSingleItem();
        only.Id.ShouldNotBe(earlier.Id);
        only.Colour.ShouldBe(TabPalette.Default[0], "the session's earlier tab no longer holds its colour");
    }

    [Fact]
    public async Task OpenANewTabWhenNoRegistrationExists()
    {
        Outcome outcome = await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.Open(TabPalette.Default[0])), CancellationToken.None);

        terminal.Focused.ShouldBeEmpty();
        terminal.Opened.ShouldHaveSingleItem();
        outcome.ShouldBeOfType<OpenTabOutcome.Opened>();
    }

    [Fact]
    public async Task SearchEveryWindowWhenTheTabShellHasNoKnownParent()
    {
        await Store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Noon));

        await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.Open(TabPalette.Default[0])), CancellationToken.None);

        terminal.Focused.Single().ShouldBe(((int?)null, "Refund retry policy"));
    }

    [Fact]
    public async Task SayWhenTheLiveTabCannotBeFound()
    {
        terminal.FocusFinds = false;
        await Store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Noon));

        Outcome outcome = await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.Open(TabPalette.Default[0])), CancellationToken.None);

        terminal.Opened.ShouldBeEmpty();
        outcome.ShouldBe(new OpenTabOutcome.NotFound("Refund retry policy"));
    }

    [Fact]
    public async Task ClaimARegistrationWithTheTabProcess()
    {
        TabRegistration pending = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Noon);
        await Store.WriteTabAsync(pending);
        registry.Running(4242, Noon.AddSeconds(2));

        await new TabClaimer(Store, registry).ClaimAsync(pending.Id, processId: 4242);

        TabRegistration claimed = (await Store.ReadTabsAsync()).Single();
        claimed.Claim.ShouldBe(new TabClaim.Claimed(4242, Noon.AddSeconds(2)));
    }
}