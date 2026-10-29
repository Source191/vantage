namespace Source191.Vantage.Domain;

/// <summary>
/// Whether the process behind a listed process id is still the session's. A session records its start after its
/// process has started, so the process started no later than the session; one that started after it is another
/// process given the same id once the session's had gone. A few seconds cover the rounding of the two clocks.
/// </summary>
public static class ProcessIdentity
{
    private static readonly TimeSpan Rounding = TimeSpan.FromSeconds(5);

    public static bool StillTheSession(DateTimeOffset? processStartedAt, DateTimeOffset sessionStartedAt) =>
        processStartedAt is DateTimeOffset started && started <= sessionStartedAt + Rounding;
}