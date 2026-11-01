using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// The update key, from any tab. Its flow asks the update check what is known: with a newer release it shows what
/// changed and offers <see cref="StartUpdate"/>, otherwise the status line says why there is nothing to do.
/// </summary>
public sealed record UpdateVantage : ViewEffect, IDialogEffect
{
    /// <summary>Show the newest release's notes since the running version when an update is available; otherwise refuse with the status message that says why there is nothing to update to.</summary>
    public static UpdateDecision Decide(UpdateStatus status, ReleaseNotes notes)
    {
        ArgumentNullException.ThrowIfNull(notes);

        return status switch
        {
            UpdateStatus.Available available => new UpdateDecision.Ask(available.Current, available.Newest, notes.Between(available.Current, available.Newest)),
            UpdateStatus.UpToDate => new UpdateDecision.Refuse(new StatusMessage.UpToDate()),
            UpdateStatus.Off => new UpdateDecision.Refuse(new StatusMessage.UpdateCheckOff()),
            _ => throw new UnreachableException(),
        };
    }
}