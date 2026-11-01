using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application.Diagnostics;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class RefreshCountsShould
{
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero));
    private readonly DiagnosticLog log;
    private readonly RefreshCounts refreshes;

    public RefreshCountsShould()
    {
        log = new DiagnosticLog(clock, []);
        refreshes = new RefreshCounts(clock, log);
    }

    [Fact]
    public async Task CountACompletedPart()
    {
        int listed = await refreshes.TimeAsync(RefreshPart.SessionsListing, () => Taking(TimeSpan.FromMilliseconds(1700), 3));

        listed.ShouldBe(3);
        refreshes.Snapshot().ShouldBe([new RefreshCount(RefreshPart.SessionsListing, Figures(1, 1700))]);
        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordAPartSlowerThanThreeSeconds()
    {
        await refreshes.TimeAsync(RefreshPart.WorktreesScan, () => Taking(TimeSpan.FromMilliseconds(3400), 0));

        refreshes.Snapshot().ShouldBe([new RefreshCount(RefreshPart.WorktreesScan, Figures(1, 3400))]);
        log.Snapshot().Select(timed => timed.Event).ShouldBe([new RefreshSlow(RefreshPart.WorktreesScan, TimeSpan.FromMilliseconds(3400))]);
    }

    [Fact]
    public async Task NotRecordAPartOfExactlyThreeSeconds()
    {
        await refreshes.TimeAsync(RefreshPart.SessionsEnrichment, () => Taking(TimeSpan.FromSeconds(3), 0));

        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task NotCountACancelledOrFailedPart()
    {
        await Should.ThrowAsync<OperationCanceledException>(() => refreshes.TimeAsync<int>(RefreshPart.WorktreesScan, () => Throwing(new OperationCanceledException())));
        await Should.ThrowAsync<InvalidOperationException>(() => refreshes.TimeAsync<int>(RefreshPart.SessionsListing, () => Throwing(new InvalidOperationException("listing failed"))));

        refreshes.Snapshot().ShouldBeEmpty();
        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task ListThePartsInTheirOrder()
    {
        await refreshes.TimeAsync(RefreshPart.WorktreesScan, () => Taking(TimeSpan.FromMilliseconds(800), 0));
        refreshes.Add(RefreshPart.PullRequestRefresh, TimeSpan.FromMilliseconds(1200));
        await refreshes.TimeAsync(RefreshPart.SessionsListing, () => Taking(TimeSpan.FromMilliseconds(1700), 0));

        refreshes.Snapshot().Select(count => count.Part).ShouldBe([RefreshPart.SessionsListing, RefreshPart.WorktreesScan, RefreshPart.PullRequestRefresh]);
    }

    private static DurationFigures Figures(int runs, int milliseconds)
    {
        TimeSpan duration = TimeSpan.FromMilliseconds(milliseconds);

        return new DurationFigures(runs, duration, duration, duration);
    }

    private async Task<int> Taking(TimeSpan duration, int answer)
    {
        await Task.Yield();
        clock.Advance(duration);

        return answer;
    }

    private async Task<int> Throwing(Exception exception)
    {
        await Task.Yield();
        clock.Advance(TimeSpan.FromSeconds(4));

        throw exception;
    }
}
