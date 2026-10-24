using System.Diagnostics;
using System.Text.Json;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>The few Claude Code CLI calls the hand-off check needs: start a background job, find it by name, stop and remove it.</summary>
public sealed class ClaudeJobs
{
    private readonly string claude = Locate();

    public async Task StartAsync(string repository, string name, string prompt)
    {
        await RunAsync(repository, "--bg", "-n", name, prompt);
    }

    public async Task<string?> FindJobIdAsync(string repository, string name)
    {
        string json = await RunAsync(repository, "agents", "--json");
        using JsonDocument listing = JsonDocument.Parse(json);
        foreach (JsonElement session in listing.RootElement.EnumerateArray())
        {
            bool named = session.TryGetProperty("name", out JsonElement sessionName) && sessionName.GetString() == name;
            if (named && session.TryGetProperty("id", out JsonElement id))
            {
                return id.GetString();
            }
        }

        return null;
    }

    public async Task StopAndRemoveAsync(string repository, string jobId)
    {
        await RunAsync(repository, "stop", jobId);
        await RunAsync(repository, "rm", jobId);
    }

    private async Task<string> RunAsync(string directory, params string[] arguments)
    {
        ProcessStartInfo start = new(claude) { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start) ?? throw new InvalidOperationException("claude did not start");
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output;
    }

    private static string Locate()
    {
        string native = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "claude.exe");

        return File.Exists(native) ? native : "claude";
    }
}