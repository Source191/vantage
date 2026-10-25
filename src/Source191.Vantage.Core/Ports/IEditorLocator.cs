using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>
/// Where an editor preset really is on this machine: the executable itself, never a .cmd shim, because Windows runs
/// batch files through cmd.exe and a folder path would be parsed there. Null when the editor is not installed.
/// </summary>
public interface IEditorLocator
{
    Task<string?> LocateAsync(EditorPreset preset, CancellationToken cancellationToken);
}