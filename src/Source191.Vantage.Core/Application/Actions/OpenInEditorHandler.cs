using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

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
        ProgramLocation location = editor.Preset == EditorPreset.Custom
            ? CustomExecutable(editor)
            : await locator.LocateAsync(editor.Preset, cancellationToken);

        return location switch
        {
            ProgramLocation.Found found => await OpenAsync(editor, found.Path, folder, cancellationToken),
            ProgramLocation.Missing => new OpenInEditorOutcome.EditorNotFound(editor),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>A Custom editor's executable is looked for as the key is pressed, since it may have gone since it was saved.</summary>
    private ProgramLocation CustomExecutable(EditorChoice editor) => folders.FileExists(editor.Executable) ? new ProgramLocation.Found(editor.Executable) : new ProgramLocation.Missing();

    private async Task<Outcome> OpenAsync(EditorChoice editor, string executable, FolderPath folder, CancellationToken cancellationToken)
    {
        await desktop.StartAsync(EditorCommand.For(editor, executable, folder), cancellationToken);

        return new OpenInEditorOutcome.Opened(folder.Name, editor);
    }
}