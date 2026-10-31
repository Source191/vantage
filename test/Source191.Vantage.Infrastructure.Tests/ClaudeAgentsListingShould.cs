using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
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

    private static readonly IReadOnlyList<Session> Sessions = ClaudeAgentsListing.Parse(Capture).Sessions;

    [Fact]
    public void ParseEveryRow()
    {
        Sessions.Count.ShouldBe(4);
    }

    [Fact]
    public void SkipARowWithoutASessionIdOrAFullWorkingDirectoryRatherThanFailTheListing()
    {
        const string json = """
            [
              {"pid":51432,"kind":"interactive","startedAt":1792591583841,"sessionId":"82db48ec-63de-49db-9831-b8147075dc3a","name":"no-cwd","status":"idle"},
              {"pid":51433,"cwd":"repos","kind":"interactive","startedAt":1792591583841,"sessionId":"92db48ec-63de-49db-9831-b8147075dc3a","name":"relative-cwd","status":"idle"},
              {"pid":51434,"cwd":"C:\\Users\\me\\source\\repos\\Northwind.Orders","kind":"interactive","startedAt":1792591583841,"sessionId":"a2db48ec-63de-49db-9831-b8147075dc3a","name":"full-cwd","status":"idle"},
              {"pid":51435,"cwd":"C:\\Users\\me\\source\\repos\\Northwind.Orders","kind":"interactive","startedAt":1792591583841,"name":"no-session-id","status":"idle"}
            ]
            """;

        ClaudeAgentsListing.Parse(json).Sessions.Select(session => session.Name).ShouldBe(["full-cwd"]);
    }

    [Fact]
    public void MapBackgroundRowsAndInteractiveRows()
    {
        InteractiveSession interactive = Sessions.Single(session => session.Name == "repos-93").ShouldBeOfType<InteractiveSession>();
        BackgroundSession background = Sessions.Single(session => session.Name == "spike-commit-check").ShouldBeOfType<BackgroundSession>();

        interactive.ProcessId.ShouldBe(51432);
        interactive.SessionId.Value.ShouldBe("82db48ec-63de-49db-9831-b8147075dc3a");
        interactive.WorkingDirectory.Value.ShouldBe(@"C:\Users\me\source\repos");
        interactive.State.ShouldBe(DisplayState.Idle);
        background.JobId.ShouldBe(JobId.From("1a4d2d86"));
        background.ProcessId.ShouldBe(57456);
        background.State.ShouldBe(DisplayState.Completed);
        Sessions.Single(session => session.Name == "spike-permission").ShouldBeOfType<BackgroundSession>().ProcessId.ShouldBeNull("a stopped job has no process");
    }

    /// <summary>
    /// Not seen from the CLI: every row has a job id or a process. Should one arrive, it reads as a stopped background job
    /// whose id is its session id's first eight characters, as the CLI derives it; when those are not hex the row is skipped.
    /// </summary>
    [Fact]
    public void MapARowWithNeitherPidNorIdToAStoppedBackgroundSession()
    {
        const string json = """
            [
              {"cwd":"C:\\Users\\me\\source\\repos\\Northwind.Orders","kind":"background","startedAt":1,"sessionId":"fa82d736-d876-4840-b6c2-d62a6767e1c1","name":"no-job"},
              {"cwd":"C:\\Users\\me\\source\\repos\\Northwind.Orders","kind":"interactive","startedAt":1,"sessionId":"b2db48ec-63de-49db-9831-b8147075dc3a","name":"interactive-ended"},
              {"cwd":"C:\\Users\\me\\source\\repos\\Northwind.Orders","kind":"background","startedAt":1,"sessionId":"not-hex-at-all","name":"underivable"}
            ]
            """;

        IReadOnlyList<Session> sessions = ClaudeAgentsListing.Parse(json).Sessions;

        sessions.Select(session => session.Name).ShouldBe(["no-job", "interactive-ended"]);
        sessions[0].ShouldBeOfType<BackgroundSession>().JobId.ShouldBe(JobId.From("fa82d736"));
        sessions[1].ShouldBeOfType<BackgroundSession>().JobId.ShouldBe(JobId.From("b2db48ec"), "an interactive row without a process cannot be its own terminal");
        sessions.ShouldAllBe(session => session.State == DisplayState.Stopped);
    }

    /// <summary>
    /// Captured 2026-10-30 (Claude Code 2.1.314): a plugin's background review through the Agent SDK, with no name, and
    /// <c>claude -p</c>, with a derived one, are both listed as interactive with no status for as long as they run.
    /// </summary>
    [Fact]
    public void ListAnInteractiveRowWithAProcessAndNoStatusAsAnAutomatedRun()
    {
        const string json = """
            [
              {"pid":30532,"id":"ed9c604e","cwd":"C:\\Users\\me\\source\\worktrees\\Northwind.Orders-fix-123","kind":"background","startedAt":1793352255092,"sessionId":"ed9c604e-5c03-49f6-bd67-c0471a9f3207","name":"fix-123","status":"busy","state":"working"},
              {"pid":60672,"cwd":"C:\\Users\\me\\source\\worktrees\\Northwind.Orders-fix-123","kind":"interactive","startedAt":1793380027100,"sessionId":"0fe9bf3e-385c-452d-a37b-7da761157fac"},
              {"pid":56712,"cwd":"C:\\Users\\me\\source\\repos\\Northwind.Orders","kind":"interactive","startedAt":1793380519221,"sessionId":"788e2abb-6dde-41c5-84e9-890d3a367799","name":"Northwind-9d"}
            ]
            """;

        SessionListing listing = ClaudeAgentsListing.Parse(json);

        listing.Sessions.Select(session => session.Name).ShouldBe(["fix-123"]);
        listing.AutomatedRuns.ShouldBe([
            new AutomatedRun(SessionId.From("0fe9bf3e-385c-452d-a37b-7da761157fac"), FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123"), 60672),
            new AutomatedRun(SessionId.From("788e2abb-6dde-41c5-84e9-890d3a367799"), FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), 56712),
        ]);
    }

    [Fact]
    public void KeepAnInteractiveRowWithAStatusAsAnInteractiveSession()
    {
        SessionListing listing = ClaudeAgentsListing.Parse(Capture);

        listing.Sessions.Single(session => session.Name == "repos-93").ShouldBeOfType<InteractiveSession>();
        listing.AutomatedRuns.ShouldBeEmpty();
    }

    [Fact]
    public void KeepABackgroundRowWithAProcessAndNoStatusAsABackgroundSession()
    {
        const string json = """
            [
              {"pid":57456,"id":"1a4d2d86","cwd":"C:\\Users\\me\\source\\repos\\Northwind.Orders","kind":"background","startedAt":1,"sessionId":"1a4d2d86-8e87-44fe-a9f7-61d47d481ae3","name":"starting"}
            ]
            """;

        SessionListing listing = ClaudeAgentsListing.Parse(json);

        listing.Sessions.Single().ShouldBeOfType<BackgroundSession>().State.Kind.ShouldBe(DisplayStateKind.Unknown);
        listing.AutomatedRuns.ShouldBeEmpty();
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
        SessionListing listing = ClaudeAgentsListing.Parse("[]");

        listing.Sessions.ShouldBeEmpty();
        listing.AutomatedRuns.ShouldBeEmpty();
    }

    [Fact]
    public void KeepTheWaitingReason()
    {
        IReadOnlyList<Session> sessions = ClaudeAgentsListing.Parse(Capture).Sessions;

        sessions.Single(session => session.Name == "spike-permission-2").WaitingFor.ShouldBe("permission prompt");
        sessions.Single(session => session.Name == "spike-commit-check").WaitingFor.ShouldBeNull();
    }

    [Fact]
    public void RejectTextThatIsNotJson()
    {
        Should.Throw<JsonException>(() => ClaudeAgentsListing.Parse("Workspace not trusted."));
    }
}