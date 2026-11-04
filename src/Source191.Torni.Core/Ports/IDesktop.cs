namespace Source191.Torni.Ports;

/// <summary>
/// Things Torni hands to the desktop rather than runs and waits for: opening a file with the application Windows
/// associates with it, and starting a program that is left running. Implemented at the boundary, faked in tests.
/// </summary>
public interface IDesktop
{
    /// <summary>Opens a file (or a URL) with its associated application, whose environment lacks Torni's Claude Code markers. Throws <see cref="NoApplicationException"/> when Windows has no application for a file's type (never for an address), and <see cref="ProgramNotStartedException"/>, with the reason, when nothing else could be started (PowerShell missing, the open script failing). A caller with something to undo or say catches the ones it can meet; one with nothing lets them reach the screen as a failed status.</summary>
    Task OpenAsync(string file, CancellationToken cancellationToken);

    /// <summary>Starts a program without waiting for it, with Torni's Claude Code markers left out of its environment. Throws <see cref="ProgramNotStartedException"/> when Windows refuses to start it.</summary>
    Task StartAsync(ProcessRequest request, CancellationToken cancellationToken);
}