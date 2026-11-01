using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application.Diagnostics;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class HeartbeatShould
{
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero));
    private readonly StallCounts stalls = new();
    private readonly DiagnosticLog log;
    private readonly Heartbeat heartbeat;

    public HeartbeatShould()
    {
        log = new DiagnosticLog(clock, []);
        heartbeat = new Heartbeat(clock, stalls, log);
    }

    [Fact]
    public void BeatFourTimesASecond()
    {
        Heartbeat.Interval.ShouldBe(TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public void SetTheBaselineOnTheFirstBeat()
    {
        clock.Advance(TimeSpan.FromSeconds(10));

        heartbeat.Beat();

        stalls.Figures().ShouldBe(DurationFigures.None);
        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public void CountABeatMoreThanAHundredMillisecondsLate()
    {
        heartbeat.Beat();
        BeatAfter(250);
        BeatAfter(430);
        BeatAfter(300);
        BeatAfter(350);

        TimeSpan stall = TimeSpan.FromMilliseconds(180);
        stalls.Figures().ShouldBe(new DurationFigures(1, stall, stall, stall), "a beat 50 ms late, or exactly 100 ms late, is not a stall");
        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public void RecordABeatMoreThanHalfASecondLate()
    {
        heartbeat.Beat();
        BeatAfter(1070);
        BeatAfter(750);

        stalls.Figures().ShouldBe(new DurationFigures(2, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(820), TimeSpan.FromMilliseconds(820)));
        log.Snapshot().Select(timed => timed.Event).ShouldBe([new UiThreadStalled(TimeSpan.FromMilliseconds(820))], "a beat exactly 500 ms late is counted but not recorded");
    }

    [Fact]
    public void IgnoreAGapLongerThanThirtySeconds()
    {
        heartbeat.Beat();
        clock.Advance(TimeSpan.FromMinutes(40));
        heartbeat.Beat();
        BeatAfter(250);

        stalls.Figures().ShouldBe(DurationFigures.None);
        log.Snapshot().ShouldBeEmpty();

        BeatAfter(430);

        stalls.Figures().Longest.ShouldBe(TimeSpan.FromMilliseconds(180), "the beat after waking is the new baseline");
    }

    private void BeatAfter(int milliseconds)
    {
        clock.Advance(TimeSpan.FromMilliseconds(milliseconds));
        heartbeat.Beat();
    }
}
