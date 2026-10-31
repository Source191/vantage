using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// The one home of the settings while Vantage runs. Every consumer reads <see cref="Value"/> when it starts a scan, a
/// wizard or a dialog, so a save from the settings dialog applies at once without a restart; a hand edit of
/// config.json applies on the next start.
/// </summary>
public sealed class CurrentSettings(Settings initial, IVantageConfig store)
{
    private readonly IVantageConfig store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly SharedSnapshot<Settings> snapshot = new(initial);

    /// <summary>The settings now: an immutable snapshot, replaced whole on a save.</summary>
    public Settings Value => snapshot.Value;

    public static async Task<CurrentSettings> LoadAsync(IVantageConfig store, FolderPath home)
    {
        ArgumentNullException.ThrowIfNull(store);

        return new CurrentSettings(SettingsFile.From(await store.ReadConfigAsync(), home), store);
    }

    /// <summary>Writes config.json, then makes the saved settings the current ones.</summary>
    public async Task SaveAsync(Settings settings)
    {
        await store.WriteConfigAsync(SettingsFile.ToConfig(settings));
        snapshot.Replace(settings);
    }
}