using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Text.RegularExpressions;

namespace Source191.Vantage.Adapters.Claude;

public sealed partial class ClaudeCli(IProcessRunner processes, string executable) : IClaudeCli
{
    public async Task<IReadOnlyList<Session>> ListSessionsAsync(CancellationToken cancellationToken)
    {
        ProcessResult result = await processes.RunAsync(Request(["agents", "--json", "--all"]), cancellationToken);

        return ClaudeAgentsListing.Parse(result.StandardOutput);
    }

    public async Task<StartResult> StartBackgroundAsync(string directory, SessionName name, string? openingPrompt, CancellationToken cancellationToken)
    {
        List<string> arguments = ["--bg", "-n", name.Text];
        if (openingPrompt is not null)
        {
            arguments.Add(openingPrompt);
        }

        ProcessResult result = await processes.RunAsync(Request(arguments, directory), cancellationToken);

        return InterpretStart(result);
    }

    public Task<int> AttachAsync(string jobId, CancellationToken cancellationToken)
    {
        return processes.RunInteractiveAsync(Request(["attach", jobId]), cancellationToken);
    }

    public Task StopAsync(string jobId, CancellationToken cancellationToken)
    {
        return processes.RunAsync(Request(["stop", jobId]), cancellationToken);
    }

    public Task RemoveAsync(string jobId, CancellationToken cancellationToken)
    {
        return processes.RunAsync(Request(["rm", jobId]), cancellationToken);
    }

    public async Task<ResumeResult> ResumeBackgroundAsync(string sessionId, string directory, SessionName? name, CancellationToken cancellationToken)
    {
        List<string> arguments = ["--bg", "--resume", sessionId];
        if (name is not null)
        {
            arguments.AddRange(["-n", name.Text]);
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

    public Task<int> OpenAgentViewAsync(CancellationToken cancellationToken)
    {
        return processes.RunInteractiveAsync(Request(["agents"]), cancellationToken);
    }

    private ProcessRequest Request(IReadOnlyList<string> arguments, string? workingDirectory = null)
    {
        return new ProcessRequest(executable, arguments, workingDirectory);
    }

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

    // The separator is a middle dot; matched loosely so a mis-decoded byte cannot hide a successful start.
    [GeneratedRegex(@"backgrounded\s+\S+\s+(?<id>[0-9a-f]{8})\b")]
    private static partial Regex BackgroundedLine();
}