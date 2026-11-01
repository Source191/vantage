using Source191.Vantage.Application.Effects;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Actions;

/// <summary>What starting the wizard's session did, including its tab.</summary>
public abstract record StartSessionOutcome : Outcome
{
    public sealed record StartedAndOpened(string Name, JobId JobId, OpenTabOutcome Tab) : StartSessionOutcome;

    public sealed record NotListedYet(string Name, JobId JobId) : StartSessionOutcome;

    /// <summary>A Claude tab was opened in the directory so the trust prompt can be accepted.</summary>
    public sealed record NotTrusted(FolderPath Directory) : StartSessionOutcome;

    public sealed record Failed(string Message) : StartSessionOutcome;
}