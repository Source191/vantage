using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// The update key, from either view. Its flow asks the update check what is known: with a newer release it asks
/// before raising <see cref="StartUpdate"/>, otherwise the status line says why there is nothing to do.
/// </summary>
public sealed record UpdateVantage : ViewEffect, IDialogEffect
{
    /// <summary>The status message when there is nothing to update to; null when an update is available and the question should be asked.</summary>
    public static StatusMessage? Refusal(UpdateStatus status)
    {
        return status switch
        {
            UpdateStatus.Available => null,
            UpdateStatus.UpToDate => new StatusMessage.UpToDate(),
            _ => new StatusMessage.UpdateCheckOff(),
        };
    }
}

/// <summary>Open the tab that waits for this Vantage to exit, updates the tool and starts it again; raised once the question is answered yes.</summary>
public sealed record StartUpdate(ReleaseVersion Newest) : ViewEffect;

/// <summary>The update tab is open, so this Vantage quits and lets the update replace its files.</summary>
public sealed record UpdateTabOpened : ViewInput;