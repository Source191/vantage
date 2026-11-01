using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Source191.Vantage.Adapters.Claude;

/// <summary>
/// The Claude CLI adapter: runs claude by its full path for the listing, the version, a <c>--bg</c> start and resume,
/// attach, stop and rm, and reads the CLI's words in its answers (the version, the job id, an untrusted workspace) here.
/// </summary>
public sealed partial class ClaudeCli(IProcessRunner processes, string executable) : IClaudeCli
{
    public async Task<SessionListing> ListSessionsAsync(CancellationToken cancellationToken)
    {
        ProcessResult result = await RunOrThrowAsync(["agents", "--json", "--all"], cancellationToken);

        return ClaudeAgentsListing.Parse(result.StandardOutput);
    }

    public async Task<ClaudeVersionReport> VersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            ProcessResult result = await processes.RunAsync(Request(["--version"], WorkingFolder.Inherit), cancellationToken);
            Match version = LeadingVersion().Match(result.StandardOutput);

            return result.ExitCode == 0 && version.Success ? new ClaudeVersionReport.Printed(version.Value) : new ClaudeVersionReport.NotRead();
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException or OperationCanceledException or ProcessTimedOutException)
        {
            // A cosmetic header value: a program that cannot start, fails to answer or is given up on reads as unknown.
            return new ClaudeVersionReport.NotRead();
        }
    }

    public async Task<StartResult> StartBackgroundAsync(string directory, SessionName name, OpeningPrompt openingPrompt, CancellationToken cancellationToken)
    {
        List<string> arguments = ["--bg", "-n", name.Value];
        if (openingPrompt is OpeningPrompt.Text text)
        {
            arguments.Add(text.Words);
        }

        ProcessResult result = await processes.RunAsync(Request(arguments, new WorkingFolder.At(directory)), cancellationToken);

        return InterpretStart(result);
    }

    public Task<int> AttachAsync(string jobId, CancellationToken cancellationToken) => processes.RunInteractiveAsync(Request(["attach", jobId], WorkingFolder.Inherit), cancellationToken);

    public Task StopAsync(string jobId, CancellationToken cancellationToken) => RunOrThrowAsync(["stop", jobId], cancellationToken);

    public Task RemoveAsync(string jobId, CancellationToken cancellationToken) => RunOrThrowAsync(["rm", jobId], cancellationToken);

    public Task<ResumeResult> ResumeBackgroundAsync(string sessionId, string directory, SessionName name, CancellationToken cancellationToken) =>
        ResumeAsync(["--bg", "--resume", sessionId, "-n", name.Value], directory, cancellationToken);

    public Task<ResumeResult> ResumeBackgroundAsync(string sessionId, string directory, CancellationToken cancellationToken) =>
        ResumeAsync(["--bg", "--resume", sessionId], directory, cancellationToken);

    private async Task<ResumeResult> ResumeAsync(IReadOnlyList<string> arguments, string directory, CancellationToken cancellationToken)
    {
        ProcessResult result = await processes.RunAsync(Request(arguments, new WorkingFolder.At(directory)), cancellationToken);
        if (result.ExitCode != 0)
        {
            return new ResumeResult.Failed((result.StandardError + result.StandardOutput).Trim());
        }

        string? note = result.StandardOutput
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("note:", StringComparison.OrdinalIgnoreCase));

        return note is null ? new ResumeResult.Continued(result.StandardOutput.Trim()) : new ResumeResult.Failed(note);
    }

    private async Task<ProcessResult> RunOrThrowAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        ProcessResult result = await processes.RunAsync(Request(arguments, WorkingFolder.Inherit), cancellationToken);
        if (result.ExitCode == 0)
        {
            return result;
        }

        throw new ClaudeCliException(RefusalWords(arguments[0], result));
    }

    private static string RefusalWords(string subcommand, ProcessResult result)
    {
        string words = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput.Trim() : result.StandardError.Trim();

        return words.Length > 0 ? words : $"claude {subcommand} exited with code {result.ExitCode}";
    }

    private ProcessRequest Request( IReadOnlyList<string> arguments, WorkingFolder folder) => new(executable, arguments, folder);

    private static StartResult InterpretStart(ProcessResult result)
    {
        Match started = BackgroundedLine().Match(result.StandardOutput);
        if (started.Success)
        {
            // The pattern's eight hex characters are what a job id is, so this never fails; the adapter still checks the text it was given.
            return JobId.TryParse(started.Groups["id"].Value, out JobId? jobId)
                ? new StartResult.Started(jobId, result.StandardOutput.Trim())
                : new StartResult.Failed(result.StandardOutput.Trim());
        }

        string message = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput.Trim() : result.StandardError.Trim();

        return message.Contains("Workspace not trusted", StringComparison.OrdinalIgnoreCase)
            ? new StartResult.WorkspaceNotTrusted(message)
            : new StartResult.Failed(message);
    }

    [GeneratedRegex(@"^\s*\d+(\.\d+)+")]
    private static partial Regex LeadingVersion();

    // The separator is a middle dot; matched loosely so a mis-decoded byte cannot hide a successful start.
    [GeneratedRegex(@"backgrounded\s+\S+\s+(?<id>[0-9a-f]{8})\b")]
    private static partial Regex BackgroundedLine();
}