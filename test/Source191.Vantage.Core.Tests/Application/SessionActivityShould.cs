using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class SessionActivityShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider clock = new(Noon);

    [Theory]
    [MemberData(nameof(Prompted))]
    public void RecordTheTimeASessionWasPrompted(DisplayState before)
    {
        SessionActivity activity = new(clock);
        SessionRow row = Row(before);

        clock.Advance(TimeSpan.FromMinutes(3));
        activity.Observe([row], [WithState(row, DisplayState.Working)]);

        activity.LastPromptedAt(row.Session.SessionId).ShouldBe(Noon.AddMinutes(3));
    }

    public static TheoryData<DisplayState> Prompted => new(DisplayState.NeedsInput, DisplayState.Idle);

    [Fact]
    public void IgnoreASessionThatStopsWorking()
    {
        SessionActivity activity = new(clock);
        SessionRow row = Row(DisplayState.Working);

        activity.Observe([row], [WithState(row, DisplayState.NeedsInput)]);

        activity.LastPromptedAt(row.Session.SessionId).ShouldBeNull();
    }

    [Fact]
    public void IgnoreAnInteractiveSession()
    {
        SessionActivity activity = new(clock);
        SessionRow row = Row(DisplayState.NeedsInput, interactive: true);

        activity.Observe([row], [WithState(row, DisplayState.Working)]);

        activity.LastPromptedAt(row.Session.SessionId).ShouldBeNull();
    }

    [Fact]
    public void AnswerNothingForASessionNeverSeenPrompted()
    {
        new SessionActivity(clock).LastPromptedAt(SessionId.From(Guid.NewGuid().ToString())).ShouldBeNull();
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

        activity.LastPromptedAt(waiting.Session.SessionId).ShouldBe(Noon.AddMinutes(10));
    }

    private static SessionRow WithState(SessionRow row, DisplayState state) => row with { Session = row.Session with { State = state } };

    private static SessionRow Row(DisplayState state, bool interactive = false)
    {
        SessionId id = SessionId.From(Guid.NewGuid().ToString());
        FolderPath directory = FolderPath.From(@"C:\repos\Northwind.Orders");
        Session session = interactive
            ? new InteractiveSession(id, "fix-123", directory, Noon, state, ProcessId: 4242)
            : new BackgroundSession(id, JobId.From("81cdb0f9"), "fix-123", directory, Noon, state);

        return new SessionRow(session, "Northwind.Orders", new Head.OnBranch(BranchName.From("main")), new LiveTab.None(), HasTranscript: true);
    }
}