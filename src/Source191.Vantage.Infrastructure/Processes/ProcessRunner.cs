using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;
using System.Text;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// Runs child processes with System.Diagnostics.Process. Captured runs redirect output; interactive runs
/// inherit the console so the child owns the terminal until it exits.
/// </summary>
public sealed class ProcessRunner(TimeSpan limit) : IProcessRunner
{
    /// <summary>How long a captured run may take: a claude agents listing takes about two seconds, the slowest gh or az call a few.</summary>
    public static readonly TimeSpan DefaultLimit = TimeSpan.FromSeconds(60);

    public ProcessRunner()
        : this(DefaultLimit)
    {
    }

    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        using Process process = new();
        process.StartInfo = StartInfo(request, capture: true);
        process.Start();

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        Task<string> standardError = process.StandardError.ReadToEndAsync(CancellationToken.None);
        using CancellationTokenSource timeout = new(limit);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            await EndAsync(process, standardOutput, standardError);
            cancellationToken.ThrowIfCancellationRequested();

            throw new ProcessTimedOutException(Path.GetFileName(request.FileName), limit);
        }

        return new ProcessResult(process.ExitCode, await standardOutput, await standardError);
    }

    /// <summary>Ends the child and its descendants, then waits for the pipes to close; one already gone, or a descendant that cannot be ended, is not an error.</summary>
    private static async Task EndAsync(Process process, Task<string> standardOutput, Task<string> standardError)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or AggregateException)
        {
            // Already exited, or a descendant refused: the exception that follows still reports the outcome.
        }

        try
        {
            await Task.WhenAll(standardOutput, standardError).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            // A descendant that survived still holds a pipe open; the output is no longer wanted.
        }
    }

    public async Task<int> RunInteractiveAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        using Process process = new();
        process.StartInfo = StartInfo(request, capture: false);
        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode;
    }

    /// <summary>
    /// The empty string is
    /// <see href="https://learn.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.workingdirectory">ProcessStartInfo's
    /// own "start in this process's folder"</see>.
    /// </summary>
    private static string FolderOf(WorkingFolder folder) => folder switch
    {
        WorkingFolder.Inherited => string.Empty,
        WorkingFolder.At at => at.Path,
        _ => throw new UnreachableException(),
    };

    internal static ProcessStartInfo StartInfo(ProcessRequest request, bool capture)
    {
        // Redirected streams decode with the console code page unless told otherwise; Claude Code prints UTF-8
        // (its start line uses a middle dot), so both streams are read as UTF-8
        // (https://learn.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.standardoutputencoding).
        ProcessStartInfo startInfo = new(request.FileName)
        {
            UseShellExecute = false,
            // A captured child (git, claude --json) must never get a console window of its own,
            // which would take the focus from the terminal; an interactive child owns the current console instead
            // (https://learn.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.createnowindow).
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