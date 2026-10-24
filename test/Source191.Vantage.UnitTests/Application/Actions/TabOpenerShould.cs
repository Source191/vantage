using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.UnitTests.TestDoubles;

namespace Source191.Vantage.UnitTests.Application.Actions;

public sealed class TabOpenerShould : IDisposable
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Vantage = @"C:\Users\me\.dotnet\tools\vantage.exe";

    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly string stateFolder = Path.Combine(Path.GetTempPath(), "vantage-tabs-" + Guid.NewGuid().ToString("N"));
    private readonly FakeWindowsTerminal terminal = new();
    private readonly FakeProcessRegistry registry = new();

    private StateStore Store => new(stateFolder, TimeProvider.System);

    private TabOpener Opener => new(Store, registry, terminal, new FakeTimeProvider(Noon), Claude, Vantage);

    private static SessionRow Row(bool hasLiveTab = false)
    {
        Session session = new("sess-1", "81cdb0f9", "Refund retry policy", @"C:\repos\Northwind.Orders", SessionKind.Background, Noon, DisplayState.Working);

        return new SessionRow(session, "Northwind.Orders", "main", IsWorktree: false, Description: null, hasLiveTab, HasTranscript: true);
    }

    [Fact]
    public async Task OpenATabTitledWithTheSessionNameThatClaimsItselfThenAttaches()
    {
        string footer = await Opener.OpenAsync(Row(), CancellationToken.None);

        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Refund retry policy");
        tab.StartingDirectory.ShouldBe(@"C:\repos\Northwind.Orders");
        tab.Colour.ShouldBe(TabPalette.Default[0]);
        tab.Script.ShouldContain($"& '{Vantage}' 'claim-tab' '");
        tab.Script.ShouldContain("' $PID", customMessage: "the tab shell must pass its own process id, unquoted, so the registration outlives claim-tab");
        tab.Script.ShouldContain("Clear-Host", customMessage: "profile output must be cleared before the full-screen attach");
        tab.Script.ShouldContain($"& '{Claude}' 'attach' '81cdb0f9'");
        footer.ShouldContain("Opened");
    }

    [Fact]
    public async Task OpenATabForAJustStartedSessionThatIsNotARowYet()
    {
        string footer = await Opener.OpenAsync(Row().Session, CancellationToken.None);

        TabLaunch tab = terminal.Opened.Single();
        tab.Title.ShouldBe("Refund retry policy");
        tab.StartingDirectory.ShouldBe(@"C:\repos\Northwind.Orders");
        tab.Script.ShouldContain($"& '{Claude}' 'attach' '81cdb0f9'");
        (await Store.ReadTabsAsync()).Single().SessionId.ShouldBe("sess-1");
        footer.ShouldContain("Opened Refund retry policy in a");
    }

    [Fact]
    public async Task RecordAPendingRegistrationForTheTab()
    {
        await Opener.OpenAsync(Row(), CancellationToken.None);

        TabRegistration pending = (await Store.ReadTabsAsync()).Single();
        pending.SessionId.ShouldBe("sess-1");
        pending.ProcessId.ShouldBeNull();
        pending.CreatedAt.ShouldBe(Noon);
        terminal.Opened.Single().Script.ShouldContain(pending.Id.ToString("N"));
    }

    [Fact]
    public async Task ChooseAColourNoLiveTabIsUsing()
    {
        await Store.WriteTabAsync(TabRegistration.Pending("other", TabPalette.Default[0], Noon.AddSeconds(-5)));

        await Opener.OpenAsync(Row(), CancellationToken.None);

        terminal.Opened.Single().Colour.ShouldBe(TabPalette.Default[1]);
    }

    [Fact]
    public async Task SkipTheColourOfTheVantageTabItself()
    {
        registry.Running(100, Noon);
        await Store.WriteTabAsync(TabRegistration.Pending(OwnTab.RegistrationKey, TabPalette.Default[0], Noon).ClaimedBy(100, Noon));

        await Opener.OpenAsync(Row(), CancellationToken.None);

        terminal.Opened.Single().Colour.ShouldBe(TabPalette.Default[1]);
    }

    [Fact]
    public async Task SwitchToTheLiveTabInsteadOfOpeningAnother()
    {
        await Store.WriteTabAsync(TabRegistration.Pending("sess-1", TabPalette.Default[0], Noon).ClaimedBy(4242, Noon));
        registry.Running(4242, Noon).InTree(4242, "pwsh", 777).InTree(777, "WindowsTerminal", 1);

        string footer = await Opener.OpenAsync(Row(hasLiveTab: true), CancellationToken.None);

        terminal.Opened.ShouldBeEmpty();
        terminal.Focused.Single().ShouldBe((777, "Refund retry policy"));
        footer.ShouldBe("Switched to the tab of Refund retry policy.");
    }

    [Fact]
    public async Task SearchEveryWindowWhenTheTabShellHasNoKnownParent()
    {
        await Store.WriteTabAsync(TabRegistration.Pending("sess-1", TabPalette.Default[0], Noon));

        await Opener.OpenAsync(Row(hasLiveTab: true), CancellationToken.None);

        terminal.Focused.Single().ShouldBe(((int?)null, "Refund retry policy"));
    }

    [Fact]
    public async Task SayWhenTheLiveTabCannotBeFound()
    {
        terminal.FocusFinds = false;
        await Store.WriteTabAsync(TabRegistration.Pending("sess-1", TabPalette.Default[0], Noon));

        string footer = await Opener.OpenAsync(Row(hasLiveTab: true), CancellationToken.None);

        terminal.Opened.ShouldBeEmpty();
        footer.ShouldBe("Refund retry policy is open in another tab, but Vantage could not find it. Is it in another window?");
    }

    [Fact]
    public async Task ClaimARegistrationWithTheTabProcess()
    {
        TabRegistration pending = TabRegistration.Pending("sess-1", TabPalette.Default[0], Noon);
        await Store.WriteTabAsync(pending);
        registry.Running(4242, Noon.AddSeconds(2));

        await new TabClaimer(Store, registry).ClaimAsync(pending.Id, processId: 4242);

        TabRegistration claimed = (await Store.ReadTabsAsync()).Single();
        claimed.ProcessId.ShouldBe(4242);
        claimed.ProcessStartedAt.ShouldBe(Noon.AddSeconds(2));
    }

    public void Dispose()
    {
        if (Directory.Exists(stateFolder))
        {
            Directory.Delete(stateFolder, recursive: true);
        }
    }
}