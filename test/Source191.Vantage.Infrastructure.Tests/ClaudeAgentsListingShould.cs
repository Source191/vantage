using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Domain;
using System.Text.Json;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class ClaudeAgentsListingShould
{
    /// <summary>
    /// Captured from "claude agents --json --all" on 2026-10-23 (Claude Code 2.1.309): an interactive session,
    /// a background session, one waiting on a permission prompt, and one that was deliberately stopped.
    /// </summary>
    private const string Capture = """
        [
          {"pid":51432,"cwd":"C:\\Users\\me\\source\\repos","kind":"interactive","startedAt":1792591583841,"sessionId":"82db48ec-63de-49db-9831-b8147075dc3a","name":"repos-93","status":"idle"},
          {"pid":57456,"id":"1a4d2d86","cwd":"C:\\Users\\me\\source\\repos\\Source191.Vantage-spike-commit","kind":"background","startedAt":1792745753340,"sessionId":"1a4d2d86-8e87-44fe-a9f7-61d47d481ae3","name":"spike-commit-check","status":"idle","state":"done"},
          {"pid":67268,"id":"23abd87d","cwd":"C:\\Users\\me\\source\\repos\\Source191.Vantage-spike-commit","kind":"background","startedAt":1792745984436,"sessionId":"23abd87d-3498-436d-b264-7227a61036f8","name":"spike-permission-2","status":"waiting","waitingFor":"permission prompt","state":"blocked"},
          {"id":"fa82d736","cwd":"C:\\Users\\me\\source\\repos\\Source191.Vantage","kind":"background","startedAt":1792745856987,"sessionId":"fa82d736-d876-4840-b6c2-d62a6767e1c1","name":"spike-permission","state":"done"}
        ]
        """;

    private static readonly IReadOnlyList<Session> Sessions = ClaudeAgentsListing.Parse(Capture);

    [Fact]
    public void ParseEveryRow()
    {
        Sessions.Count.ShouldBe(4);
    }

    [Fact]
    public void ParseAnInteractiveSessionWithoutAJobId()
    {
        Session interactive = Sessions.Single(session => session.Name == "repos-93");

        interactive.Kind.ShouldBe(SessionKind.Interactive);
        interactive.JobId.ShouldBeNull();
        interactive.SessionId.ShouldBe("82db48ec-63de-49db-9831-b8147075dc3a");
        interactive.WorkingDirectory.ShouldBe(@"C:\Users\me\source\repos");
        interactive.State.ShouldBe(DisplayState.Idle);
    }

    [Fact]
    public void ParseABackgroundSessionWithItsJobId()
    {
        Session background = Sessions.Single(session => session.Name == "spike-commit-check");

        background.Kind.ShouldBe(SessionKind.Background);
        background.JobId.ShouldBe("1a4d2d86");
        background.State.ShouldBe(DisplayState.Completed);
    }

    [Fact]
    public void ConvertStartedAtFromEpochMilliseconds()
    {
        Session background = Sessions.Single(session => session.Name == "spike-commit-check");

        background.StartedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1792745753340));
    }

    [Fact]
    public void MapAWaitingRowToNeedsInput()
    {
        Session waiting = Sessions.Single(session => session.Name == "spike-permission-2");

        waiting.State.ShouldBe(DisplayState.NeedsInput);
    }

    [Fact]
    public void MapARowWithoutAProcessToStopped()
    {
        Session stopped = Sessions.Single(session => session.Name == "spike-permission");

        stopped.State.ShouldBe(DisplayState.Stopped);
    }

    [Fact]
    public void ReturnNothingForAnEmptyArray()
    {
        ClaudeAgentsListing.Parse("[]").ShouldBeEmpty();
    }

    [Fact]
    public void RejectTextThatIsNotJson()
    {
        Should.Throw<JsonException>(() => ClaudeAgentsListing.Parse("Workspace not trusted."));
    }
}