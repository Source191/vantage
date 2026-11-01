using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class UpdateCheckShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly ToolVersion Running = new("0.9.0");
    private static readonly ReleaseVersion Current = ReleaseVersions.Version("0.9.0");
    private static readonly ReleaseVersion Newer = ReleaseVersions.Version("0.10.0");
    private const string NewerNotes = "## 0.10.0\n### Added\n- A key that opens the Northwind order (#171)\n";

    private readonly InMemoryState state = new();
    private readonly FakePackageVersions packages = new(["0.8.0", "0.9.0"]);
    private readonly FakeTimeProvider clock = new(Noon);

    [Fact]
    public async Task OfferTheRecordedReleaseAtStart()
    {
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon.AddHours(-1), new NewestRelease.Found(Newer)));
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        check.Status.ShouldBe(new UpdateStatus.Available(Current, Newer));
        packages.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task RequestOnceAndRecordTheNewest()
    {
        packages.Answer = new VersionListing.Listed(["0.9.0", "0.10.0", "0.11.0-rc.1"]);
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.Calls.ShouldBe(1);
        packages.LastPackageId.ShouldBe("Source191.Vantage");
        check.Status.ShouldBe(new UpdateStatus.Available(Current, Newer));
        (await state.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord.Checked(Noon, new NewestRelease.Found(Newer)));
    }

    [Fact]
    public async Task RecordACheckThatFoundNoRelease()
    {
        packages.Answer = new VersionListing.Listed(["1.0.0-rc.1"]);
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        check.Status.ShouldBe(new UpdateStatus.UpToDate());
        (await state.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord.Checked(Noon, new NewestRelease.None()));
    }

    [Fact]
    public async Task NotRequestWithinADay()
    {
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon.AddHours(-23), new NewestRelease.Found(Current)));
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.Calls.ShouldBe(0);
        check.Status.ShouldBe(new UpdateStatus.UpToDate());
    }

    [Fact]
    public async Task OfferNothingForARecordedNewestThatIsNotAVersion()
    {
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon.AddHours(-1), new NewestRelease.Unreadable("rubbish")));
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.Calls.ShouldBe(0);
        check.Status.ShouldBe(new UpdateStatus.UpToDate());
    }

    [Fact]
    public async Task RequestAtOnceWhenNeverChecked()
    {
        packages.Answer = new VersionListing.Listed(["0.10.0"]);
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.Calls.ShouldBe(1);
        (await state.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord.Checked(Noon, new NewestRelease.Found(Newer)));
    }

    [Fact]
    public async Task RecordNothingWhenTheRequestFails()
    {
        UpdateCheckRecord.Checked old = new(Noon.AddDays(-2), new NewestRelease.Found(Newer));
        await state.WriteUpdateCheckAsync(old);
        packages.Answer = new VersionListing.NothingLearned();
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.Calls.ShouldBe(1);
        (await state.ReadUpdateCheckAsync()).ShouldBe(old);
        check.Status.ShouldBe(new UpdateStatus.Available(Current, Newer));
    }

    [Fact]
    public async Task NotRequestForAPrerelease()
    {
        UpdateCheck check = Check(new ToolVersion("0.10.0-local"));

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.Calls.ShouldBe(0);
        check.Status.ShouldBe(new UpdateStatus.Off());
    }

    [Fact]
    public async Task NotRequestWhenTurnedOff()
    {
        UpdateCheck check = Check(Running, enabled: false);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.Calls.ShouldBe(0);
        check.Status.ShouldBe(new UpdateStatus.Off());
    }

    [Fact]
    public void SayUpToDateBeforeTheCheckHasRun()
    {
        Check(Running).Status.ShouldBe(new UpdateStatus.UpToDate());
    }

    [Fact]
    public async Task ReadTheSettingEachTimeItIsAsked()
    {
        packages.Answer = new VersionListing.Listed(["0.10.0"]);
        bool enabled = true;
        UpdateCheck check = new(Running, () => enabled, state, packages, clock);
        await check.RunAsync(TestContext.Current.CancellationToken);

        enabled = false;

        check.Status.ShouldBe(new UpdateStatus.Off());
    }

    [Fact]
    public async Task RecordTheNotesOfANewerRelease()
    {
        packages.Answer = new VersionListing.Listed(["0.9.0", "0.10.0"]);
        packages.Notes["0.10.0"] = NewerNotes;
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.NotesAskedFor.ShouldBe([Newer]);
        (await state.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord.Checked(Noon, new NewestRelease.Found(Newer), Notes(NewerNotes)));
        check.Notes.ShouldBe(Notes(NewerNotes));
    }

    [Fact]
    public async Task AskForNoNotesWhenAlreadyNewest()
    {
        packages.Answer = new VersionListing.Listed(["0.8.0", "0.9.0"]);
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.NotesAskedFor.ShouldBeEmpty();
        (await state.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord.Checked(Noon, new NewestRelease.Found(Current), ReleaseNotes.None));
    }

    [Fact]
    public async Task KeepOfferingTheUpdateWhenItsNotesCannotBeRead()
    {
        packages.Answer = new VersionListing.Listed(["0.10.0"]);
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.NotesAskedFor.ShouldBe([Newer]);
        (await state.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord.Checked(Noon, new NewestRelease.Found(Newer), ReleaseNotes.None));
        check.Status.ShouldBe(new UpdateStatus.Available(Current, Newer));
        check.Notes.ShouldBe(ReleaseNotes.None);
    }

    [Fact]
    public async Task AskForTheNotesAgainWhenTheRecordHasNone()
    {
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon.AddDays(-2), new NewestRelease.Found(Newer)));
        packages.Answer = new VersionListing.Listed(["0.10.0"]);
        packages.Notes["0.10.0"] = NewerNotes;
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.NotesAskedFor.ShouldBe([Newer]);
        (await state.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord.Checked(Noon, new NewestRelease.Found(Newer), Notes(NewerNotes)));
    }

    [Fact]
    public async Task KeepTheRecordedNotesOfTheSameRelease()
    {
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon.AddDays(-2), new NewestRelease.Found(Newer), Notes(NewerNotes)));
        packages.Answer = new VersionListing.Listed(["0.10.0"]);
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.NotesAskedFor.ShouldBeEmpty();
        (await state.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord.Checked(Noon, new NewestRelease.Found(Newer), Notes(NewerNotes)));
    }

    [Fact]
    public async Task OfferTheRecordedNotesAtStart()
    {
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon.AddHours(-1), new NewestRelease.Found(Newer), Notes(NewerNotes)));
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        check.Notes.ShouldBe(Notes(NewerNotes));
    }

    [Fact]
    public async Task AnnounceANewlyFoundReleaseOnce()
    {
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon.AddDays(-2), new NewestRelease.Found(ReleaseVersions.Version("0.9.5"))));
        packages.Answer = new VersionListing.Listed(["0.9.5", "0.10.0"]);
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        check.TakeAnnouncement().ShouldBe(new StatusMessage.NewerReleaseFound(Newer));
        check.TakeAnnouncement().ShouldBe(StatusMessage.None);
    }

    [Fact]
    public async Task AnnounceAReleaseFoundOnTheFirstCheck()
    {
        packages.Answer = new VersionListing.Listed(["0.10.0"]);
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        check.TakeAnnouncement().ShouldBe(new StatusMessage.NewerReleaseFound(Newer));
    }

    [Fact]
    public async Task NotAnnounceAReleaseAlreadyRecorded()
    {
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon.AddDays(-2), new NewestRelease.Found(Newer)));
        packages.Answer = new VersionListing.Listed(["0.10.0"]);
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        check.TakeAnnouncement().ShouldBe(StatusMessage.None);
    }

    [Fact]
    public async Task NotAnnounceAReleaseRecordedWithinADay()
    {
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Noon.AddHours(-1), new NewestRelease.Found(Newer)));
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        check.TakeAnnouncement().ShouldBe(StatusMessage.None);
    }

    [Fact]
    public async Task NotAnnounceTheRunningVersion()
    {
        packages.Answer = new VersionListing.Listed(["0.9.0"]);
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        check.TakeAnnouncement().ShouldBe(StatusMessage.None);
    }

    private static ReleaseNotes Notes(string text)
    {
        ReleaseNotes.TryParse(text, out ReleaseNotes? notes).ShouldBeTrue();

        return notes;
    }

    private UpdateCheck Check(ToolVersion running, bool enabled = true) => new(running, () => enabled, state, packages, clock);
}