using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class SessionsShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PutSessionsNeedingInputBeforeWorkingAndStoppedOnes()
    {
        Sessions sessions = new([
            Session("stopped", DisplayState.Stopped, Noon),
            Session("working", DisplayState.Working, Noon),
            Session("waiting", DisplayState.NeedsInput, Noon),
        ]);

        IReadOnlyList<Session> ordered = sessions.OrderedByAttention();

        ordered.Select(session => session.Name).ShouldBe(["waiting", "working", "stopped"]);
    }

    [Fact]
    public void OrderTheSixStatesByAttentionNeeded()
    {
        Sessions sessions = new([
            Session("unknown", new DisplayState(DisplayStateKind.Unknown, "odd"), Noon),
            Session("stopped", DisplayState.Stopped, Noon),
            Session("failed", DisplayState.Failed, Noon),
            Session("completed", DisplayState.Completed, Noon),
            Session("idle", DisplayState.Idle, Noon),
            Session("working", DisplayState.Working, Noon),
            Session("waiting", DisplayState.NeedsInput, Noon),
        ]);

        IReadOnlyList<Session> ordered = sessions.OrderedByAttention();

        ordered.Select(session => session.Name).ShouldBe(["waiting", "working", "idle", "completed", "failed", "stopped", "unknown"]);
    }

    [Fact]
    public void PutTheMostRecentlyStartedFirstWithinAState()
    {
        Sessions sessions = new([
            Session("older", DisplayState.Working, Noon.AddHours(-2)),
            Session("newer", DisplayState.Working, Noon),
        ]);

        IReadOnlyList<Session> ordered = sessions.OrderedByAttention();

        ordered.Select(session => session.Name).ShouldBe(["newer", "older"]);
    }

    private static BackgroundSession Session(string name, DisplayState state, DateTimeOffset startedAt)
    {
        return new BackgroundSession(
            SessionId: SessionId.From(Guid.NewGuid().ToString()),
            JobId: JobId.From("81cdb0f9"),
            Name: name,
            WorkingDirectory: FolderPath.From(@"C:\repos\Northwind.Orders"),
            StartedAt: startedAt,
            State: state);
    }
}