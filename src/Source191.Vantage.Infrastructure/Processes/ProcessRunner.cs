using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;
using System.Text;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// Runs child processes with System.Diagnostics.Process. Captured runs redirect output; interactive runs
/// inherit the console so the child owns the terminal until it exits.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        using Process process = new();
        process.StartInfo = StartInfo(request, capture: true);
        process.Start();

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult(process.ExitCode, await standardOutput, await standardError);
    }

    public async Task<int> RunInteractiveAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        using Process process = new();
        process.StartInfo = StartInfo(request, capture: false);
        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode;
    }

    /// <summary>The empty string is ProcessStartInfo's own "start in this process's folder".</summary>
    private static string FolderOf(WorkingFolder folder) => folder switch
    {
        WorkingFolder.Inherited => string.Empty,
        WorkingFolder.At at => at.Path,
        _ => throw new UnreachableException(),
    };

    internal static ProcessStartInfo StartInfo(ProcessRequest request, bool capture)
    {
        // Redirected streams decode with the console code page unless told otherwise; Claude Code prints UTF-8
        // (its start line uses a middle dot), so both streams are read as UTF-8.
        ProcessStartInfo startInfo = new(request.FileName)
        {
            UseShellExecute = false,
            // A captured child (git, claude --json) must never get a console window of its own,
            // which would take the focus from the terminal; an interactive child owns the current console instead.
            CreateNoWindow = capture,
            RedirectStandardOutput = capture,
            RedirectStandardError = capture,
            StandardOutputEncoding = capture ? Encoding.UTF8 : null,
            StandardErrorEncoding = capture ? Encoding.UTF8 : null,
            WorkingDirectory = FolderOf(request.WorkingDirectory),
        };
        foreach (string argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (string name in startInfo.Environment.Keys.Where(InheritedEnvironment.ShouldDrop).ToList())
        {
            startInfo.Environment.Remove(name);
        }

        return startInfo;
    }
}