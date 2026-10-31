using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// The update key, from either view. Its flow asks the update check what is known: with a newer release it asks
/// before raising <see cref="StartUpdate"/>, otherwise the status line says why there is nothing to do.
/// </summary>
public sealed record UpdateVantage : ViewEffect, IDialogEffect
{
    /// <summary>Ask about the newest release when an update is available; otherwise refuse with the status message that says why there is nothing to update to.</summary>
    public static UpdateDecision Decide(UpdateStatus status)
    {
        return status switch
        {
            UpdateStatus.Available available => new UpdateDecision.Ask(available.Newest),
            UpdateStatus.UpToDate => new UpdateDecision.Refuse(new StatusMessage.UpToDate()),
            UpdateStatus.Off => new UpdateDecision.Refuse(new StatusMessage.UpdateCheckOff()),
            _ => throw new UnreachableException(),
        };
    }
}

/// <summary>What the update key does with what the update check knows.</summary>
public abstract record UpdateDecision
{
    /// <summary>Ask whether to update to this release.</summary>
    public sealed record Ask(ReleaseVersion Newest) : UpdateDecision;

    /// <summary>Nothing to update to: show the message instead.</summary>
    public sealed record Refuse(StatusMessage Message) : UpdateDecision;
}

/// <summary>Open the tab that waits for this Vantage to exit, updates the tool and starts it again; raised once the question is answered yes.</summary>
public sealed record StartUpdate(ReleaseVersion Newest) : ViewEffect;

/// <summary>The update tab is open, so this Vantage quits and lets the update replace its files.</summary>
public sealed record UpdateTabOpened : ViewInput;