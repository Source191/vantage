using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

/// <summary>The rows and canned git answers the session handler tests share.</summary>
internal static class SessionTestData
{
    public const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    public const string Repository = @"C:\repos\Northwind.Orders";
    public const string Worktree = @"C:\repos\Northwind.Orders-fix-123";

    public static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Every git question answered as for a clean, merged worktree of Northwind.Orders.</summary>
    public static FakeProcessRunner Processes()
    {
        return new FakeProcessRunner()
            .AnswerAll(new ProcessResult(0, string.Empty, string.Empty))
            .Answer(request => request.Arguments.Contains("--porcelain") && request.Arguments.Contains("list"), new ProcessResult(0, "worktree C:/repos/Northwind.Orders\nHEAD a\nbranch refs/heads/main\n\nworktree C:/repos/Northwind.Orders-fix-123\nHEAD b\nbranch refs/heads/fix-123\n", string.Empty))
            .Answer(request => request.Arguments.Contains("status"), new ProcessResult(0, string.Empty, string.Empty))
            .Answer(request => request.Arguments.Contains("rev-list"), new ProcessResult(0, "0\n", string.Empty))
            .Answer(request => request.Arguments.Contains("symbolic-ref"), new ProcessResult(0, "origin/main\n", string.Empty))
            .Answer(request => request.Arguments.Contains("merge-base"), new ProcessResult(0, string.Empty, string.Empty));
    }

    /// <summary>A plain claude session in some tab, session id sess-9.</summary>
    public static SessionRow Interactive(string name, int? processId, DisplayState? state = null)
    {
        Session session = new(new SessionId("sess-9"), JobId: null, name, new FolderPath(Repository), SessionKind.Interactive, Noon, state ?? DisplayState.Idle, processId);

        return new SessionRow(session, "Northwind.Orders", new BranchName("main"), IsWorktree: false, Description: null, HasLiveTab: false, HasTranscript: true);
    }

    /// <summary>A supervised session named fix-123, job 81cdb0f9, session id sess-1.</summary>
    public static SessionRow Background(string directory, bool isWorktree, DisplayState? state = null)
    {
        Session session = new(new SessionId("sess-1"), new JobId("81cdb0f9"), "fix-123", new FolderPath(directory), SessionKind.Background, Noon, state ?? DisplayState.Working);

        return new SessionRow(session, "Northwind.Orders", new BranchName(isWorktree ? "fix-123" : "main"), isWorktree, Description: null, HasLiveTab: false, HasTranscript: true);
    }
}