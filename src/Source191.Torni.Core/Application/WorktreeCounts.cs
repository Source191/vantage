namespace Source191.Torni.Application;

/// <summary>What git says about a worktree's work: changed files, commits not pushed, and when it last committed.</summary>
public sealed record WorktreeCounts(int DirtyFiles, int UnpushedCommits, LastCommit LastCommitAt);