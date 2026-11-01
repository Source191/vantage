using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>The Config tab's rows: the settings in the order the tab lists them, then the Setup heading and the setup, read or still being read.</summary>
public static class ConfigRows
{
    public static IReadOnlyList<ConfigRow> For(Settings settings, SetupFacts facts)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(facts);

        return [.. SettingRows(settings), new ConfigRow.Heading(), .. SetupRows(settings, facts)];
    }

    private static IEnumerable<ConfigRow> SettingRows(Settings settings)
    {
        return
        [
            .. settings.RepositoryRoots.Select((root, index) => new ConfigRow.RepositoryRoot(index, root)),
            new ConfigRow.WorktreeRoot(settings.WorktreeRoot),
            new ConfigRow.OpenFolderWith(settings.Editor),
            new ConfigRow.DefaultBase(settings.DefaultBase),
            new ConfigRow.DefaultPlace(settings.DefaultPlace),
            new ConfigRow.CheckForUpdates(settings.CheckForUpdates),
        ];
    }

    /// <summary>The last update check reads as off while the check is off, whatever was recorded before.</summary>
    private static IEnumerable<ConfigRow> SetupRows(Settings settings, SetupFacts facts)
    {
        return
        [
            new ConfigRow.Setup(SetupItem.SettingsFile, facts.SettingsFile),
            new ConfigRow.Setup(SetupItem.TerminalProfiles, facts.TerminalProfiles),
            new ConfigRow.Setup(SetupItem.ClaudeBell, facts.ClaudeBell),
            new ConfigRow.Setup(SetupItem.DiagnosticLog, facts.DiagnosticLog),
            new ConfigRow.Setup(SetupItem.LastUpdateCheck, settings.CheckForUpdates ? facts.LastUpdateCheck : new SetupState.Disabled()),
        ];
    }
}