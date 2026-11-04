using Source191.Torni.Application;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application.Actions;

/// <summary>The rows and canned git answers the worktree handler tests share.</summary>
internal static class WorktreeTestData
{
    public const string Root = @"C:\Users\me\source\repos";
    public const string Repository = @"C:\Users\me\source\repos\Northwind.Orders";
    public const string Worktree = @"C:\Users\me\source\repos\Northwind.Orders-fix-123";

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

    /// <summary>A row on branch fix-123 with no session running in it, unless the head or session say otherwise.</summary>
    public static WorktreeRow Row(WorktreeHealthKind health, string path = Worktree, Head? head = null, bool ownerExists = true, WorktreeSession? session = null)
    {
        return new WorktreeRow(FolderPath.From(Repository), FolderPath.From(path), head ?? new Head.OnBranch(BranchName.From("fix-123")), new WorktreeHealth(health, new HealthReason.None()), 0, 0, new LastCommit.Made(Fixtures.Noon), session ?? new WorktreeSession.None(), 0, ownerExists, FolderExists: true);
    }
}