using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// Hands things to Windows: a file or URL to its associated application (ShellExecute), a program to run on its own.
/// Neither is waited for to close, and both get the same environment as every other child of Vantage, without the Claude
/// Code markers. ShellExecute cannot be given an environment, so a short-lived PowerShell child that has the cleaned one
/// makes the call (see <see cref="OpenFileScript"/>); the call waits for that child, which ends once Windows has started the
/// application, so a file that has gone is reported rather than lost.
/// </summary>
public sealed class Desktop(IProcessRunner processes) : IDesktop
{
    public async Task OpenAsync(string file, CancellationToken cancellationToken)
    {
        ProcessRequest request = new("pwsh.exe", ["-NoProfile", "-NonInteractive", "-EncodedCommand", EncodedCommand.Encode(OpenFileScript.For(file))], WorkingDirectory: null);
        ProcessResult result = await processes.RunAsync(request, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(result.StandardError.Trim());
        }
    }

    public Task StartAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = ProcessRunner.StartInfo(request, capture: false);
        startInfo.CreateNoWindow = true;
        using Process? process = Process.Start(startInfo);

        return Task.CompletedTask;
    }
}