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
    private static readonly TimeZoneInfo UnitedKingdom = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
    private static readonly TimeZoneInfo Nairobi = TimeZoneInfo.FindSystemTimeZoneById("E. Africa Standard Time");

    [Fact]
    public void DescribeASessionAtAPermissionPrompt()
    {
        SessionRow row = Row(DisplayState.NeedsInput, process: new JobProcess.Running(4242)) with { Tab = new LiveTab.Open(new TabColour("Yellow", "#FFB900")), Memory = new SessionMemory.Measured(MemorySize.FromMegabytes(903)) };
        row = row with { Session = row.Session with { WaitingFor = new WaitingReason.Given("permission prompt") } };
        SessionDetail detail = new(row, new LastPrompt.NotSeen(), new DetailFact<WorktreeCounts>.Known(new WorktreeCounts(3, 0, new LastCommit.Made(Now.AddHours(-2)))));

        DetailContent content = Wording.Detail(detail, Now, TimeZoneInfo.Utc);

        content.Title.ShouldBe("fix-123");
        Rows(content).ShouldBe(
        [
            ["Waiting permission prompt", "Started 14:02 (5m ago)", "Prompted not seen since Vantage started"],
            [@"Folder C:\Users\me\source\worktrees\Northwind.Orders-fix-123", "Session 81cdb0f9-0000-4000-8000-000000000001"],
            ["Worktree fix-123", "Branch fix-123", "Tab open, yellow"],
            ["Changes 3 changed files · 0 unpushed commits · last commit 2h00m ago", "Memory 903 MB (m for the breakdown)"],
        ]);
        content.Facts.SelectMany(line => line).Where(line => line.Span == 2).Select(line => line.Label).ShouldBe(["Folder", "Changes"]);
        content.Facts[0][0].Tone.ShouldBe(TextTone.Amber);
        content.Below.ShouldBeEmpty();
    }

    [Fact]
    public void SayASessionWaitsForAFirstPrompt()
    {
        SessionRow row = Row(DisplayState.NeedsInput, process: new JobProcess.Running(4242)) with { HasTranscript = false };

        DetailLine waiting = Wording.Detail(Detail(row), Now, TimeZoneInfo.Utc).Facts[0][0];

        (waiting.Text, waiting.Tone).ShouldBe(("a first prompt", TextTone.Amber));
    }

    [Fact]
    public void SayAWorkingSessionWaitsForNothing()
    {
        DetailLine waiting = Wording.Detail(Detail(Row(DisplayState.Working, process: new JobProcess.Running(4242))), Now, TimeZoneInfo.Utc).Facts[0][0];

        (waiting.Text, waiting.Tone).ShouldBe(("nothing", TextTone.Plain));
    }

    [Fact]
    public void NameTheMainCheckoutAndAFolderInsideIt()
    {
        SessionRow main = Row(DisplayState.Idle, process: new JobProcess.Running(4242), folder: Repository);
        SessionRow inside = Row(DisplayState.Idle, process: new JobProcess.Running(4242), folder: Repository.Combine(@"src\Api"));

        Text(Wording.Detail(Detail(main), Now, TimeZoneInfo.Utc)).ShouldContain("Worktree main checkout");
        Text(Wording.Detail(Detail(inside), Now, TimeZoneInfo.Utc)).ShouldContain(@"Worktree src\Api");
    }

    [Fact]
    public void NameAWorktreeNotNamedAfterItsRepository()
    {
        SessionRow spike = Row(DisplayState.Idle, process: new JobProcess.Running(4242), folder: FolderPath.From(@"C:\Users\me\source\worktrees\spike"));

        Text(Wording.Detail(Detail(spike), Now, TimeZoneInfo.Utc)).ShouldContain("Worktree spike");
    }

    [Fact]
    public void GiveTheLastPromptAndAStoppedSessionsMemory()
    {
        SessionDetail detail = new(Row(DisplayState.Stopped, process: new JobProcess.None()), new LastPrompt.Seen(Now.AddMinutes(-4)), new DetailFact<WorktreeCounts>.Unavailable());

        string[] lines = Text(Wording.Detail(detail, Now, TimeZoneInfo.Utc));

        lines.ShouldContain("Prompted 14:03 (4m ago)");
        lines.ShouldContain("Changes unavailable");
        lines.ShouldContain("Memory nothing to measure");
    }

    [Fact]
    public void NameATerminalSessionsOwnTerminal()
    {
        SessionRow row = Row(DisplayState.Idle, process: new JobProcess.None()) with { Session = new InteractiveSession(SessionId.From("82db48ec-63de-49db-9831-b8147075dc3a"), "repos-93", Worktree, Now, DisplayState.Idle, ProcessId: 51432), Location = new SessionLocation.Outside("Northwind.Orders") };

        string[] lines = Text(Wording.Detail(Detail(row), Now, TimeZoneInfo.Utc));

        lines.ShouldContain("Tab its own terminal");
        lines.ShouldContain("Worktree not in a repository");
        lines.ShouldContain("Changes not in a repository");
    }

    [Fact]
    public void DescribeAWorktreeWithUnpushedCommits()
    {
        UnpushedCommit[] commits = [.. Enumerable.Range(1, 5).Select(day => new UnpushedCommit($"a1b2c3{day}", Now.AddDays(-day), $"Invoice change {day}"))];
        WorktreeRow row = WorktreeRowOf(unpushed: 12, dirty: 0);
        WorktreeDetail detail = new(
            row,
            new WorktreeOrigin.CreatedByVantage(new CreatedWorktree(Worktree, BranchName.From("fix-123"), Repository, new DateTimeOffset(2026, 10, 23, 9, 14, 0, TimeSpan.Zero))),
            new DetailFact<MergeState>.Known(new MergeState("origin/main", Merged: false)),
            new DetailFact<IReadOnlyList<UnpushedCommit>>.Known(commits),
            new DetailFact<IReadOnlyList<string>>.NotApplicable());

        DetailContent content = Wording.Detail(detail, Now, TimeZoneInfo.Utc);

        content.Title.ShouldBe("Northwind.Orders-fix-123");
        Rows(content).ShouldBe(
        [
            ["Health ok"],
            ["Branch fix-123, not merged into origin/main", "PR none"],
            [@"Folder C:\Users\me\source\worktrees\Northwind.Orders-fix-123", "Created by Vantage, 23 Oct 09:14"],
            ["Changes 0 changed files · 12 unpushed commits"],
        ]);
        content.Facts.SelectMany(line => line).Select(line => line.Span).ShouldBe([3, 2, 1, 2, 1, 3]);
        content.Facts[3][0].Tone.ShouldBe(TextTone.Amber);
        content.Below.Select(line => $"{line.Label} {line.Text}").ShouldBe(
        [
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
            new WorktreeOrigin.NotByVantage(),
            new DetailFact<MergeState>.Unavailable(),
            new DetailFact<IReadOnlyList<UnpushedCommit>>.NotApplicable(),
            new DetailFact<IReadOnlyList<string>>.Known(["Invoice.cs", "Totals.cs"]));

        string[] lines = Text(Wording.Detail(detail, Now, TimeZoneInfo.Utc));

        lines.ShouldContain("Created not by Vantage");
        lines.ShouldContain("Branch fix-123, merge state unavailable");
        lines[^2..].ShouldBe(["Changed Invoice.cs", " Totals.cs"]);
    }

    [Fact]
    public void GiveABrokenLeftoversReasonAndUnavailableGitLines()
    {
        WorktreeRow row = WorktreeRowOf(unpushed: 0, dirty: 0) with { Health = new WorktreeHealth(WorktreeHealthKind.Broken, new HealthReason.FolderGone()), Head = new Head.Unknown(), FolderExists = false };
        WorktreeDetail detail = new(row, new WorktreeOrigin.NotByVantage(), new DetailFact<MergeState>.NotApplicable(), new DetailFact<IReadOnlyList<UnpushedCommit>>.Unavailable(), new DetailFact<IReadOnlyList<string>>.Unavailable());

        DetailContent content = Wording.Detail(detail, Now, TimeZoneInfo.Utc);
        string[] lines = Text(content);

        lines[0].ShouldBe($"Health broken: {Wording.Describe(new HealthReason.FolderGone())}");
        content.Facts[0][0].Tone.ShouldBe(TextTone.Red);
        lines.ShouldContain("Branch none");
        lines[^1].ShouldBe("Git unavailable");
    }

    [Fact]
    public void SayNoBranchAndNoTabForASessionOnNoBranch()
    {
        SessionRow detached = Row(DisplayState.Idle, process: new JobProcess.Running(4242)) with { Head = new Head.Detached() };
        SessionRow unknown = Row(DisplayState.Idle, process: new JobProcess.Running(4242)) with { Head = new Head.Unknown() };

        Rows(Wording.Detail(Detail(detached), Now, TimeZoneInfo.Utc))[2].ShouldBe(["Worktree fix-123", "Branch none", "Tab none"]);
        Rows(Wording.Detail(Detail(unknown), Now, TimeZoneInfo.Utc))[2].ShouldBe(["Worktree fix-123", "Branch none", "Tab none"]);
    }

    [Fact]
    public void SayNoBranchForAWorktreeOnNoBranch()
    {
        foreach (Head head in new Head[] { new Head.Detached(), new Head.Unknown() })
        {
            WorktreeRow row = WorktreeRowOf(unpushed: 0, dirty: 0) with { Head = head };
            WorktreeDetail detail = new(row, new WorktreeOrigin.NotByVantage(), new DetailFact<MergeState>.NotApplicable(), new DetailFact<IReadOnlyList<UnpushedCommit>>.NotApplicable(), new DetailFact<IReadOnlyList<string>>.NotApplicable());

            Rows(Wording.Detail(detail, Now, TimeZoneInfo.Utc))[1].ShouldBe(["Branch none", "PR none"]);
        }
    }

    [Fact]
    public void KeepEveryDetailToItsFactRows()
    {
        WorktreeDetail worktree = new(WorktreeRowOf(unpushed: 0, dirty: 0), new WorktreeOrigin.NotByVantage(), new DetailFact<MergeState>.NotApplicable(), new DetailFact<IReadOnlyList<UnpushedCommit>>.NotApplicable(), new DetailFact<IReadOnlyList<string>>.NotApplicable());

        Wording.Detail(Detail(Row(DisplayState.Idle, process: new JobProcess.None())), Now, TimeZoneInfo.Utc).Facts.Count.ShouldBe(Wording.DetailFactRows);
        Wording.Detail(worktree, Now, TimeZoneInfo.Utc).Facts.Count.ShouldBe(Wording.DetailFactRows);
    }

    [Fact]
    public void ShowASummerStartInUnitedKingdomTime()
    {
        SessionRow row = Row(DisplayState.Working, process: new JobProcess.Running(4242));
        row = row with { Session = row.Session with { StartedAt = new DateTimeOffset(2026, 7, 15, 13, 32, 0, TimeSpan.Zero) } };

        DetailContent content = Wording.Detail(Detail(row), new DateTimeOffset(2026, 7, 15, 13, 37, 0, TimeSpan.Zero), UnitedKingdom);

        Text(content).ShouldContain("Started 14:32 (5m ago)");
    }

    [Fact]
    public void ShowATimeFromBeforeTheClocksWentBackInItsOwnOffset()
    {
        WorktreeDetail detail = new(
            WorktreeRowOf(unpushed: 0, dirty: 0),
            new WorktreeOrigin.CreatedByVantage(new CreatedWorktree(Worktree, BranchName.From("fix-123"), Repository, new DateTimeOffset(2026, 10, 23, 8, 14, 0, TimeSpan.Zero))),
            new DetailFact<MergeState>.NotApplicable(),
            new DetailFact<IReadOnlyList<UnpushedCommit>>.NotApplicable(),
            new DetailFact<IReadOnlyList<string>>.NotApplicable());

        DetailContent content = Wording.Detail(detail, new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero), UnitedKingdom);

        Text(content).ShouldContain("Created by Vantage, 23 Oct 09:14");
    }

    [Fact]
    public void CountTodayByTheLocalDate()
    {
        SessionRow row = Row(DisplayState.Working, process: new JobProcess.Running(4242));
        row = row with { Session = row.Session with { StartedAt = new DateTimeOffset(2026, 11, 1, 21, 30, 0, TimeSpan.Zero) } };

        DetailContent content = Wording.Detail(Detail(row), new DateTimeOffset(2026, 11, 2, 0, 30, 0, TimeSpan.Zero), Nairobi);

        Text(content).ShouldContain("Started 00:30 (3h00m ago)");
    }

    private static SessionDetail Detail(SessionRow row) => new(row, new LastPrompt.NotSeen(), new DetailFact<WorktreeCounts>.NotApplicable());

    private static string[] Text(DetailContent content) => [.. content.Facts.SelectMany(line => line).Concat(content.Below).Select(line => $"{line.Label} {line.Text}")];

    private static string[][] Rows(DetailContent content) => [.. content.Facts.Select(row => row.Select(line => $"{line.Label} {line.Text}").ToArray())];

    private static SessionRow Row(DisplayState state, JobProcess process, FolderPath? folder = null)
    {
        BackgroundSession session = new(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), "fix-123", folder ?? Worktree, Now.AddMinutes(-5), state, process);

        return new SessionRow(session, new SessionLocation.InRepository("Northwind.Orders", Repository), new Head.OnBranch(BranchName.From("fix-123")), new LiveTab.None(), HasTranscript: true);
    }

    private static WorktreeRow WorktreeRowOf(int unpushed, int dirty) =>
        new(Repository, Worktree, new Head.OnBranch(BranchName.From("fix-123")), new WorktreeHealth(WorktreeHealthKind.Ok, new HealthReason.None()), dirty, unpushed, new LastCommit.Made(Now), new WorktreeSession.None(), LeftoverFileCount: 0, OwnerExists: true, FolderExists: true);
}