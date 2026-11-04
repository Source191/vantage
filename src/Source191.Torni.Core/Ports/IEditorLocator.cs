using Source191.Torni.Domain;

namespace Source191.Torni.Ports;

/// <summary>
/// Where an editor preset really is on this machine: the executable itself, never a .cmd shim, because Windows runs
/// batch files through cmd.exe and a folder path would be parsed there. Missing when the editor is not installed.
/// </summary>
public interface IEditorLocator
{
    Task<ProgramLocation> LocateAsync(EditorPreset preset, CancellationToken cancellationToken);
}