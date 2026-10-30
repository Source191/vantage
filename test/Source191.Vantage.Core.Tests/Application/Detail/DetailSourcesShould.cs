using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Detail;

public sealed class DetailSourcesShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly FolderPath Worktree = FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf");
    private static readonly FolderPath Repository = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Billing");

    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
    private readonly FakeTimeProvider clock = new(Noon);

    [Fact]
    public async Task WordASessionsDetailKeyedByItsId()
    {
        SessionDetails source = new(Loader(), clock);
        BackgroundSession session = new(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), "invoice-pdf", Worktree, Noon.AddMinutes(-5), DisplayState.Stopped, ProcessId: null);
        SessionRow row = new(session, "Northwind.Billing", BranchName.From("invoice-pdf"), HasLiveTab: false, HasTranscript: true);

        DetailContent content = await source.ReadAsync(row, rows: 5, readScreen: true, CancellationToken.None);

        source.ReadsScreen.ShouldBeTrue();
        source.KeyOf(row).ShouldBe(session.SessionId);
        content.Title.ShouldBe("invoice-pdf");
        content.Facts[0][1].Text.ShouldBe("11:55 (5m ago)");
        content.Below.ShouldBe([new DetailLine(string.Empty, "no output: the session has no process", TextTone.Grey)]);
    }

    [Fact]
    public async Task WordAWorktreesDetailKeyedByItsFolder()
    {
        WorktreeDetails source = new(Loader(), clock);
        WorktreeRow row = new(Repository, Worktree, BranchName.From("invoice-pdf"), new WorktreeHealth(WorktreeHealthKind.Ok, new HealthReason.None()), 0, 0, Noon, SessionId: null, SessionName: null, LeftoverFileCount: 0, OwnerExists: false, FolderExists: true);

        DetailContent content = await source.ReadAsync(row, rows: 5, readScreen: false, CancellationToken.None);

        source.ReadsScreen.ShouldBeFalse();
        source.KeyOf(row).ShouldBe(Worktree);
        content.Title.ShouldBe("Northwind.Billing-invoice-pdf");
        content.Facts[0][0].Text.ShouldBe("ok");
    }

    [Fact]
    public async Task ListOnlyWhatFitsUnderTheFactsWithARowForHowManyMore()
    {
        WorktreeDetails source = new(Loader(), clock);
        WorktreeRow row = new(Repository, Worktree, BranchName.From("invoice-pdf"), new WorktreeHealth(WorktreeHealthKind.Ok, new HealthReason.None()), 0, 12, Noon, SessionId: null, SessionName: null, LeftoverFileCount: 0, OwnerExists: false, FolderExists: true);

        await source.ReadAsync(row, rows: 9, readScreen: false, CancellationToken.None);

        int limit = Wording.DetailFactRows + 1;
        processes.CapturedRequests.ShouldContain(request => request.Arguments.Count > 2 && request.Arguments[0] == "log" && request.Arguments[2] == $"{9 - limit}");
    }

    [Fact]
    public void KeepEveryDetailToItsFactRows()
    {
        BackgroundSession session = new(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), "invoice-pdf", Worktree, Noon, DisplayState.Idle, ProcessId: null);
        SessionDetail sessionDetail = new(new SessionRow(session, "Northwind.Billing", null, HasLiveTab: false, HasTranscript: true), null, new DetailFact<WorktreeCounts>.NotApplicable(), new SessionOutput.NoProcess());
        WorktreeRow row = new(Repository, Worktree, null, new WorktreeHealth(WorktreeHealthKind.Ok, new HealthReason.None()), 0, 0, Noon, SessionId: null, SessionName: null, LeftoverFileCount: 0, OwnerExists: false, FolderExists: true);
        WorktreeDetail worktreeDetail = new(row, null, new DetailFact<MergeState>.NotApplicable(), new DetailFact<IReadOnlyList<UnpushedCommit>>.NotApplicable(), new DetailFact<IReadOnlyList<string>>.NotApplicable());

        Wording.Detail(sessionDetail, Noon).Facts.Count.ShouldBe(Wording.DetailFactRows);
        Wording.Detail(worktreeDetail, Noon).Facts.Count.ShouldBe(Wording.DetailFactRows);
    }

    private DetailLoader Loader() => new(new ClaudeCli(processes, @"C:\Users\me\.local\bin\claude.exe"), new GitCli(processes, "git"), new InMemoryState(), new SessionActivity(clock));
}