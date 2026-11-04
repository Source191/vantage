using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Application;
using Source191.Torni.Application.Actions;
using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application.Actions;

public sealed class OpenTabHandlerShould
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Torni = @"C:\Users\me\.dotnet\tools\torni.exe";

    private readonly FakeWindowsTerminal terminal = new();
    private readonly FakeProcessRegistry registry = new();

    private readonly InMemoryState store = new();

    private OpenTabHandler Handler => new(store, registry, terminal, new FakeTimeProvider(Fixtures.Noon), new TabScripts(Claude, Torni));

    private static Session.Background Job() => new(SessionId.From("sess-1"), JobId.From("81cdb0f9"), Listed.As("Refund retry policy"), FolderPath.From(@"C:\repos\Northwind.Orders"), Fixtures.Noon, DisplayState.Working, new JobProcess.None());

    [Fact]
    public async Task OpenATabTitledWithTheSessionNameThatClaimsItselfThenAttaches()
    {
        Outcome outcome = await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.None()), CancellationToken.None);

        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Refund retry policy");
        tab.StartingDirectory.Value.ShouldBe(@"C:\repos\Northwind.Orders");
        tab.Colour.ShouldBe(TabPalette.Default[0]);
        tab.Script.ShouldContain($"& '{Torni}' 'claim-tab' '");
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
        (await store.ReadTabsAsync()).Single().Owner.ShouldBe(new TabOwner.ForSession(SessionId.From("sess-1")));
        outcome.ShouldBeOfType<OpenTabOutcome.Opened>().Name.ShouldBe(Listed.As("Refund retry policy"));
    }

    [Fact]
    public async Task RecordAPendingRegistrationForTheTab()
    {
        await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.None()), CancellationToken.None);

        TabRegistration pending = (await store.ReadTabsAsync()).Single();
        pending.Owner.ShouldBe(new TabOwner.ForSession(SessionId.From("sess-1")));
        pending.Claim.ShouldBe(new TabClaim.Pending());
        pending.CreatedAt.ShouldBe(Fixtures.Noon);
        terminal.Opened.Single().Script.ShouldContain(pending.Id.ToString("N"));
    }

    [Fact]
    public async Task ChooseAColourNoLiveTabIsUsing()
    {
        await store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("other")), TabPalette.Default[0], Fixtures.Noon.AddSeconds(-5)));

        await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.None()), CancellationToken.None);

        terminal.Opened.Single().Colour.ShouldBe(TabPalette.Default[1]);
    }

    [Fact]
    public async Task SkipTheColourOfTheTorniTabItself()
    {
        registry.Running(100, Fixtures.Noon);
        await store.WriteTabAsync(TabRegistration.Pending(TabOwner.Torni, TabPalette.Default[0], Fixtures.Noon).ClaimedBy(100, Fixtures.Noon));

        await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.None()), CancellationToken.None);

        terminal.Opened.Single().Colour.ShouldBe(TabPalette.Default[1]);
    }

    [Fact]
    public async Task SwitchToTheLiveTabInsteadOfOpeningAnother()
    {
        await store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Fixtures.Noon).ClaimedBy(4242, Fixtures.Noon));
        registry.Running(4242, Fixtures.Noon).InTree(4242, "pwsh", 777).InTree(777, "WindowsTerminal", 1, Fixtures.Noon.AddHours(-1));

        Outcome outcome = await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.Open(TabPalette.Default[0])), CancellationToken.None);

        terminal.Opened.ShouldBeEmpty();
        terminal.Focused.Single().ShouldBe((new TerminalWindow.Owning(777), "Refund retry policy"));
        outcome.ShouldBe(new OpenTabOutcome.SwitchedTo(Listed.As("Refund retry policy")));
    }

    [Fact]
    public async Task SearchForTheTitleTheTabWasOpenedWith()
    {
        Session.Background unnamed = Job() with { Name = Listed.None() };
        await Handler.HandleAsync(new OpenTabForSession(unnamed, new LiveTab.None()), CancellationToken.None);
        await store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Fixtures.Noon).ClaimedBy(4242, Fixtures.Noon));
        registry.Running(4242, Fixtures.Noon).InTree(4242, "pwsh", 777).InTree(777, "WindowsTerminal", 1, Fixtures.Noon.AddHours(-1));

        await Handler.HandleAsync(new OpenTabForSession(unnamed, new LiveTab.Open(TabPalette.Default[0])), CancellationToken.None);

        terminal.Focused.Single().Item2.ShouldBe(terminal.Opened.Single().Title);
    }

    [Fact]
    public async Task OpenANewTabWhenTheRegisteredShellHasGone()
    {
        TabRegistration stale = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Fixtures.Noon.AddMinutes(-10)).ClaimedBy(4242, Fixtures.Noon.AddMinutes(-9));
        await store.WriteTabAsync(stale);

        Outcome outcome = await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.Open(TabPalette.Default[0])), CancellationToken.None);

        terminal.Focused.ShouldBeEmpty();
        terminal.Opened.Single().Title.ShouldBe("Refund retry policy");
        (await store.ReadTabsAsync()).ShouldNotContain(registration => registration.Id == stale.Id);
        outcome.ShouldBeOfType<OpenTabOutcome.Opened>();
    }

    [Fact]
    public async Task DropEarlierRegistrationsForTheSession()
    {
        TabRegistration earlier = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Fixtures.Noon.AddMinutes(-10)).ClaimedBy(4242, Fixtures.Noon.AddMinutes(-9));
        registry.Running(4242, Fixtures.Noon.AddMinutes(-9));
        await store.WriteTabAsync(earlier);

        await Handler.OpenAsync(Job(), CancellationToken.None);

        TabRegistration only = (await store.ReadTabsAsync()).ShouldHaveSingleItem();
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
        await store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Fixtures.Noon));

        await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.Open(TabPalette.Default[0])), CancellationToken.None);

        terminal.Focused.Single().ShouldBe((new TerminalWindow.Any(), "Refund retry policy"));
    }

    [Fact]
    public async Task SayWhenTheLiveTabCannotBeFound()
    {
        terminal.FocusFinds = false;
        await store.WriteTabAsync(TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Fixtures.Noon));

        Outcome outcome = await Handler.HandleAsync(new OpenTabForSession(Job(), new LiveTab.Open(TabPalette.Default[0])), CancellationToken.None);

        terminal.Opened.ShouldBeEmpty();
        outcome.ShouldBe(new OpenTabOutcome.NotFound(Listed.As("Refund retry policy")));
    }

    [Fact]
    public async Task ClaimARegistrationWithTheTabProcess()
    {
        TabRegistration pending = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("sess-1")), TabPalette.Default[0], Fixtures.Noon);
        await store.WriteTabAsync(pending);
        registry.Running(4242, Fixtures.Noon.AddSeconds(2));

        await new TabClaimer(store, registry).ClaimAsync(pending.Id, processId: 4242);

        TabRegistration claimed = (await store.ReadTabsAsync()).Single();
        claimed.Claim.ShouldBe(new TabClaim.Claimed(4242, Fixtures.Noon.AddSeconds(2)));
    }

    [Fact]
    public async Task LeaveThePendingRegistrationWhenTheTerminalRefusesTheTab()
    {
        terminal.FailTheOpenWith = new TerminalException("wt refused");

        await Should.ThrowAsync<TerminalException>(() => Handler.OpenAsync(Job(), CancellationToken.None));

        (await store.ReadTabsAsync()).Single().Claim.ShouldBe(new TabClaim.Pending());
        terminal.Opened.ShouldBeEmpty();
    }
}