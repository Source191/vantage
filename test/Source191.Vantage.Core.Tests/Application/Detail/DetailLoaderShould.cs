using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Detail;

public sealed class DetailLoaderShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly FolderPath Worktree = FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf");
    private static readonly FolderPath Repository = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Billing");

    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .AnswerAll(new ProcessResult(0, string.Empty, string.Empty));

    private readonly InMemoryState state = new();
    private readonly FakeTimeProvider clock = new(Noon);

    private DetailLoader Loader(SessionActivity? activity = null) => new(new GitCli(processes, "git"), state, activity ?? new SessionActivity(clock));

    [Fact]
    public async Task FillASessionsDetail()
    {
        processes
            .Answer(request => request.Arguments.Contains("status"), new ProcessResult(0, " M a.cs\n M b.cs\n?? c.cs\n", string.Empty))
            .Answer(request => request.Arguments.Contains("rev-list"), new ProcessResult(0, "0\n", string.Empty))
            .Answer(request => request.Arguments.Contains("log"), new ProcessResult(0, "2026-10-30T11:00:00+00:00\n", string.Empty));
        SessionRow row = Row(process: new JobProcess.Running(4242));

        SessionDetail detail = await Loader().SessionAsync(row, CancellationToken.None);

        detail.Row.ShouldBe(row);
        detail.Counts.ShouldBe(new DetailFact<WorktreeCounts>.Known(new WorktreeCounts(3, 0, new LastCommit.Made(new DateTimeOffset(2026, 10, 30, 11, 0, 0, TimeSpan.Zero)))));
        detail.LastPromptedAt.ShouldBeNull();
    }

    [Fact]
    public async Task CarryTheTimeTheSessionWasLastPrompted()
    {
        SessionActivity activity = new(clock);
        SessionRow waiting = Row(process: new JobProcess.Running(4242)) with { Session = Row(process: new JobProcess.Running(4242)).Session with { State = DisplayState.NeedsInput } };
        SessionRow working = waiting with { Session = waiting.Session with { State = DisplayState.Working } };
        clock.Advance(TimeSpan.FromMinutes(3));
        activity.Observe([waiting], [working]);

        SessionDetail detail = await Loader(activity).SessionAsync(working, CancellationToken.None);

        detail.LastPromptedAt.ShouldBe(Noon.AddMinutes(3));
    }

    [Fact]
    public async Task MarkFailedGitUnavailableAndKeepTheRest()
    {
        processes
            .Answer(request => request.Arguments.Contains("status"), new ProcessResult(128, string.Empty, "fatal: not a git repository"));
        SessionRow row = Row(process: new JobProcess.Running(4242));

        SessionDetail detail = await Loader().SessionAsync(row, CancellationToken.None);

        detail.Counts.ShouldBeOfType<DetailFact<WorktreeCounts>.Unavailable>();
        detail.Row.ShouldBe(row);
    }

    [Fact]
    public async Task LeaveOutGitCountsForAFolderOutsideARepository()
    {
        SessionRow outside = Row(process: new JobProcess.None()) with { Location = new SessionLocation.Outside("Northwind.Billing") };

        SessionDetail detail = await Loader().SessionAsync(outside, CancellationToken.None);

        detail.Counts.ShouldBeOfType<DetailFact<WorktreeCounts>.NotApplicable>();
    }

    [Fact]
    public async Task FillAWorktreesDetailWithItsUnpushedCommits()
    {
        await state.RecordCreatedWorktreeAsync(new CreatedWorktree(Worktree, BranchName.From("invoice-pdf"), Repository, new DateTimeOffset(2026, 10, 23, 9, 14, 0, TimeSpan.Zero)));
        processes
            .Answer(request => request.Arguments.Contains("symbolic-ref"), new ProcessResult(0, "origin/main\n", string.Empty))
            .Answer(request => request.Arguments.Contains("merge-base"), new ProcessResult(1, string.Empty, string.Empty))
            .Answer(request => request.Arguments.Contains("log"), new ProcessResult(0, "a1b2c3d\t2026-10-30T10:00:00+00:00\tSplit the invoice totals\n", string.Empty));

        WorktreeDetail detail = await Loader().WorktreeAsync(WorktreeRowOf(unpushed: 12, dirty: 0), listRows: 4, CancellationToken.None);

        detail.Created!.CreatedAt.ShouldBe(new DateTimeOffset(2026, 10, 23, 9, 14, 0, TimeSpan.Zero));
        detail.Merge.ShouldBe(new DetailFact<MergeState>.Known(new MergeState("origin/main", Merged: false)));
        detail.Commits.ShouldBeOfType<DetailFact<IReadOnlyList<UnpushedCommit>>.Known>().Value.Single().Subject.ShouldBe("Split the invoice totals");
        detail.ChangedFiles.ShouldBeOfType<DetailFact<IReadOnlyList<string>>.NotApplicable>();
        processes.CapturedRequests.ShouldContain(request => request.Arguments.Contains("-n") && request.Arguments.Contains("4"));
    }

    [Fact]
    public async Task ListTheChangedFilesWhenNothingIsUnpushed()
    {
        processes.Answer(request => request.Arguments.Contains("status"), new ProcessResult(0, " M Invoice.cs\n?? Totals.cs\n", string.Empty));

        WorktreeDetail detail = await Loader().WorktreeAsync(WorktreeRowOf(unpushed: 0, dirty: 2), listRows: 4, CancellationToken.None);

        detail.Created.ShouldBeNull();
        detail.ChangedFiles.ShouldBe(new DetailFact<IReadOnlyList<string>>.Known(["Invoice.cs", "Totals.cs"]), new ListOfStringsComparer());
        detail.Commits.ShouldBeOfType<DetailFact<IReadOnlyList<UnpushedCommit>>.NotApplicable>();
    }

    [Fact]
    public async Task MarkTheGitLinesOfALeftoverUnavailable()
    {
        WorktreeRow leftover = WorktreeRowOf(unpushed: 0, dirty: 0) with { FolderExists = false, OwnerExists = false, Head = new Head.Unknown() };

        WorktreeDetail detail = await Loader().WorktreeAsync(leftover, listRows: 4, CancellationToken.None);

        detail.Merge.ShouldBeOfType<DetailFact<MergeState>.NotApplicable>();
        detail.Commits.ShouldBeOfType<DetailFact<IReadOnlyList<UnpushedCommit>>.Unavailable>();
        detail.ChangedFiles.ShouldBeOfType<DetailFact<IReadOnlyList<string>>.Unavailable>();
    }

    [Fact]
    public async Task AskForNoMergeStateOfAWorktreeOnADetachedHead()
    {
        WorktreeRow detached = WorktreeRowOf(unpushed: 0, dirty: 0) with { Head = new Head.Detached() };

        WorktreeDetail detail = await Loader().WorktreeAsync(detached, listRows: 4, CancellationToken.None);

        detail.Merge.ShouldBeOfType<DetailFact<MergeState>.NotApplicable>();
    }

    private static SessionRow Row(JobProcess process)
    {
        BackgroundSession session = new(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), "invoice-pdf", Worktree, Noon, DisplayState.Working, process);

        return new SessionRow(session, new SessionLocation.InRepository("Northwind.Billing", Repository), new Head.OnBranch(BranchName.From("invoice-pdf")), new LiveTab.None(), HasTranscript: true);
    }

    private static WorktreeRow WorktreeRowOf(int unpushed, int dirty) =>
        new(Repository, Worktree, new Head.OnBranch(BranchName.From("invoice-pdf")), new WorktreeHealth(WorktreeHealthKind.Ok, new HealthReason.None()), dirty, unpushed, new LastCommit.Made(Noon), new WorktreeSession.None(), LeftoverFileCount: 0, OwnerExists: true, FolderExists: true);

    /// <summary>Records compare lists by reference, so a known list is compared by its items.</summary>
    private sealed class ListOfStringsComparer : IEqualityComparer<DetailFact<IReadOnlyList<string>>>
    {
        public bool Equals(DetailFact<IReadOnlyList<string>>? x, DetailFact<IReadOnlyList<string>>? y) =>
            x is DetailFact<IReadOnlyList<string>>.Known known && y is DetailFact<IReadOnlyList<string>>.Known other && known.Value.SequenceEqual(other.Value);

        public int GetHashCode(DetailFact<IReadOnlyList<string>> obj) => 0;
    }
}