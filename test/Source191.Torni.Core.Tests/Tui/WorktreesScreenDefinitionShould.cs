using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Adapters.Git;
using Source191.Torni.Application;
using Source191.Torni.Application.Detail;
using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;
using Source191.Torni.Tui;

namespace Source191.Torni.Core.Tests.Tui;

public sealed class WorktreesScreenDefinitionShould
{
    private static readonly FolderPath Repository = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Billing");
    private static readonly FolderPath Worktree = FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf");

    private readonly WorktreesScreenDefinition definition;

    public WorktreesScreenDefinitionShould()
    {
        FakeTimeProvider clock = new(Fixtures.Noon);
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
        DetailLoader loader = new(new GitCli(processes, "git"), new InMemoryState(), new SessionActivity(clock), new DiagnosticLog(new FakeTimeProvider(Fixtures.Noon), []));
        FolderLabels folders = new(TestSettings.Current(TestSettings.For([@"C:\Users\me\source\repos"], @"D:\work")), FolderPath.From(@"C:\Users\me"));
        definition = new WorktreesScreenDefinition(folders, new WorktreeDetails(loader, clock));
    }

    [Fact]
    public void ListItsColumnsAsWordingDoesWithWorktreeElastic()
    {
        definition.Columns.Select(column => column.Help).ShouldBe(Wording.WorktreesColumns);
        definition.Columns.Single(column => column.Fit is ColumnFit.Elastic).Help.ShouldBeSameAs(Wording.WorktreeColumn);
    }

    [Fact]
    public void RightAlignLastCommit()
    {
        TableColumn<WorktreeRow> lastCommit = definition.Columns.Single(column => column.Help == Wording.LastCommitColumn);

        lastCommit.Fit.ShouldBe(new ColumnFit.RightAligned());
        lastCommit.Cell(Row(new LastCommit.Made(Fixtures.Noon.AddMinutes(-5))), Fixtures.Noon, int.MaxValue).ShouldBe("5m");
        lastCommit.Cell(Row(new LastCommit.Unknown()), Fixtures.Noon, int.MaxValue).ShouldBe("-");
    }

    [Fact]
    public void ShowEveryColumnOfARow()
    {
        WorktreeRow row = Row(new LastCommit.Made(Fixtures.Noon.AddHours(-2))) with { Session = new WorktreeSession.Running(SessionId.From("81cdb0f9-0000-4000-8000-000000000001"), Listed.As("invoice-pdf")) };

        definition.Columns.Select(column => column.Cell(row, Fixtures.Noon, int.MaxValue)).ShouldBe(
        [
            @"Northwind.Billing › ~\source\worktrees\Northwind.Billing-invoice-pdf",
            Wording.HealthCell(WorktreeHealthKind.Ok),
            "2h00m",
            "invoice-pdf",
        ]);
    }

    [Fact]
    public void ShowNoSessionAsNothing()
    {
        definition.Columns.Single(column => column.Help == Wording.SessionColumn).Cell(Row(new LastCommit.Unknown()), Fixtures.Noon, int.MaxValue).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(WorktreeHealthKind.Ok)]
    [InlineData(WorktreeHealthKind.Locked)]
    [InlineData(WorktreeHealthKind.Empty)]
    [InlineData(WorktreeHealthKind.Orphan)]
    [InlineData(WorktreeHealthKind.Broken)]
    [InlineData(WorktreeHealthKind.Elsewhere)]
    public void ColourARowByItsHealth(WorktreeHealthKind kind)
    {
        WorktreeRow row = Row(new LastCommit.Unknown()) with { Health = new WorktreeHealth(kind, new HealthReason.None()) };

        definition.RowColour(row).ShouldBe(kind switch
        {
            WorktreeHealthKind.Ok => Palette.White,
            WorktreeHealthKind.Locked => Palette.Amber,
            WorktreeHealthKind.Empty => Palette.Grey,
            WorktreeHealthKind.Orphan => Palette.Red,
            WorktreeHealthKind.Broken => Palette.Red,
            WorktreeHealthKind.Elsewhere => Palette.Amber,
        });
    }

    [Fact]
    public void NameItsTab()
    {
        definition.Tab.ShouldBe(ConsoleTab.Worktrees);
    }

    [Fact]
    public void RefreshEveryFifteenSeconds()
    {
        definition.Refresh.ShouldBe(new TabRefresh.Every(TimeSpan.FromSeconds(15)));
    }

    private static WorktreeRow Row(LastCommit lastCommit) => new(Repository, Worktree, new Head.OnBranch(BranchName.From("invoice-pdf")), new WorktreeHealth(WorktreeHealthKind.Ok, new HealthReason.None()), 0, 0, lastCommit, new WorktreeSession.None(), LeftoverFileCount: 0, OwnerExists: true, FolderExists: true);
}