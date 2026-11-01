using Microsoft.Extensions.Time.Testing;
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
    private static readonly DateTimeOffset SummerNoon = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo UnitedKingdom = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
    private static readonly FolderPath Worktree = FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf");
    private static readonly FolderPath Repository = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Billing");

    private readonly FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
    private readonly FakeTimeProvider clock = new(Noon);

    [Fact]
    public async Task WordASessionsDetailKeyedByItsIdInItsFactRows()
    {
        SessionDetails source = new(Loader(), clock);
        BackgroundSession session = new(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), "invoice-pdf", Worktree, Noon.AddMinutes(-5), DisplayState.Stopped, new JobProcess.None());
        SessionRow row = new(session, new SessionLocation.Outside("Northwind.Billing"), new Head.OnBranch(BranchName.From("invoice-pdf")), new LiveTab.None(), HasTranscript: true);

        DetailContent content = await source.ReadAsync(row, CancellationToken.None);

        source.Rows.ShouldBe(Wording.DetailFactRows);
        source.KeyOf(row).ShouldBe(session.SessionId);
        content.Title.ShouldBe("invoice-pdf");
        content.Facts[0][1].Text.ShouldBe("11:55 (5m ago)");
    }

    [Fact]
    public async Task WordAWorktreesDetailKeyedByItsFolderWithRoomForItsListing()
    {
        WorktreeDetails source = new(Loader(), clock);
        WorktreeRow row = new(Repository, Worktree, new Head.OnBranch(BranchName.From("invoice-pdf")), new WorktreeHealth(WorktreeHealthKind.Ok, new HealthReason.None()), 0, 12, Noon, new WorktreeSession.None(), LeftoverFileCount: 0, OwnerExists: false, FolderExists: true);

        DetailContent content = await source.ReadAsync(row, CancellationToken.None);

        source.Rows.ShouldBe(Wording.DetailFactRows + Wording.DetailListedItems + 1);
        source.KeyOf(row).ShouldBe(Worktree);
        content.Title.ShouldBe("Northwind.Billing-invoice-pdf");
        content.Facts[0][0].Text.ShouldBe("ok");
        processes.CapturedRequests.ShouldContain(request => request.Arguments.Count > 2 && request.Arguments[0] == "log" && request.Arguments[2] == $"{Wording.DetailListedItems}");
    }

    [Fact]
    public async Task WordASessionsStartInTheClocksLocalZone()
    {
        FakeTimeProvider summer = new(SummerNoon);
        summer.SetLocalTimeZone(UnitedKingdom);
        SessionDetails source = new(Loader(new InMemoryState(), summer), summer);
        BackgroundSession session = new(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), JobId.From("81cdb0f9"), "invoice-pdf", Worktree, SummerNoon.AddMinutes(-5), DisplayState.Stopped, new JobProcess.None());
        SessionRow row = new(session, new SessionLocation.Outside("Northwind.Billing"), new Head.OnBranch(BranchName.From("invoice-pdf")), new LiveTab.None(), HasTranscript: true);

        DetailContent content = await source.ReadAsync(row, CancellationToken.None);

        content.Facts[0][1].Text.ShouldBe("12:55 (5m ago)");
    }

    [Fact]
    public async Task WordAWorktreesCreationInTheClocksLocalZone()
    {
        FakeTimeProvider summer = new(SummerNoon);
        summer.SetLocalTimeZone(UnitedKingdom);
        InMemoryState state = new();
        await state.RecordCreatedWorktreeAsync(new CreatedWorktree(Worktree, BranchName.From("invoice-pdf"), Repository, new DateTimeOffset(2026, 7, 15, 8, 14, 0, TimeSpan.Zero)));
        WorktreeDetails source = new(Loader(state, summer), summer);
        WorktreeRow row = new(Repository, Worktree, new Head.OnBranch(BranchName.From("invoice-pdf")), new WorktreeHealth(WorktreeHealthKind.Ok, new HealthReason.None()), 0, 0, SummerNoon, new WorktreeSession.None(), LeftoverFileCount: 0, OwnerExists: true, FolderExists: true);

        DetailContent content = await source.ReadAsync(row, CancellationToken.None);

        content.Facts[2][1].Text.ShouldBe("by Vantage, 09:14");
    }

    private DetailLoader Loader() => Loader(new InMemoryState(), clock);

    private DetailLoader Loader(InMemoryState state, TimeProvider time) => new(new GitCli(processes, "git"), state, new SessionActivity(time));
}