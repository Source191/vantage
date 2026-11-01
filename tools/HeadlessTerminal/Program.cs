namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// HeadlessTerminal drives a console program inside a Windows pseudo-console
/// (<see href="https://learn.microsoft.com/windows/console/creating-a-pseudoconsole-session">ConPTY</see>) with scripted keystrokes and
/// captures everything it writes, replays a capture as a screen, counts markers in one, and runs the hand-off and smoke checks.
/// <code>
///   dotnet run --project tools\HeadlessTerminal -- check [exe] [repository]
///   dotnet run --project tools\HeadlessTerminal -- smoke &lt;exe&gt; &lt;worktree-folder&gt;
///   dotnet run --project tools\HeadlessTerminal -- smoke-update &lt;exe&gt;
///   dotnet run --project tools\HeadlessTerminal -- smoke-whats-new &lt;exe&gt;
///   dotnet run --project tools\HeadlessTerminal -- drive &lt;exe&gt; &lt;script&gt; [args...]
///   dotnet run --project tools\HeadlessTerminal -- grid &lt;raw-file&gt; &lt;marker&gt; [cols] [rows]
///   dotnet run --project tools\HeadlessTerminal -- count &lt;raw-file&gt; &lt;marker...&gt;
/// </code>
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            await Console.Error.WriteLineAsync("HeadlessTerminal needs Windows: it drives programs through ConPTY.");
            return 2;
        }

        return args switch
        {
            ["check", .. string[] rest] => await new HandoffCheck(new ClaudeJobs()).RunAsync(rest),
            ["smoke", string exe, string worktreeFolder] => await new SmokeCheck(new StepRunner(SmokeCheck.Options, StepTimings.Default, "drive-raw.txt")).RunAsync(exe, worktreeFolder),
            ["smoke-update", string exe] => await new SmokeCheck(new StepRunner(SmokeCheck.Options, StepTimings.Default, "drive-raw.txt")).RunAsync(exe, SmokeScript.WhatsNewBeforeUpdate()),
            ["smoke-whats-new", string exe] => await new SmokeCheck(new StepRunner(SmokeCheck.Options, StepTimings.Default, "drive-raw.txt")).RunAsync(exe, SmokeScript.WhatsNewOnStart()),
            ["drive", string exe, string script, .. string[] rest] => await new Driver(DriverOptions.FromEnvironment()).RunAsync(exe, KeystrokeScript.Parse(script), rest),
            ["grid", string raw, string marker, .. string[] size] => ScreenReplay.PrintFile(raw, marker, size),
            ["count", string raw, .. string[] markers] => MarkerCount.PrintFile(raw, markers),
            _ => await UsageAsync(),
        };
    }

    private static async Task<int> UsageAsync()
    {
        await Console.Error.WriteLineAsync("usage: headless-terminal check [exe] [repository] | smoke <exe> <worktree-folder> | smoke-update <exe> | smoke-whats-new <exe> | drive <exe> <script> [args...] | grid <raw> <marker> [cols] [rows] | count <raw> <marker...>");
        return 2;
    }
}