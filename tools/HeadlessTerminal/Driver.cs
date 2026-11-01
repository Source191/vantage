using System.Runtime.Versioning;
using System.Text.Json;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// Runs a program in a pseudo-console, sends the scripted keystrokes on time, and reports whether the program exited
/// by itself within six seconds of the last key; the capture goes to drive-raw.txt (<see cref="CapturedRun"/>).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class Driver(DriverOptions options)
{
    private const int GraceAfterLastKeyMs = 6000;

    public async Task<int> RunAsync(string exe, IReadOnlyList<Keystroke> script, IReadOnlyList<string> arguments)
    {
        using CapturedRun run = CapturedRun.Start(options, exe, arguments);

        foreach (Keystroke step in script)
        {
            await WaitUntil(run, step.At);
            if (run.HasExited)
            {
                break;
            }

            await run.SendAsync(step.Keys);
            await Console.Out.WriteLineAsync($"t={step.At}ms sent {JsonSerializer.Serialize(step.Keys)} (output so far {run.Captured().Length} chars)");
        }

        int deadline = (script.Count == 0 ? 0 : script[^1].At) + GraceAfterLastKeyMs;

        return await run.FinishAsync(deadline) ? 0 : 1;
    }

    private static async Task WaitUntil(CapturedRun run, int at)
    {
        long wait = at - run.ElapsedMilliseconds;
        if (wait > 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(wait));
        }
    }
}