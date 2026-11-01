using System.Diagnostics;

namespace Source191.Vantage.Domain;

/// <summary>
/// The editor that opens a folder: a preset, or a custom executable whose arguments hold <see cref="FolderPlaceholder"/>
/// where the folder goes. Arguments are a list, never a command line, so nothing re-parses a path. A preset's
/// executable is found by <see cref="Ports.IEditorLocator"/> when key f opens a folder; this only holds the choice.
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

    /// <summary>For a custom command already checked by <see cref="Validate"/>; typed text goes through <see cref="Settings.Validate"/>.</summary>
    public static EditorChoice Custom(string executable, IReadOnlyList<string> arguments)
    {
        return Validate(executable, arguments, AnyFileExists) switch
        {
            EditorChoiceValidation.Valid valid => valid.Editor,
            EditorChoiceValidation.Refused refused => throw new ArgumentException($"'{executable}' is not a usable custom editor: {refused.Problem}.", nameof(executable)),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>The editor as config.json holds it: a missing or unusable one reads as VS Code, and a preset ignores any command beside it. A custom executable that does not exist is kept (it may be on a drive not yet attached), so the editor key reports it.</summary>
    public static EditorChoice FromFile(EditorPreset? preset, string? executable, IReadOnlyList<string>? arguments)
    {
        return preset switch
        {
            null => VsCode,
            EditorPreset.Custom when executable is string path && arguments is { } list && Validate(path, list, AnyFileExists) is EditorChoiceValidation.Valid valid => valid.Editor,
            EditorPreset.Custom => VsCode,
            EditorPreset known => Of(known),
        };
    }

    /// <summary>
    /// A custom command checked against the rules: the editor, or the first reason it cannot be used: no executable, not a
    /// full path (<see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw">Windows
    /// would look for a bare name in the folder Vantage was started from</see>), a batch file
    /// (<see href="https://learn.microsoft.com/windows-server/administration/windows-commands/cmd">cmd.exe</see> would
    /// parse the folder path), no placeholder, or, last, an executable <paramref name="fileExists"/> says is not there, so a
    /// missing file never hides a reason the typed text alone gives.
    /// </summary>
    public static EditorChoiceValidation Validate(string executable, IReadOnlyList<string> arguments, Func<string, bool> fileExists)
    {
        ArgumentNullException.ThrowIfNull(executable);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(fileExists);
        string trimmed = executable.Trim();

        return trimmed switch
        {
            "" => Refused(new SettingsProblem.NoExecutable()),
            _ when !Path.IsPathFullyQualified(trimmed) => Refused(new SettingsProblem.ExecutableNotFullPath()),
            _ when IsBatchFile(trimmed) => Refused(new SettingsProblem.BatchFileExecutable()),
            _ when !arguments.Any(argument => argument.Contains(FolderPlaceholder, StringComparison.Ordinal)) => Refused(new SettingsProblem.NoFolderPlaceholder()),
            _ when !fileExists(trimmed) => Refused(new SettingsProblem.ExecutableMissing(trimmed)),
            _ => new EditorChoiceValidation.Valid(new EditorChoice(EditorPreset.Custom, trimmed, arguments)),
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

    private static EditorChoiceValidation.Refused Refused(SettingsProblem problem) => new(problem);

    /// <summary>The existence check for a command Vantage already holds: a file's editor and <see cref="Custom"/> are never refused for a missing executable.</summary>
    private static bool AnyFileExists(string executable) => true;

    private static bool IsBatchFile(string executable)
    {
        return executable.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
            || executable.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);
    }
}