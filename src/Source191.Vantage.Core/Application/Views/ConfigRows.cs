using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>The Config tab's rows: the settings in the order the tab lists them, then the setup, read or still being read.</summary>
public static class ConfigRows
{
    public static IReadOnlyList<ConfigRow> For(Settings settings, SetupFacts? facts)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return [.. SettingRows(settings), .. SetupRows(settings, facts)];
    }

    private static IEnumerable<ConfigRow> SettingRows(Settings settings)
    {
        return
        [
            .. settings.RepositoryRoots.Select((root, index) => new ConfigRow.RepositoryRoot(index, root)),
            new ConfigRow.WorktreeRoot(settings.WorktreeRoot),
            new ConfigRow.OpenFolderWith(settings.Editor),
            new ConfigRow.DefaultBase(settings.DefaultBase),
            new ConfigRow.CheckForUpdates(settings.CheckForUpdates),
        ];
    }

    /// <summary>The last update check reads as off while the check is off, whatever was recorded before.</summary>
    private static IEnumerable<ConfigRow> SetupRows(Settings settings, SetupFacts? facts)
    {
        SetupState reading = new SetupState.Reading();
        SetupFacts read = facts ?? new SetupFacts(reading, reading, reading, reading, reading);

        return
        [
            new ConfigRow.Setup(SetupItem.SettingsFile, read.SettingsFile),
            new ConfigRow.Setup(SetupItem.TerminalProfiles, read.TerminalProfiles),
            new ConfigRow.Setup(SetupItem.ClaudeBell, read.ClaudeBell),
            new ConfigRow.Setup(SetupItem.DiagnosticLog, read.DiagnosticLog),
            new ConfigRow.Setup(SetupItem.LastUpdateCheck, settings.CheckForUpdates ? read.LastUpdateCheck : new SetupState.Disabled()),
        ];
    }
}