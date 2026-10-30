using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WordingOfDetailShould
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 7, 0, TimeSpan.Zero);
    private static readonly FolderPath Worktree = FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123");
    private static readonly FolderPath Repository = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders");

    [Fact]
    public void DescribeASessionAtAPermissionPrompt()
    {
        SessionRow row = Row(DisplayState.NeedsInput, processId: 4242) with { HasLiveTab = true, TabColour = new TabColour("Yellow", "#FFB900"), Memory = MemorySize.FromMegabytes(903) };
        row = row with { Session = row.Session with { WaitingFor = "permission prompt" } };
        SessionDetail detail = new(row, LastPromptedAt: null, new DetailFact<WorktreeCounts>.Known(new WorktreeCounts(3, 0, Now.AddHours(-2))), new SessionOutput.NotRead());

        Text(Wording.DetailLines(detail, Now)).ShouldBe(
        [
            "State needs input (permission prompt)",
            "Started 14:02 (5m ago)",
            "Prompted not seen since Vantage started",
            "Session 81cdb0f9-0000-4000-8000-000000000001",
            @"Folder C:\Users\me\source\worktrees\Northwind.Orders-fix-123",
            "Branch fix-123",
            "Pull request none",
            "Tab open, yellow",
            "Worktree 3 changed files · 0 unpushed commits · last commit 2h00m ago",
            "Memory 903 MB (m for the breakdown)",
        ]);
    }

    [Fact]
    public void SayASessionWasNeverSentAPrompt()
    {
        SessionRow row = Row(DisplayState.NeedsInput, processId: 4242) with { HasTranscript = false };
        SessionDetail detail = new(row, LastPromptedAt: null, new DetailFact<WorktreeCounts>.NotApplicable(), new SessionOutput.NotRead());

        Text(Wording.DetailLines(detail, Now))[0].ShouldBe("State needs input (no prompt sent yet)");
    }

    [Fact]
    public void GiveTheLastPromptAndAStoppedSessionsMemory()
    {
        SessionDetail detail = new(Row(DisplayState.Stopped, processId: null), Now.AddMinutes(-4), new DetailFact<WorktreeCounts>.Unavailable(), new SessionOutput.NoProcess());

        string[] lines = Text(Wording.DetailLines(detail, Now));

        lines.ShouldContain("Prompted 14:03 (4m ago)");
        lines.ShouldContain("Worktree unavailable");
        lines.ShouldContain("Memory nothing to measure");
    }

    [Fact]
    public void NameATerminalSessionsOwnTerminal()
    {
        SessionRow row = Row(DisplayState.Idle, processId: null) with { Session = new InteractiveSession(SessionId.From("82db48ec-63de-49db-9831-b8147075dc3a"), "repos-93", Worktree, Now, DisplayState.Idle, ProcessId: 51432) };
        SessionDetail detail = new(row, LastPromptedAt: null, new DetailFact<WorktreeCounts>.NotApplicable(), new SessionOutput.NotSupervised());

        Text(Wording.DetailLines(detail, Now)).ShouldContain("Tab its own terminal");
        Text(Wording.DetailLines(detail, Now)).ShouldContain("Worktree not in a repository");
    }

    [Fact]
    public void SayWhyThereIsNoOutput()
    {
        Wording.OutputLines(new SessionOutput.NoProcess()).ShouldBe(["no output: the session has no process"]);
        Wording.OutputLines(new SessionOutput.NotSupervised()).ShouldBe(["no output: a session outside the supervisor has none to show"]);
        Wording.OutputLines(new SessionOutput.Unavailable()).ShouldBe(["output unavailable"]);
        Wording.OutputLines(new SessionOutput.Screen(["Do you want to make this edit?"])).ShouldBe(["Do you want to make this edit?"]);
    }

    [Fact]
    public void DescribeAWorktreeWithUnpushedCommits()
    {
        UnpushedCommit[] commits = [.. Enumerable.Range(1, 5).Select(day => new UnpushedCommit($"a1b2c3{day}", Now.AddDays(-day), $"Invoice change {day}"))];
        WorktreeRow row = WorktreeRowOf(unpushed: 12, dirty: 0) with { SessionId = SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), SessionName = "invoice-pdf" };
        WorktreeDetail detail = new(
            row,
            new CreatedWorktree(Worktree, BranchName.From("fix-123"), Repository, new DateTimeOffset(2026, 10, 23, 9, 14, 0, TimeSpan.Zero)),
            new DetailFact<MergeState>.Known(new MergeState("origin/main", Merged: false)),
            new DetailFact<IReadOnlyList<UnpushedCommit>>.Known(commits),
            new DetailFact<IReadOnlyList<string>>.NotApplicable());

        Text(Wording.DetailLines(detail, Now)).ShouldBe(
        [
            "Health ok",
            "Created by Vantage, 23 Oct 09:14",
            @"Folder C:\Users\me\source\worktrees\Northwind.Orders-fix-123",
            "Branch fix-123, not merged into origin/main",
            "Pull request none",
            "Risk 0 changed files · 12 unpushed commits",
            "Session invoice-pdf (Enter goes to it)",
            "Unpushed a1b2c31  1d0h  Invoice change 1",
            " a1b2c32  2d0h  Invoice change 2",
            " a1b2c33  3d0h  Invoice change 3",
            " a1b2c34  4d0h  Invoice change 4",
            " a1b2c35  5d0h  Invoice change 5",
            " … 7 more",
        ]);
    }

    [Fact]
    public void ListChangedFilesWhenNothingIsUnpushed()
    {
        WorktreeDetail detail = new(
            WorktreeRowOf(unpushed: 0, dirty: 2),
            Created: null,
            new DetailFact<MergeState>.Unavailable(),
            new DetailFact<IReadOnlyList<UnpushedCommit>>.NotApplicable(),
            new DetailFact<IReadOnlyList<string>>.Known(["Invoice.cs", "Totals.cs"]));

        string[] lines = Text(Wording.DetailLines(detail, Now));

        lines.ShouldContain("Created not by Vantage");
        lines.ShouldContain("Branch fix-123, merge state unavailable");
        lines[^2..].ShouldBe(["Changed Invoice.cs", " Totals.cs"]);
    }

    [Fact]
    public void GiveABrokenLeftoversReasonAndUnavailableGitLines()
    {
        WorktreeRow row = WorktreeRowOf(unpushed: 0, dirty: 0) with { Health = new WorktreeHealth(WorktreeHealthKind.Broken, new HealthReason.FolderGone()), Branch = null, FolderExists = false };
        WorktreeDetail detail = new(row, Created: null, new DetailFact<MergeState>.NotApplicable(), new DetailFact<IReadOnlyList<UnpushedCommit>>.Unavailable(), new DetailFact<IReadOnlyList<string>>.Unavailable());

        string[] lines = Text(Wording.DetailLines(detail, Now));

        lines[0].ShouldBe($"Health broken: {Wording.Describe(new HealthReason.FolderGone())}");
        lines.ShouldContain("Branch none");
        lines[^1].ShouldBe("Git unavailable");
    }

    private static string[] Text(IReadOnlyList<DetailLine> lines) => [.. lines.Select(line => $"{line.Label} {line.Text}")];

    private static SessionRow Row(DisplayState state, int? processId)
    {
        BackgroundSession session = new(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), "fix-123", Worktree, Now.AddMinutes(-5), state, processId);

        return new SessionRow(session, "Northwind.Orders", BranchName.From("fix-123"), HasLiveTab: false, HasTranscript: true) { Checkout = Repository };
    }

    private static WorktreeRow WorktreeRowOf(int unpushed, int dirty) =>
        new(Repository, Worktree, BranchName.From("fix-123"), new WorktreeHealth(WorktreeHealthKind.Ok, new HealthReason.None()), dirty, unpushed, Now, SessionId: null, SessionName: null, LeftoverFileCount: 0, OwnerExists: true, FolderExists: true);
}