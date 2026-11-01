using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class GitWorktreeListingShould
{
    /// <summary>
    /// Captured from "git worktree list --porcelain" on 2026-10-23, with a lock reason and a prunable entry added
    /// in the <see href="https://git-scm.com/docs/git-worktree">shapes git documents</see>.
    /// </summary>
    private const string Capture = """
        worktree C:/Users/me/source/repos/Northwind.Billing
        HEAD b7b91cb0000000000000000000000000000000000
        branch refs/heads/main

        worktree C:/Users/me/tools/worktrees/Northwind.Devices-event-cbd26
        HEAD ab1044d0000000000000000000000000000000000
        branch refs/heads/release/event-contract-nested-objects
        locked

        worktree C:/Users/me/source/repos/Northwind.Billing-study
        HEAD 70bcf2c0000000000000000000000000000000000
        detached
        locked kept for the study day

        worktree C:/Users/me/source/repos/Northwind.Billing/.claude/worktrees/analyzers-fixes
        HEAD 00103b90000000000000000000000000000000000
        branch refs/heads/feature/analyzers-fixes
        prunable gitdir file points to non-existent location

        """;

    private static readonly IReadOnlyList<GitWorktreeEntry> Entries = GitWorktreeListing.Parse(Capture);

    [Fact]
    public void ParseEveryBlock()
    {
        Entries.Count.ShouldBe(4);
    }

    [Fact]
    public void MarkTheFirstEntryAsTheMainWorktree()
    {
        Entries[0].IsMain.ShouldBeTrue();
        Entries.Skip(1).ShouldAllBe(entry => !entry.IsMain);
    }

    [Fact]
    public void NormaliseForwardSlashesToWindowsPaths()
    {
        Entries[0].Path.ShouldBe(@"C:\Users\me\source\repos\Northwind.Billing");
    }

    [Fact]
    public void ReadABranchLineAsOnBranch()
    {
        Entries[1].Head.ShouldBe(new Head.OnBranch(BranchName.From("release/event-contract-nested-objects")));
        Entries[1].Commit.ShouldStartWith("ab1044d");
    }

    [Fact]
    public void ReadADetachedLineAsDetached()
    {
        Entries[2].Head.ShouldBe(new Head.Detached());
        Entries[2].Commit.ShouldStartWith("70bcf2c");
    }

    [Fact]
    public void ReadABranchItCannotNameAsUnknown()
    {
        const string listing = """
            worktree C:/Users/me/source/repos/Northwind.Billing.git
            bare

            worktree C:/Users/me/source/repos/Northwind.Billing-odd
            HEAD 1234567000000000000000000000000000000000
            branch refs/heads/odd name

            """;

        GitWorktreeListing.Parse(listing).Select(entry => entry.Head).ShouldBe([new Head.Unknown(), new Head.Unknown()]);
    }

    [Fact]
    public void ReportALockWithNoReasonAsAnEmptyReason()
    {
        Entries[1].Lock.ShouldBe(new WorktreeLock.Locked(string.Empty));
    }

    [Fact]
    public void ReportALockReasonWhenGitGivesOne()
    {
        Entries[2].Lock.ShouldBe(new WorktreeLock.Locked("kept for the study day"));
    }

    [Fact]
    public void ReportAWorktreeWithNoLockLineAsUnlocked()
    {
        Entries[0].Lock.ShouldBe(new WorktreeLock.Unlocked());
    }

    [Fact]
    public void MarkAWorktreeWithAPrunableLineAsPrunable()
    {
        Entries[3].IsPrunable.ShouldBeTrue();
        Entries[0].IsPrunable.ShouldBeFalse();
    }

    [Fact]
    public void ReturnNothingForEmptyOutput()
    {
        GitWorktreeListing.Parse(string.Empty).ShouldBeEmpty();
    }
}