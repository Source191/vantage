using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// The process that opens a folder in an editor. Arguments are a list with the folder as one element (for a custom
/// editor, each placeholder replaced inside its own argument), so no quoting or interpolation happens anywhere and a
/// path holding &amp;, quotes or spaces reaches the editor untouched.
/// </summary>
public static class EditorCommand
{
    public static ProcessRequest For(EditorChoice editor, string executable, FolderPath folder)
    {
        IReadOnlyList<string> arguments = editor.Preset == EditorPreset.Custom
            ? [.. editor.Arguments.Select(argument => argument.Replace(EditorChoice.FolderPlaceholder, folder.Value, StringComparison.Ordinal))]
            : [folder.Value];

        return new ProcessRequest(executable, arguments, WorkingDirectory: null);
    }
}