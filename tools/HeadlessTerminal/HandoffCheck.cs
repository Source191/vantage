using System.Runtime.Versioning;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// The repeatable hand-off check: run <c>vantage here</c> with a throwaway name and no prompt, wait for Claude's own
/// screen, detach with Ctrl+Z and wait for <c>here</c> to exit by itself. Exit 0 when it did and Claude's screen is in
/// the capture, 1 when <c>here</c> had to be killed after the detach, 2 when the check could not start, 3 when Claude's
/// screen never appeared. The throwaway session is stopped and removed whatever happens; nothing else is touched.
/// Defaults, relative to the current folder: the Release build two levels up, started in that repository.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class HandoffCheck(ClaudeJobs jobs)
{
    public const int CouldNotStart = 2;
    public const int ClaudeNeverShown = 3;

    private const string ClaudeBanner = "Claude Code v";
    private const string CtrlZ = "\u001a";

    private static readonly DriverOptions Options = new(PretendWindowsTerminal: false, AnswerQueries: true, XtversionDelayMs: 0);

    /// <summary>Claude takes a few seconds to start the session and draw; it is given time to settle before the Ctrl+Z, and here time to exit after it.</summary>
    private static readonly StepTimings Timings = new(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10));

    public async Task<int> RunAsync(IReadOnlyList<string> rest)
    {
        string exe = Path.GetFullPath(rest.Count > 0 ? rest[0] : @"..\..\src\Source191.Vantage\bin\Release\net10.0\vantage.exe");
        string repository = Path.GetFullPath(rest.Count > 1 ? rest[1] : @"..\..");
        if (!File.Exists(exe))
        {
            await Console.Error.WriteLineAsync($"No exe at {exe}; build Release first.");
            return CouldNotStart;
        }

        string name = "vantage-check-" + Guid.NewGuid().ToString("N")[..6];
        try
        {
            return await DriveAsync(exe, repository, name);
        }
        finally
        {
            string? jobId = await jobs.FindJobIdAsync(repository, name);
            if (jobId is not null)
            {
                await jobs.StopAndRemoveAsync(repository, jobId);
                await Console.Out.WriteLineAsync($"Throwaway session {name} ({jobId}) stopped and removed.");
            }
        }
    }

    /// <summary>vantage here in the repository, Ctrl+Z once Claude's banner is drawn, then the wait for here to exit; the capture stays in the start folder.</summary>
    private static async Task<int> DriveAsync(string exe, string repository, string name)
    {
        string home = Environment.CurrentDirectory;
        Environment.CurrentDirectory = repository;
        try
        {
            StepRunner runner = new(Options, Timings, "drive-raw.txt");
            SmokeStep detach = new(new CaptureMarker.ScreenText(ClaudeBanner), CtrlZ, TimeSpan.FromSeconds(60));
            int result = await runner.RunAsync(exe, [detach], ["here", name]);

            return await ReportAsync(result);
        }
        finally
        {
            File.Move(Path.Combine(repository, "drive-raw.txt"), Path.Combine(home, "drive-raw.txt"), overwrite: true);
            Environment.CurrentDirectory = home;
        }
    }

    private static async Task<int> ReportAsync(int result)
    {
        switch (result)
        {
            case StepRunner.MarkerNeverAppeared:
                await Console.Error.WriteLineAsync("Claude's own screen is not in the capture: the attach never reached Claude.");
                return ClaudeNeverShown;
            case 0 when HandoffCapture.ShowsClaudeScreen(await File.ReadAllTextAsync("drive-raw.txt")):
                await Console.Out.WriteLineAsync("Claude's own screen is in the capture, and vantage here exited by itself after the detach.");
                return 0;
            case 0:
                await Console.Error.WriteLineAsync("vantage here exited, but Claude's own screen is not in the capture.");
                return ClaudeNeverShown;
            default:
                await Console.Error.WriteLineAsync("vantage here did not exit within the deadline after the detach.");
                return result;
        }
    }
}