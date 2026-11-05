using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Application;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application;

public sealed class SessionActivityShould
{
    private readonly FakeTimeProvider clock = new(Fixtures.Noon);

    [Theory]
    [MemberData(nameof(Prompted))]
    public void RecordTheTimeASessionWasPrompted(DisplayState before)
    {
        SessionActivity activity = new(clock);
        SessionRow row = Row(before);

        clock.Advance(TimeSpan.FromMinutes(3));
        activity.Observe([row], [WithState(row, DisplayState.Working)]);

        activity.LastPromptedAt(row.Session.SessionId).ShouldBe(new LastPrompt.Seen(Fixtures.Noon.AddMinutes(3)));
    }

    public static TheoryData<DisplayState> Prompted => new(DisplayState.NeedsInput, DisplayState.Idle);

    [Fact]
    public void IgnoreASessionThatStopsWorking()
    {
        SessionActivity activity = new(clock);
        SessionRow row = Row(DisplayState.Working);

        activity.Observe([row], [WithState(row, DisplayState.NeedsInput)]);

        activity.LastPromptedAt(row.Session.SessionId).ShouldBe(new LastPrompt.NotSeen());
    }

    [Fact]
    public void IgnoreAnInteractiveSession()
    {
        SessionActivity activity = new(clock);
        SessionRow row = Row(DisplayState.NeedsInput, interactive: true);

        activity.Observe([row], [WithState(row, DisplayState.Working)]);

        activity.LastPromptedAt(row.Session.SessionId).ShouldBe(new LastPrompt.NotSeen());
    }

    [Fact]
    public void AnswerNothingForASessionNeverSeenPrompted()
    {
        new SessionActivity(clock).LastPromptedAt(SessionId.From(Guid.NewGuid().ToString())).ShouldBe(new LastPrompt.NotSeen());
    }

    [Fact]
    public void KeepTheLatestPrompt()
    {
        SessionActivity activity = new(clock);
        SessionRow waiting = Row(DisplayState.NeedsInput);
        SessionRow working = WithState(waiting, DisplayState.Working);

        activity.Observe([waiting], [working]);
        clock.Advance(TimeSpan.FromMinutes(10));
        activity.Observe([working], [waiting]);
        activity.Observe([waiting], [working]);

        activity.LastPromptedAt(waiting.Session.SessionId).ShouldBe(new LastPrompt.Seen(Fixtures.Noon.AddMinutes(10)));
    }

    private static SessionRow WithState(SessionRow row, DisplayState state) => row with { Session = row.Session with { State = state } };

    private static SessionRow Row(DisplayState state, bool interactive = false)
    {
        SessionId id = SessionId.From(Guid.NewGuid().ToString());
        FolderPath directory = FolderPath.From(@"C:\repos\Northwind.Orders");
        Session session = interactive
            ? new Session.Interactive(id, Listed.As("fix-123"), directory, Fixtures.Noon, state, ProcessId: 4242)
            : new Session.Background(id, JobId.From("81cdb0f9"), Listed.As("fix-123"), directory, Fixtures.Noon, state, new JobProcess.None());

        return new SessionRow(session, new SessionLocation.InRepository("Northwind.Orders", directory), new Head.OnBranch(BranchName.From("main")), new LiveTab.None(), HasTranscript: true);
    }
}