namespace Source191.Vantage.Domain;

/// <summary>Why a row shows no pull request.</summary>
public enum PullRequestGap
{
    NoBranch,
    NoRemote,
    UnknownHost,
    CliMissing,
    NotSignedIn,
    Looking,
    Failed,
    NoPullRequest,

    /// <summary>The branch name holds a character a batch file cannot be given, so az.cmd was never started for it.</summary>
    BranchNotGivenToAz,
}