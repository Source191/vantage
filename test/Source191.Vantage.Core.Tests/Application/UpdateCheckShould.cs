using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class UpdateCheckShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly ToolVersion Running = new("0.9.0");
    private static readonly ReleaseVersion Current = ReleaseVersion.TryParse("0.9.0")!;
    private static readonly ReleaseVersion Newer = ReleaseVersion.TryParse("0.10.0")!;

    private readonly InMemoryState state = new();
    private readonly FakePackageVersions packages = new(["0.8.0", "0.9.0"]);
    private readonly FakeTimeProvider clock = new(Noon);

    [Fact]
    public async Task OfferTheRecordedReleaseAtStart()
    {
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord(Noon.AddHours(-1), "0.10.0"));
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        check.Status.ShouldBe(new UpdateStatus.Available(Current, Newer));
        packages.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task RequestOnceAndRecordTheNewest()
    {
        packages.Answer = ["0.9.0", "0.10.0", "0.11.0-rc.1"];
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.Calls.ShouldBe(1);
        packages.LastPackageId.ShouldBe("Source191.Vantage");
        check.Status.ShouldBe(new UpdateStatus.Available(Current, Newer));
        (await state.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord(Noon, "0.10.0"));
    }

    [Fact]
    public async Task RecordACheckThatFoundNoRelease()
    {
        packages.Answer = ["1.0.0-rc.1"];
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        check.Status.ShouldBe(new UpdateStatus.UpToDate());
        (await state.ReadUpdateCheckAsync()).ShouldBe(new UpdateCheckRecord(Noon, null));
    }

    [Fact]
    public async Task NotRequestWithinADay()
    {
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord(Noon.AddHours(-23), "0.9.0"));
        UpdateCheck check = Check(Running);

        await check.RunAsync(TestContext.Current.CancellationToken);

        packages.Calls.ShouldBe(0);
        check.Status.ShouldBe(new UpdateStatus.UpToDate());
    }

    [Fact]
    public async Task RecordNothingWhenTheRequestFails()
    {
        UpdateCheckRecord old = new(Noon.AddDays(-2), "0.10.0");
        await state.WriteUpdateCheckAsync(old);
        packages.Answer = null;
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
        packages.Answer = ["0.10.0"];
        bool enabled = true;
        UpdateCheck check = new(Running, () => enabled, state, packages, clock);
        await check.RunAsync(TestContext.Current.CancellationToken);

        enabled = false;

        check.Status.ShouldBe(new UpdateStatus.Off());
    }

    private UpdateCheck Check(ToolVersion running, bool enabled = true) => new(running, () => enabled, state, packages, clock);
}
