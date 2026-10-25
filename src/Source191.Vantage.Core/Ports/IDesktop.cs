namespace Source191.Vantage.Ports;

/// <summary>
/// Things Vantage hands to the desktop rather than runs and waits for: opening a file with the application Windows
/// associates with it, and starting a program that is left running. Implemented at the boundary, faked in tests.
/// </summary>
public interface IDesktop
{
    /// <summary>Opens a file (or a URL) with its associated application.</summary>
    Task OpenAsync(string file, CancellationToken cancellationToken);

    /// <summary>Starts a program without waiting for it, with Vantage's Claude Code markers left out of its environment.</summary>
    Task StartAsync(ProcessRequest request, CancellationToken cancellationToken);
}