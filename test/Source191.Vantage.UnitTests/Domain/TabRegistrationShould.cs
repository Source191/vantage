using Source191.Vantage.Domain;
using Source191.Vantage.UnitTests.TestDoubles;

namespace Source191.Vantage.UnitTests.Domain;

public sealed class TabRegistrationShould
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ProcessStart = Now.AddMinutes(-5);

    [Fact]
    public void BeLiveWhilePendingWithinTheGracePeriod()
    {
        TabRegistration pending = Pending(createdAt: Now.AddSeconds(-30));

        pending.IsLive(Now, new FakeProcessRegistry()).ShouldBeTrue();
    }

    [Fact]
    public void BeStaleWhenPendingForOverAMinute()
    {
        TabRegistration pending = Pending(createdAt: Now.AddSeconds(-61));

        pending.IsLive(Now, new FakeProcessRegistry()).ShouldBeFalse();
    }

    [Fact]
    public void BeStaleTheMomentTheGracePeriodEnds()
    {
        TabRegistration pending = Pending(createdAt: Now.AddSeconds(-60));

        pending.IsLive(Now, new FakeProcessRegistry()).ShouldBeFalse();
    }

    [Fact]
    public void BeLiveWhenItsProcessIsRunningWithTheSameStartTime()
    {
        TabRegistration claimed = Pending(createdAt: Now.AddMinutes(-10)).ClaimedBy(processId: 4242, ProcessStart);
        FakeProcessRegistry processes = new FakeProcessRegistry().Running(4242, ProcessStart);

        claimed.IsLive(Now, processes).ShouldBeTrue();
    }

    [Fact]
    public void BeStaleWhenItsProcessIsGone()
    {
        TabRegistration claimed = Pending(createdAt: Now.AddMinutes(-10)).ClaimedBy(processId: 4242, ProcessStart);

        claimed.IsLive(Now, new FakeProcessRegistry()).ShouldBeFalse();
    }

    [Fact]
    public void BeStaleWhenTheProcessIdWasReusedByAnotherProcess()
    {
        TabRegistration claimed = Pending(createdAt: Now.AddMinutes(-10)).ClaimedBy(processId: 4242, ProcessStart);
        FakeProcessRegistry processes = new FakeProcessRegistry().Running(4242, ProcessStart.AddMinutes(3));

        claimed.IsLive(Now, processes).ShouldBeFalse();
    }

    [Fact]
    public void KeepOnlyLiveRegistrationsInTheCollection()
    {
        TabRegistration live = Pending(createdAt: Now.AddSeconds(-10));
        TabRegistration stale = Pending(createdAt: Now.AddMinutes(-10));
        TabRegistrations registrations = new([live, stale]);

        IReadOnlyList<TabRegistration> remaining = registrations.Live(Now, new FakeProcessRegistry());

        remaining.ShouldBe([live]);
    }

    private static TabRegistration Pending(DateTimeOffset createdAt)
    {
        return TabRegistration.Pending(
            sessionId: Guid.NewGuid().ToString(),
            colour: TabPalette.Default[0],
            createdAt: createdAt);
    }
}