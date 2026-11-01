using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>
/// Whether the update check may run: the <c>checkForUpdates</c> setting, read on every call so a save applies without a
/// restart, and <see cref="UpdateCheckOptOut.VariableName"/>'s value, judged once as Vantage starts.
/// </summary>
public sealed class UpdateCheckSwitch(CurrentSettings settings, string optOutVariable)
{
    private readonly CurrentSettings settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly bool optedOut = UpdateCheckOptOut.Applies(optOutVariable ?? throw new ArgumentNullException(nameof(optOutVariable)));

    public bool IsOn() => settings.Value.CheckForUpdates && !optedOut;
}
