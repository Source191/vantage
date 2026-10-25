using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>
/// Opens a folder in the editor chosen in settings, without waiting for it. The folder is checked first because a
/// stopped session's folder may have been deleted and the editor would otherwise open an empty window.
/// </summary>
public sealed class OpenInEditorHandler(IEditorLocator locator, IDesktop desktop, IFolders folders, CurrentSettings settings) : IEffectHandler<OpenFolderInEditor>
{
    public async Task<Outcome> HandleAsync(OpenFolderInEditor effect, CancellationToken cancellationToken)
    {
        FolderPath folder = effect.Folder;
        if (!folders.DirectoryExists(folder.Value))
        {
            return new OpenInEditorOutcome.FolderMissing(folder.Name);
        }

        EditorChoice editor = settings.Value.Editor;
        string? executable = editor.Preset == EditorPreset.Custom
            ? editor.Executable
            : await locator.LocateAsync(editor.Preset, cancellationToken);
        if (executable is null)
        {
            return new OpenInEditorOutcome.EditorNotFound(editor);
        }

        await desktop.StartAsync(EditorCommand.For(editor, executable, folder), cancellationToken);

        return new OpenInEditorOutcome.Opened(folder.Name, editor);
    }
}