using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Source191.Vantage.Adapters.Claude;

public sealed partial class ClaudeCli(IProcessRunner processes, string executable) : IClaudeCli
{
    public async Task<SessionListing> ListSessionsAsync(CancellationToken cancellationToken)
    {
        ProcessResult result = await processes.RunAsync(Request(["agents", "--json", "--all"]), cancellationToken);

        return ClaudeAgentsListing.Parse(result.StandardOutput);
    }

    public async Task<string?> VersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            ProcessResult result = await processes.RunAsync(Request(["--version"]), cancellationToken);
            Match version = LeadingVersion().Match(result.StandardOutput);

            return result.ExitCode == 0 && version.Success ? version.Value : null;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException or OperationCanceledException)
        {
            // A cosmetic header value: a program that cannot start, fails to answer or is given up on reads as unknown.
            return null;
        }
    }

    public async Task<StartResult> StartBackgroundAsync(string directory, SessionName name, string? openingPrompt, CancellationToken cancellationToken)
    {
        List<string> arguments = ["--bg", "-n", name.Value];
        if (openingPrompt is not null)
        {
            arguments.Add(openingPrompt);
        }

        ProcessResult result = await processes.RunAsync(Request(arguments, directory), cancellationToken);

        return InterpretStart(result);
    }

    public Task<int> AttachAsync(string jobId, CancellationToken cancellationToken) => processes.RunInteractiveAsync(Request(["attach", jobId]), cancellationToken);

    public Task StopAsync(string jobId, CancellationToken cancellationToken) => processes.RunAsync(Request(["stop", jobId]), cancellationToken);

    public Task RemoveAsync(string jobId, CancellationToken cancellationToken) => processes.RunAsync(Request(["rm", jobId]), cancellationToken);

    public async Task<ResumeResult> ResumeBackgroundAsync(string sessionId, string directory, SessionName? name, CancellationToken cancellationToken)
    {
        List<string> arguments = ["--bg", "--resume", sessionId];
        if (name is not null)
        {
            arguments.AddRange(["-n", name.Value]);
        }

        ProcessResult result = await processes.RunAsync(Request(arguments, directory), cancellationToken);
        if (result.ExitCode != 0)
        {
            return new ResumeResult(Continued: false, (result.StandardError + result.StandardOutput).Trim());
        }

        string? note = result.StandardOutput
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("note:", StringComparison.OrdinalIgnoreCase));

        return new ResumeResult(Continued: note is null, note ?? result.StandardOutput.Trim());
    }

    private ProcessRequest Request(IReadOnlyList<string> arguments, string? workingDirectory = null) => new(executable, arguments, workingDirectory);

    private static StartResult InterpretStart(ProcessResult result)
    {
        Match started = BackgroundedLine().Match(result.StandardOutput);
        if (started.Success)
        {
            return new StartResult(StartOutcome.Started, started.Groups["id"].Value, result.StandardOutput.Trim());
        }

        string message = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput.Trim() : result.StandardError.Trim();
        StartOutcome outcome = message.Contains("Workspace not trusted", StringComparison.OrdinalIgnoreCase)
            ? StartOutcome.WorkspaceNotTrusted
            : StartOutcome.Failed;

        return new StartResult(outcome, JobId: null, message);
    }

    [GeneratedRegex(@"^\s*\d+(\.\d+)+")]
    private static partial Regex LeadingVersion();

    // The separator is a middle dot; matched loosely so a mis-decoded byte cannot hide a successful start.
    [GeneratedRegex(@"backgrounded\s+\S+\s+(?<id>[0-9a-f]{8})\b")]
    private static partial Regex BackgroundedLine();
}