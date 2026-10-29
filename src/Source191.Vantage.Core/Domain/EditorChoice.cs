namespace Source191.Vantage.Domain;

/// <summary>The editors Vantage knows by name, and Custom for an executable the user names.</summary>
public enum EditorPreset
{
    VsCode,
    Cursor,
    Rider,
    Explorer,
    Custom,
}

/// <summary>
/// The editor that opens a folder: a preset, or a custom executable whose arguments hold <see cref="FolderPlaceholder"/>
/// where the folder goes. Arguments are a list, never a command line, so nothing re-parses a path. How a preset
/// reaches its executable is the open-in-editor key's business; this only holds the choice.
/// </summary>
public sealed record EditorChoice
{
    public const string FolderPlaceholder = "{folder}";

    private EditorChoice(EditorPreset preset, string executable, IReadOnlyList<string> arguments)
    {
        Preset = preset;
        Executable = executable;
        Arguments = arguments;
    }

    public static EditorChoice VsCode { get; } = Of(EditorPreset.VsCode);

    public EditorPreset Preset { get; }

    /// <summary>The custom editor's executable; empty for a preset.</summary>
    public string Executable { get; }

    /// <summary>The custom editor's arguments; empty for a preset.</summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <summary>A preset; Custom needs <see cref="Custom"/>, which checks its command.</summary>
    public static EditorChoice Of(EditorPreset preset)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(preset, EditorPreset.Custom);

        return new EditorChoice(preset, string.Empty, []);
    }

    /// <summary>For a custom command already checked by <see cref="Problem"/>; typed text goes through <see cref="Settings.Validate"/>.</summary>
    public static EditorChoice Custom(string executable, IReadOnlyList<string> arguments)
    {
        if (Problem(executable, arguments) is SettingsProblem problem)
        {
            throw new ArgumentException($"'{executable}' is not a usable custom editor: {problem}.", nameof(executable));
        }

        return new EditorChoice(EditorPreset.Custom, executable.Trim(), arguments);
    }

    /// <summary>The editor as config.json holds it: a missing or unusable one reads as VS Code, and a preset ignores any command beside it.</summary>
    public static EditorChoice FromFile(EditorPreset? preset, string? executable, IReadOnlyList<string>? arguments)
    {
        return preset switch
        {
            null => VsCode,
            EditorPreset.Custom when Problem(executable ?? string.Empty, arguments ?? []) is null => Custom(executable!, arguments!),
            EditorPreset.Custom => VsCode,
            EditorPreset known => Of(known),
        };
    }

    /// <summary>
    /// Why a custom command cannot be used, or null: no executable, not a full path (Windows would look for a bare name in
    /// the folder Vantage was started from), a batch file (cmd.exe would parse the folder path), or no placeholder.
    /// </summary>
    public static SettingsProblem? Problem(string executable, IReadOnlyList<string> arguments)
    {
        string trimmed = executable.Trim();

        return trimmed switch
        {
            "" => new SettingsProblem.NoExecutable(),
            _ when !Path.IsPathFullyQualified(trimmed) => new SettingsProblem.ExecutableNotFullPath(),
            _ when IsBatchFile(trimmed) => new SettingsProblem.BatchFileExecutable(),
            _ when !arguments.Any(argument => argument.Contains(FolderPlaceholder, StringComparison.Ordinal)) => new SettingsProblem.NoFolderPlaceholder(),
            _ => null,
        };
    }

    public bool Equals(EditorChoice? other)
    {
        return other is not null
            && Preset == other.Preset
            && string.Equals(Executable, other.Executable, StringComparison.Ordinal)
            && Arguments.SequenceEqual(other.Arguments, StringComparer.Ordinal);
    }

    public override int GetHashCode() => HashCode.Combine(Preset, Executable, Arguments.Count);

    private static bool IsBatchFile(string executable)
    {
        return executable.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
            || executable.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);
    }
}