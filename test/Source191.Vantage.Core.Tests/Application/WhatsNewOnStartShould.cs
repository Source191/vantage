using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WhatsNewOnStartShould
{
    private const string Changelog = "## Unreleased\n### Added\n- Not yet (#175)\n## 0.21.0\n### Added\n- A key that opens the Northwind order (#171)\n## 0.20.0\n### Fixed\n- Renaming an order keeps its history (#169)\n## 0.19.0\n### Fixed\n- The supplier column no longer flickers (#150)\n";

    private readonly InMemoryState state = new();
    private readonly DiagnosticLog log = new(new FakeTimeProvider(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero)), []);

    [Fact]
    public async Task ShowTheChangesSinceTheVersionThatLastRanAndRecordTheRunningOne()
    {
        await state.WriteLastRunAsync(ReleaseVersions.Version("0.19.0"));

        WhatsNewAtStart shown = await OnStart("0.21.0", new FakePackagedReleaseNotes(Changelog)).DecideAsync();

        WhatsNewAtStart.Show show = shown.ShouldBeOfType<WhatsNewAtStart.Show>();
        show.Running.ShouldBe(ReleaseVersions.Version("0.21.0"));
        show.Sections.Select(section => section.Version.ToString()).ShouldBe(["0.21.0", "0.20.0"]);
        (await state.ReadLastRunAsync()).ShouldBe(new LastRun.Ran(ReleaseVersions.Version("0.21.0")));
    }

    [Fact]
    public async Task ShowNothingWhenTheSameVersionRanLast()
    {
        await state.WriteLastRunAsync(ReleaseVersions.Version("0.21.0"));
        FakePackagedReleaseNotes packaged = new(Changelog);

        (await OnStart("0.21.0", packaged).DecideAsync()).ShouldBe(WhatsNewAtStart.Nothing);
        packaged.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task ShowNothingOnAFirstEverStartAndRecordTheVersion()
    {
        FakePackagedReleaseNotes packaged = new(Changelog);

        (await OnStart("0.21.0", packaged).DecideAsync()).ShouldBe(WhatsNewAtStart.Nothing);

        (await state.ReadLastRunAsync()).ShouldBe(new LastRun.Ran(ReleaseVersions.Version("0.21.0")));
        packaged.Reads.ShouldBe(0);
    }

    [Theory]
    [InlineData("0.21.0-local")]
    [InlineData("0.21.0-preview.3")]
    [InlineData("")]
    public async Task ShowAndRecordNothingForADevelopmentBuild(string running)
    {
        await state.WriteLastRunAsync(ReleaseVersions.Version("0.19.0"));

        (await OnStart(running, new FakePackagedReleaseNotes(Changelog)).DecideAsync()).ShouldBe(WhatsNewAtStart.Nothing);

        (await state.ReadLastRunAsync()).ShouldBe(new LastRun.Ran(ReleaseVersions.Version("0.19.0")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShowNothingAndLogItWhenThePackagedNotesCannotBeRead(bool fileMissing)
    {
        await state.WriteLastRunAsync(ReleaseVersions.Version("0.19.0"));
        FakePackagedReleaseNotes packaged = fileMissing
            ? new FakePackagedReleaseNotes(new PackageNotes.NothingLearned())
            : new FakePackagedReleaseNotes("Release notes: https://github.com/Source191/vantage/releases/tag/v0.21.0");

        (await OnStart("0.21.0", packaged).DecideAsync()).ShouldBe(WhatsNewAtStart.Nothing);

        log.Snapshot().Select(timed => timed.Event).ShouldBe([new PackagedNotesUnreadable(new ToolVersion("0.21.0"))]);
        (await state.ReadLastRunAsync()).ShouldBe(new LastRun.Ran(ReleaseVersions.Version("0.21.0")));
    }

    [Fact]
    public async Task ShowNothingAfterGoingBackToAnOlderVersionAndRecordIt()
    {
        await state.WriteLastRunAsync(ReleaseVersions.Version("0.21.0"));

        (await OnStart("0.20.0", new FakePackagedReleaseNotes(Changelog)).DecideAsync()).ShouldBe(WhatsNewAtStart.Nothing);

        (await state.ReadLastRunAsync()).ShouldBe(new LastRun.Ran(ReleaseVersions.Version("0.20.0")));
    }

    [Fact]
    public async Task ShowNothingWhenTheNotesHaveNoSectionInRange()
    {
        await state.WriteLastRunAsync(ReleaseVersions.Version("0.21.0"));

        (await OnStart("0.22.0", new FakePackagedReleaseNotes(Changelog)).DecideAsync()).ShouldBe(WhatsNewAtStart.Nothing);
    }

    private WhatsNewOnStart OnStart(string running, FakePackagedReleaseNotes packaged) => new(new ToolVersion(running), state, packaged, log);
}