using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

/// <summary>The rows and canned git answers the worktree handler tests share.</summary>
internal static class WorktreeTestData
{
    public const string Root = @"C:\Users\me\source\repos";
    public const string Repository = @"C:\Users\me\source\repos\Northwind.Orders";
    public const string Worktree = @"C:\Users\me\source\repos\Northwind.Orders-fix-123";

    public static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Every git question answered as for a merged branch with origin/main as the remote default.</summary>
    public static FakeProcessRunner Processes()
    {
        return new FakeProcessRunner()
            .AnswerAll(new ProcessResult(0, string.Empty, string.Empty))
            .Answer(request => request.Arguments.Contains("symbolic-ref"), new ProcessResult(0, "origin/main\n", string.Empty))
            .Answer(request => request.Arguments.Contains("merge-base"), new ProcessResult(0, string.Empty, string.Empty));
    }

    public static IEnumerable<ProcessRequest> GitCalls(FakeProcessRunner processes)
    {
        return processes.CapturedRequests.Where(request => request.FileName == "git");
    }

    public static WorktreeRow Row(WorktreeHealthKind health, string path = Worktree, string? branch = "fix-123", bool ownerExists = true)
    {
        return new WorktreeRow(new FolderPath(Repository), new FolderPath(path), branch is null ? null : new BranchName(branch), new WorktreeHealth(health, string.Empty), 0, 0, Noon, null, null, 0, ownerExists, FolderExists: true);
    }
}