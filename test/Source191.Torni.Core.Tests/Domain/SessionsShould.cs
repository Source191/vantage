using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Domain;

public sealed class SessionsShould
{
    [Fact]
    public void PutSessionsNeedingInputBeforeWorkingAndStoppedOnes()
    {
        Sessions sessions = new([
            Session("stopped", DisplayState.Stopped, Fixtures.Noon),
            Session("working", DisplayState.Working, Fixtures.Noon),
            Session("waiting", DisplayState.NeedsInput, Fixtures.Noon),
        ]);

        IReadOnlyList<Session> ordered = sessions.OrderedByAttention();

        ordered.Select(session => session.Name).ShouldBe([Listed.As("waiting"), Listed.As("working"), Listed.As("stopped")]);
    }

    [Fact]
    public void OrderTheSixStatesByAttentionNeeded()
    {
        Sessions sessions = new([
            Session("unknown", new DisplayState.Unknown("odd"), Fixtures.Noon),
            Session("stopped", DisplayState.Stopped, Fixtures.Noon),
            Session("failed", DisplayState.Failed, Fixtures.Noon),
            Session("completed", DisplayState.Completed, Fixtures.Noon),
            Session("idle", DisplayState.Idle, Fixtures.Noon),
            Session("working", DisplayState.Working, Fixtures.Noon),
            Session("waiting", DisplayState.NeedsInput, Fixtures.Noon),
        ]);

        IReadOnlyList<Session> ordered = sessions.OrderedByAttention();

        ordered.Select(session => session.Name).ShouldBe([Listed.As("waiting"), Listed.As("working"), Listed.As("idle"), Listed.As("completed"), Listed.As("failed"), Listed.As("stopped"), Listed.As("unknown")]);
    }

    [Fact]
    public void PutTheMostRecentlyStartedFirstWithinAState()
    {
        Sessions sessions = new([
            Session("older", DisplayState.Working, Fixtures.Noon.AddHours(-2)),
            Session("newer", DisplayState.Working, Fixtures.Noon),
        ]);

        IReadOnlyList<Session> ordered = sessions.OrderedByAttention();

        ordered.Select(session => session.Name).ShouldBe([Listed.As("newer"), Listed.As("older")]);
    }

    private static Session.Background Session(string name, DisplayState state, DateTimeOffset startedAt)
    {
        return new Session.Background(
            SessionId: SessionId.From(Guid.NewGuid().ToString()),
            JobId: JobId.From("81cdb0f9"),
            Name: Listed.As(name),
            WorkingDirectory: FolderPath.From(@"C:\repos\Northwind.Orders"),
            StartedAt: startedAt,
            State: state,
            Process: new JobProcess.None());
    }
}