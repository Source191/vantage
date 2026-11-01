using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// Hands things to Windows: a file or URL to its associated application (ShellExecute), a program to run on its own.
/// Neither is waited for to close, and both get the same environment as every other child of Vantage, without the Claude
/// Code markers. ShellExecute cannot be given an environment, so a short-lived PowerShell child that has the cleaned one
/// makes the call (see <see cref="OpenFileScript"/>); the call waits for that child, which ends once Windows has started the
/// application, so a file that has gone is reported rather than lost. PowerShell is run by the full path it was given
/// (missing when pwsh.exe is not on the PATH, which fails each open with a sentence naming it).
/// </summary>
public sealed class Desktop(IProcessRunner processes, ProgramLocation powerShell) : IDesktop
{
    public async Task OpenAsync(string file, CancellationToken cancellationToken)
    {
        // Thrown inside the task, as before, so a caller holding the task sees the failure when it awaits.
        string executable = powerShell switch
        {
            ProgramLocation.Found found => found.Path,
            ProgramLocation.Missing => throw new InvalidOperationException("PowerShell 7 (pwsh.exe) was not found on the PATH, so nothing can be opened."),
            _ => throw new UnreachableException(),
        };

        ProcessRequest request = new(executable, ["-NoProfile", "-NonInteractive", "-EncodedCommand", EncodedCommand.Encode(OpenFileScript.For(file))], WorkingFolder.Inherit);
        ProcessResult result = await processes.RunAsync(request, cancellationToken);
        if (result.ExitCode != 0)
        {
            string reason = result.StandardError.Trim();
            throw new InvalidOperationException(reason.Length > 0 ? reason : $"PowerShell stopped with exit code {result.ExitCode} before Windows opened it.");
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