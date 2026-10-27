using System.Runtime.Versioning;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// The repeatable hand-off check: start a throwaway background session, attach to it from Vantage, detach with
/// Ctrl+Z, open and cancel the wizard, quit. Exit 0 when Vantage quit by itself and Claude's own screen is in the
/// capture, 1 when Vantage had to be killed (a blocked UI thread), 2 when the check could not start, 3 when Vantage quit but
/// Claude's screen never appeared during the hand-off. The throwaway session is stopped and removed whatever happens;
/// nothing else is touched.
/// Defaults, relative to the current folder: the Release build two levels up, started in that repository.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class HandoffCheck(ClaudeJobs jobs)
{
    private const string Prompt = "Reply with the single word ready and nothing else.";

    public async Task<int> RunAsync(IReadOnlyList<string> rest)
    {
        string exe = Path.GetFullPath(rest.Count > 0 ? rest[0] : @"..\..\src\Source191.Vantage\bin\Release\net10.0\vantage.exe");
        string repository = Path.GetFullPath(rest.Count > 1 ? rest[1] : @"..\..");
        if (!File.Exists(exe))
        {
            await Console.Error.WriteLineAsync($"No exe at {exe}; build Release first.");
            return 2;
        }

        string name = "vantage-check-" + Guid.NewGuid().ToString("N")[..6];
        string? jobId = null;
        try
        {
            await jobs.StartAsync(repository, name, Prompt);
            jobId = await jobs.FindJobIdAsync(repository, name);
            if (jobId is null)
            {
                await Console.Error.WriteLineAsync($"The throwaway session {name} did not appear in claude agents --json.");
                return 2;
            }

            await Console.Out.WriteLineAsync($"Throwaway session {name} ({jobId}) started in {repository}.");

            return await DriveAsync(exe, repository, name);
        }
        finally
        {
            if (jobId is not null)
            {
                await jobs.StopAndRemoveAsync(repository, jobId);
                await Console.Out.WriteLineAsync($"Throwaway session {jobId} stopped and removed.");
            }
        }
    }

    /// <summary>Filter to the session, attach, detach with Ctrl+Z, open the wizard, cancel it, quit; the capture stays in the start folder.</summary>
    private static async Task<int> DriveAsync(string exe, string repository, string name)
    {
        string home = Environment.CurrentDirectory;
        Environment.CurrentDirectory = repository;
        try
        {
            DriverOptions options = new(PretendWindowsTerminal: false, AnswerQueries: true, XtversionDelayMs: 0);
            IReadOnlyList<Keystroke> script = KeystrokeScript.Parse($"7000:/{name}\\r,1500:\\r,12000:\\u001a,4000:n,3000:\\u001b,2000:q");
            int result = await new Driver(options).RunAsync(exe, script, []);
            await Console.Out.WriteLineAsync(result == 0
                ? "Vantage quit by itself after the hand-off: keys still work."
                : "Vantage did not quit within the deadline after the hand-off: the UI thread is probably blocked.");
            if (result != 0)
            {
                return result;
            }

            string capture = await File.ReadAllTextAsync("drive-raw.txt");
            if (HandoffCapture.ShowsClaudeScreen(capture))
            {
                await Console.Out.WriteLineAsync("Claude's own screen is in the capture.");
                return 0;
            }

            await Console.Error.WriteLineAsync("Claude's own screen is not in the capture: the attach never reached Claude.");
            return 3;
        }
        finally
        {
            File.Move(Path.Combine(repository, "drive-raw.txt"), Path.Combine(home, "drive-raw.txt"), overwrite: true);
            Environment.CurrentDirectory = home;
        }
    }
}