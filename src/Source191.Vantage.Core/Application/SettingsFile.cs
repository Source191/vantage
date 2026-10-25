using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// Between config.json's record and <see cref="Settings"/>: whatever the file leaves out, or holds in a form Vantage
/// cannot use, reads as its default; a saved file names every property, so it is complete and readable by hand.
/// </summary>
public static class SettingsFile
{
    public static Settings From(VantageConfig? config, FolderPath home)
    {
        IReadOnlyList<FolderPath> roots = [.. (config?.RepositoryRoots ?? []).Select(FolderPath.TryParse).OfType<FolderPath>()];

        return Settings.Resolve(roots, FolderPath.TryParse(config?.WorktreeRoot), Editor(config?.Editor), config?.DefaultBase, home);
    }

    public static VantageConfig ToConfig(Settings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        EditorChoice editor = settings.Editor;
        EditorSetting setting = editor.Preset == EditorPreset.Custom
            ? new EditorSetting(editor.Preset.ToString(), editor.Executable, editor.Arguments)
            : new EditorSetting(editor.Preset.ToString(), Executable: null, Arguments: null);

        return new VantageConfig([.. settings.RepositoryRoots.Select(root => root.Value)], settings.DefaultBase, settings.WorktreeRoot.Value, setting);
    }

    private static EditorChoice Editor(EditorSetting? setting)
    {
        EditorPreset? preset = Enum.TryParse(setting?.Preset, ignoreCase: true, out EditorPreset parsed) && Enum.IsDefined(parsed) ? parsed : null;

        return EditorChoice.FromFile(preset, setting?.Executable, setting?.Arguments);
    }
}
