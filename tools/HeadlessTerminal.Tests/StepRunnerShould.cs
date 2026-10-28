using System.Runtime.Versioning;

namespace Source191.Vantage.HeadlessTerminal.Tests;

/// <summary>Runs a short pwsh child in a real pseudo-console: it prints READY, waits for a key, sets the title DONE and lingers.</summary>
[SupportedOSPlatform("windows")]
public sealed class StepRunnerShould : IDisposable
{
    private static readonly DriverOptions Options = new(PretendWindowsTerminal: false, AnswerQueries: true, XtversionDelayMs: 0);
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(20);

    private readonly string folder = Directory.CreateTempSubdirectory("step-runner-").FullName;

    private string CaptureFile => Path.Combine(folder, "drive-raw.txt");

    [Fact]
    public async Task SendEachStepsKeysOnceItsMarkerAppearsAndPassWhenTheProgramExitsByItself()
    {
        StepRunner runner = new(Options, new StepTimings(Settle: TimeSpan.FromMilliseconds(100), Grace: TimeSpan.FromSeconds(10)), CaptureFile);

        int result = await runner.RunAsync("pwsh", [new SmokeStep(new CaptureMarker.ScreenText("READY"), "x", Wait), new SmokeStep(new CaptureMarker.Title("DONE"), string.Empty, Wait)], Child(lingerMs: 300));

        result.ShouldBe(0);
        (await File.ReadAllTextAsync(CaptureFile, TestContext.Current.CancellationToken)).ShouldContain("READY");
    }

    [Fact]
    public async Task EndTheRunWithMarkerNeverAppearedWhenAStepsMarkerDoesNotShowInTime()
    {
        StepRunner runner = new(Options, new StepTimings(Settle: TimeSpan.FromMilliseconds(100), Grace: TimeSpan.FromSeconds(10)), CaptureFile);

        int result = await runner.RunAsync("pwsh", [new SmokeStep(new CaptureMarker.ScreenText("NEVER"), "x", TimeSpan.FromSeconds(3))], Child(lingerMs: 300));

        result.ShouldBe(StepRunner.MarkerNeverAppeared);
        File.Exists(CaptureFile).ShouldBeTrue();
    }

    [Fact]
    public async Task KillAProgramThatDoesNotExitWithinTheGraceAfterTheLastStep()
    {
        StepRunner runner = new(Options, new StepTimings(Settle: TimeSpan.FromMilliseconds(100), Grace: TimeSpan.FromSeconds(1)), CaptureFile);

        int result = await runner.RunAsync("pwsh", [new SmokeStep(new CaptureMarker.ScreenText("READY"), "x", Wait)], Child(lingerMs: 30000));

        result.ShouldBe(1);
    }

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private static string[] Child(int lingerMs) =>
        ["-NoProfile", "-Command", $"Write-Host READY; [void][Console]::ReadKey($true); [Console]::Write([char]27 + ']0;DONE' + [char]7); Start-Sleep -Milliseconds {lingerMs}"];
}