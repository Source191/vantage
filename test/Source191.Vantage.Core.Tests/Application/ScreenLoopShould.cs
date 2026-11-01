using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;
using System.Diagnostics;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class ScreenLoopShould : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly ToolVersion Running = new("0.9.0");

    private readonly FakeProcessRegistry registry = new FakeProcessRegistry().Running(100, Noon);
    private readonly FakeTerminalConsole console = new();
    private readonly DiagnosticLog log = new(new FakeTimeProvider(Noon), []);
    private readonly StringWriter terminal = new();

    private ScreenLoop Loop(ScriptedConsoleScreen screen, UpdateCheck? updates = null)
    {
        OwnTab ownTab = new(new InMemoryState(), registry, new FakeTimeProvider(Noon));
        UpdateCheck checkTurnedOff = new(Running, () => false, new InMemoryState(), new FakePackageVersions([]), new FakeTimeProvider(Noon));

        return new ScreenLoop(screen, ownTab, console, updates ?? checkTurnedOff, log);
    }

    [Fact]
    public async Task StartTheUpdateCheckWhenTheConsoleOpens()
    {
        InMemoryState records = new();
        await records.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon.AddHours(-1), new NewestRelease.Found(ReleaseVersions.Version("0.10.0"))));
        FakePackageVersions packages = new(["0.10.0"]);
        UpdateCheck updates = new(Running, () => true, records, packages, new FakeTimeProvider(Noon));
        ScreenLoop loop = Loop(new ScriptedConsoleScreen(state => new ConsoleExit(state, new Quit())), updates);

        await loop.RunAsync(processId: 100, CancellationToken.None);
        await loop.UpdateCheckTask;

        updates.Status.ShouldBe(new UpdateStatus.Available(ReleaseVersions.Version("0.9.0"), ReleaseVersions.Version("0.10.0")));
        packages.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task LogAFailedUpdateCheckAndKeepRunning()
    {
        UpdateCheck updates = new(Running, () => true, new UnwritableRecords(Noon.AddDays(-1)), new FakePackageVersions(["0.9.0"]), new FakeTimeProvider(Noon));
        ScreenLoop loop = Loop(new ScriptedConsoleScreen(state => new ConsoleExit(state, new Quit())), updates);

        int exitCode = await loop.RunAsync(processId: 100, CancellationToken.None);
        await loop.UpdateCheckTask;

        exitCode.ShouldBe(0);
        updates.Status.ShouldBe(new UpdateStatus.UpToDate());
        Failed failed = log.Snapshot().Select(timed => timed.Event).OfType<Failed>().Single();
        failed.Site.ShouldBe(FailureSite.UpdateCheck);
        failed.What.ShouldBe(CodeName.For(typeof(UpdateCheck)));
    }

    [Fact]
    public async Task LogNothingWhenTheUpdateCheckIsCancelled()
    {
        UpdateCheck updates = new(Running, () => true, new UnwritableRecords(Noon.AddDays(-1)), new FakePackageVersions(["0.9.0"]), new FakeTimeProvider(Noon));
        ScreenLoop loop = Loop(new ScriptedConsoleScreen(state => new ConsoleExit(state, new Quit())), updates);

        await loop.RunAsync(processId: 100, new CancellationToken(canceled: true));

        await Should.ThrowAsync<OperationCanceledException>(() => loop.UpdateCheckTask);
        log.Snapshot().Select(timed => timed.Event).OfType<Failed>().ShouldBeEmpty();
    }

    [Fact]
    public async Task LabelTheOwnTabAtStartAndClearItOnQuit()
    {
        ScriptedConsoleScreen screen = new(state => new ConsoleExit(state, new Quit()));

        int exitCode = await Loop(screen).RunAsync(processId: 100, CancellationToken.None);

        exitCode.ShouldBe(0);
        console.Coloured.ShouldBe([TabPalette.Default.Single(colour => colour.Name == "Green")]);
        console.TitledOnly.ShouldBe(0);
        console.Cleared.ShouldBe(1);
    }

    [Fact]
    public async Task TitleTheOwnTabAloneWhenItCannotBeRegistered()
    {
        ScriptedConsoleScreen screen = new(state => new ConsoleExit(state, new Quit()));

        await Loop(screen).RunAsync(processId: 200, CancellationToken.None);

        console.Coloured.ShouldBeEmpty("process 200 has no start time to register under");
        console.TitledOnly.ShouldBe(1);
    }

    [Fact]
    public async Task StartOnTheSessionsTabAndQuitWhenTheConsoleLeavesWithQuit()
    {
        ScriptedConsoleScreen screen = new(state => new ConsoleExit(state, new Quit()));

        await Loop(screen).RunAsync(processId: 100, CancellationToken.None);

        screen.Given.Single().ShouldBe(ConsoleState.Empty);
        console.Cleared.ShouldBe(1);
    }

    [Fact]
    public async Task RunTheScreenOnceAndRefuseAnyOtherWayOut()
    {
        ScriptedConsoleScreen screen = new(state => new ConsoleExit(state, new AttachToSession(JobId.From("81cdb0f9"))));

        await Should.ThrowAsync<UnreachableException>(() => Loop(screen).RunAsync(processId: 100, CancellationToken.None));

        screen.Given.Count.ShouldBe(1, "only quit leaves the console, so it is never run a second time");
        console.Recovered.ShouldBe(0);
        (console.Coloured.Count + console.TitledOnly).ShouldBe(1, "the tab is labelled once, at start");
    }

    public void Dispose()
    {
        terminal.Dispose();
    }

    /// <summary>An update-check record that reads but cannot be written, as when the state folder is read-only.</summary>
    private sealed class UnwritableRecords(DateTimeOffset checkedAt) : IUpdateCheckRecords
    {
        public Task<UpdateCheckRecord> ReadUpdateCheckAsync()
        {
            return Task.FromResult<UpdateCheckRecord>(new UpdateCheckRecord.Checked(checkedAt, new NewestRelease.Found(ReleaseVersions.Version("0.9.0"))));
        }

        public Task WriteUpdateCheckAsync(UpdateCheckRecord.Checked record)
        {
            throw new UnauthorizedAccessException(@"Access to C:\Users\me\AppData\Local\Vantage\update-check.json is denied.");
        }
    }
}