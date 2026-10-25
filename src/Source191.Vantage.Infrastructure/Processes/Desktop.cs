using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// Hands things to Windows: a file to its associated application (ShellExecute), a program to run on its own. A started
/// program is never waited for and gets the same environment as every other child of Vantage, without the Claude Code markers.
/// </summary>
public sealed class Desktop : IDesktop
{
    public Task OpenAsync(string file, CancellationToken cancellationToken)
    {
        using Process? process = Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });

        return Task.CompletedTask;
    }

    public Task StartAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = ProcessRunner.StartInfo(request, capture: false);
        startInfo.CreateNoWindow = true;
        using Process? process = Process.Start(startInfo);

        return Task.CompletedTask;
    }
}