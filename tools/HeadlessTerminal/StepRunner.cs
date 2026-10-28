using System.Runtime.Versioning;
using System.Text.Json;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>One step of a marker-led script: wait up to <paramref name="Timeout"/> for the marker, then send the keys.</summary>
public sealed record SmokeStep(CaptureMarker Marker, string Keys, TimeSpan Timeout);

/// <summary>How long to let the program settle after a marker before sending keys, and how long it has to exit after the last step.</summary>
public sealed record StepTimings(TimeSpan Settle, TimeSpan Grace)
{
    public static readonly StepTimings Default = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(6));
}

/// <summary>
/// Runs a program in a pseudo-console and works through the steps: each waits for its marker after the previous
/// step's match, settles, then sends its keys. Exit 0 when every marker appeared and the program exited by itself within
/// the grace, 1 when it had to be killed, <see cref="MarkerNeverAppeared"/> when a marker did not show in time.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class StepRunner(DriverOptions options, StepTimings timings, string captureFile)
{
    public const int MarkerNeverAppeared = 4;

    public async Task<int> RunAsync(string exe, IReadOnlyList<SmokeStep> steps, IReadOnlyList<string> arguments)
    {
        using CapturedRun run = CapturedRun.Start(options, exe, arguments);
        int position = 0;
        for (int index = 0; index < steps.Count; index++)
        {
            SmokeStep step = steps[index];
            int? found = await WaitForAsync(run, step, position);
            if (found is null)
            {
                await Console.Error.WriteLineAsync($"Step {index + 1}: {step.Marker} did not appear within {step.Timeout.TotalSeconds:0} s.");
                await run.FinishAsync(run.ElapsedMilliseconds, captureFile);
                return MarkerNeverAppeared;
            }

            position = found.Value;
            await Task.Delay(timings.Settle);
            await run.SendAsync(step.Keys);
            await Console.Out.WriteLineAsync($"t={run.ElapsedMilliseconds}ms step {index + 1}: saw {step.Marker}, sent {JsonSerializer.Serialize(step.Keys)}");
        }

        return await run.FinishAsync(run.ElapsedMilliseconds + (long)timings.Grace.TotalMilliseconds, captureFile) ? 0 : 1;
    }

    /// <summary>Polls the capture until the marker appears after the position, the step's time runs out, or the program exits.</summary>
    private static async Task<int?> WaitForAsync(CapturedRun run, SmokeStep step, int position)
    {
        long deadline = run.ElapsedMilliseconds + (long)step.Timeout.TotalMilliseconds;
        while (run.ElapsedMilliseconds < deadline)
        {
            bool exited = run.HasExited;
            if (step.Marker.FindAfter(run.Captured(), position) is int found)
            {
                return found;
            }

            if (exited)
            {
                return null;
            }

            await Task.Delay(100);
        }

        return null;
    }
}