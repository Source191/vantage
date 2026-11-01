using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Launch;

/// <summary>What carrying out a launch plan came to.</summary>
public abstract record LaunchOutcome
{
    /// <summary>The session started under this job id, whether or not the supervisor lists it yet.</summary>
    public abstract record Started(JobId JobId) : LaunchOutcome;

    /// <summary>The supervisor listed the new job by the time the start returned.</summary>
    public sealed record Listed(JobId JobId, BackgroundSession Session) : Started(JobId);

    /// <summary>The supervisor had not listed the new job by the time the start returned.</summary>
    public sealed record NotListedYet(JobId JobId) : Started(JobId);

    /// <summary>The folder's repository has not been trusted interactively yet.</summary>
    public sealed record WorkspaceNotTrusted : LaunchOutcome;

    /// <summary>Git could not create the worktree, or the CLI could not start the session; the message says why.</summary>
    public sealed record Failed(string Message) : LaunchOutcome;
}