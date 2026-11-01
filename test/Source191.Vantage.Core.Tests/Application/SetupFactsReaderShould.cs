using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class SetupFactsReaderShould
{
    private const string SettingsFile = @"C:\Users\me\AppData\Local\Vantage\config.json";
    private const string LogFile = @"C:\Users\me\vantage.log";

    [Fact]
    public async Task ReadEverythingSetUp()
    {
        FakeTerminalFragment fragment = new FakeTerminalFragment().Installed();
        InMemoryState state = new();
        DateTimeOffset nine = new(2026, 10, 30, 9, 12, 0, TimeSpan.Zero);
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(nine, new NewestRelease.Found(ReleaseVersions.Version("0.18.0"))));
        SetupFactsReader reader = new(fragment, new FakeClaudeSettings(new NotificationChannelState.Configured("terminal_bell")), state, SettingsFile, new DiagnosticLogSetting.File(LogFile));

        SetupFacts facts = await reader.ReadAsync(TestContext.Current.CancellationToken);

        facts.ShouldBe(new SetupFacts(new SetupState.At(SettingsFile), new SetupState.At(fragment.Location), new SetupState.Enabled(), new SetupState.At(LogFile), new SetupState.Checked(nine, "0.18.0")));
    }

    [Fact]
    public async Task ReadNothingSetUp()
    {
        SetupFactsReader reader = new(new FakeTerminalFragment(), new FakeClaudeSettings(new NotificationChannelState.Unset()), new InMemoryState(), SettingsFile, new DiagnosticLogSetting.Off());

        SetupFacts facts = await reader.ReadAsync(TestContext.Current.CancellationToken);

        facts.ShouldBe(new SetupFacts(new SetupState.At(SettingsFile), new SetupState.NotSetUp(), new SetupState.NotSetUp(), new SetupState.Disabled(), new SetupState.Never()));
    }

    [Fact]
    public async Task CallAMissingClaudeSettingsFileNotSetUp()
    {
        SetupFactsReader reader = new(new FakeTerminalFragment(), new FakeClaudeSettings(new NotificationChannelState.Missing()), new InMemoryState(), SettingsFile, DiagnosticLogSetting.Read("  "));

        SetupFacts facts = await reader.ReadAsync(TestContext.Current.CancellationToken);

        facts.ClaudeBell.ShouldBe(new SetupState.NotSetUp());
        facts.DiagnosticLog.ShouldBe(new SetupState.Disabled());
    }

    [Fact]
    public async Task NameAnotherChannel()
    {
        SetupFactsReader reader = new(new FakeTerminalFragment(), new FakeClaudeSettings(new NotificationChannelState.Configured("iterm2")), new InMemoryState(), SettingsFile, new DiagnosticLogSetting.Off());

        (await reader.ReadAsync(TestContext.Current.CancellationToken)).ClaudeBell.ShouldBe(new SetupState.OtherChannel("iterm2"));
    }

    [Fact]
    public async Task KeepTheOtherFactsWhenClaudesSettingsAreUnreadable()
    {
        SetupFactsReader reader = new(new FakeTerminalFragment().Installed(), new FakeClaudeSettings(new NotificationChannelState.Unreadable("not JSON")), new InMemoryState(), SettingsFile, new DiagnosticLogSetting.Off());

        SetupFacts facts = await reader.ReadAsync(TestContext.Current.CancellationToken);

        facts.ClaudeBell.ShouldBe(new SetupState.Unreadable());
        facts.TerminalProfiles.ShouldBeOfType<SetupState.At>();
    }

    [Fact]
    public async Task KeepTheOtherFactsWhenAReadThrows()
    {
        FakeClaudeSettings claude = new() { FailTheReadWith = new IOException("locked") };
        SetupFactsReader reader = new(new FakeTerminalFragment().Installed(), claude, new InMemoryState(), SettingsFile, new DiagnosticLogSetting.Off());

        SetupFacts facts = await reader.ReadAsync(TestContext.Current.CancellationToken);

        facts.ClaudeBell.ShouldBe(new SetupState.Unreadable());
        facts.TerminalProfiles.ShouldBeOfType<SetupState.At>();
    }
}