using Source191.Vantage.Adapters.Git;

namespace Source191.Vantage.UnitTests.Adapters;

public sealed class GitWorktreeListingShould
{
    /// <summary>
    /// Captured from "git worktree list --porcelain" on 2026-10-23, with a lock reason and a prunable entry added
    /// in the shapes git documents.
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
    public void StripTheRefsHeadsPrefixFromBranches()
    {
        Entries[1].Branch.ShouldBe("release/event-contract-nested-objects");
    }

    [Fact]
    public void LeaveTheBranchNullForADetachedHead()
    {
        Entries[2].Branch.ShouldBeNull();
        Entries[2].Head.ShouldStartWith("70bcf2c");
    }

    [Fact]
    public void ReportALockWithNoReasonAsAnEmptyReason()
    {
        Entries[1].LockReason.ShouldBe(string.Empty);
    }

    [Fact]
    public void ReportALockReasonWhenGitGivesOne()
    {
        Entries[2].LockReason.ShouldBe("kept for the study day");
    }

    [Fact]
    public void LeaveTheLockReasonNullWhenUnlocked()
    {
        Entries[0].LockReason.ShouldBeNull();
    }

    [Fact]
    public void ReportThePrunableReason()
    {
        Entries[3].PrunableReason.ShouldBe("gitdir file points to non-existent location");
        Entries[0].PrunableReason.ShouldBeNull();
    }

    [Fact]
    public void ReturnNothingForEmptyOutput()
    {
        GitWorktreeListing.Parse(string.Empty).ShouldBeEmpty();
    }
}