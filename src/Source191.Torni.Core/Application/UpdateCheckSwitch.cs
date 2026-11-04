using Source191.Torni.Domain;

namespace Source191.Torni.Application;

/// <summary>
/// Whether the update check may run: the <c>checkForUpdates</c> setting, read on every call so a save applies without a
/// restart, and <see cref="UpdateCheckOptOut.VariableName"/>'s value, judged once as Torni starts.
/// </summary>
public sealed class UpdateCheckSwitch(CurrentSettings settings, string optOutVariable)
{
    private readonly bool optedOut = UpdateCheckOptOut.Applies(optOutVariable);

    public bool IsOn() => settings.Value.CheckForUpdates && !optedOut;
}