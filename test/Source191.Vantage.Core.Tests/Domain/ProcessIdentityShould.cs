using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

/// <summary>A listed process id is acted on only while the process behind it is still the session's.</summary>
public sealed class ProcessIdentityShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AcceptAProcessThatStartedBeforeOrWhenTheSessionDid()
    {
        ProcessIdentity.StillTheSession(Noon.AddSeconds(-3), sessionStartedAt: Noon).ShouldBeTrue();
        ProcessIdentity.StillTheSession(Noon, sessionStartedAt: Noon).ShouldBeTrue();
    }

    [Fact]
    public void AllowAFewSecondsOfRounding()
    {
        ProcessIdentity.StillTheSession(Noon.AddSeconds(5), sessionStartedAt: Noon).ShouldBeTrue();
    }

    [Fact]
    public void RefuseAProcessThatStartedAfterTheSessionBecauseItsIdWasReused()
    {
        ProcessIdentity.StillTheSession(Noon.AddSeconds(6), sessionStartedAt: Noon).ShouldBeFalse();
        ProcessIdentity.StillTheSession(Noon.AddMinutes(10), sessionStartedAt: Noon).ShouldBeFalse();
    }

    [Fact]
    public void RefuseAProcessWhoseStartIsUnknown()
    {
        ProcessIdentity.StillTheSession(processStartedAt: null, sessionStartedAt: Noon).ShouldBeFalse();
    }
}