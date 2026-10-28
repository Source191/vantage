using System.Runtime.Versioning;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// The smoke check of an installed Vantage: opens and cancels every screen and dialog a clean machine can reach
/// (<see cref="SmokeScript"/>) and quits. Exit 0 when every step's marker appeared and Vantage quit by itself, 1 when it
/// had to be killed (a blocked UI thread), 2 when there is no exe, 4 when a marker never appeared (the step is named).
/// It only ever cancels, so it is safe against your own repositories; the capture goes to drive-raw.txt.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SmokeCheck(StepRunner runner)
{
    public const int CouldNotStart = 2;

    /// <summary>As a user runs Vantage: inside Windows Terminal, which answers its queries.</summary>
    public static readonly DriverOptions Options = new(PretendWindowsTerminal: true, AnswerQueries: true, XtversionDelayMs: 0);

    public async Task<int> RunAsync(string exe, string worktreeFolder)
    {
        string fullPath = Path.GetFullPath(exe);
        if (!File.Exists(fullPath))
        {
            await Console.Error.WriteLineAsync($"No exe at {fullPath}.");
            return CouldNotStart;
        }

        int result = await runner.RunAsync(fullPath, SmokeScript.Build(worktreeFolder), []);
        await Console.Out.WriteLineAsync(result switch
        {
            0 => "Every screen and dialog opened and closed, and Vantage quit by itself.",
            StepRunner.MarkerNeverAppeared => "A screen or dialog did not appear; the step is named above and the capture is in drive-raw.txt.",
            _ => "Vantage did not quit within the deadline: the UI thread is probably blocked.",
        });

        return result;
    }
}