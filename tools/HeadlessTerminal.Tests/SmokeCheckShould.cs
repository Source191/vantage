using System.Runtime.Versioning;

namespace Source191.Vantage.HeadlessTerminal.Tests;

[SupportedOSPlatform("windows")]
public sealed class SmokeCheckShould : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("smoke-check-").FullName;

    [Fact]
    public async Task SayItCouldNotStartWhenThereIsNoExe()
    {
        SmokeCheck check = new(new StepRunner(SmokeCheck.Options, StepTimings.Default, Path.Combine(folder, "drive-raw.txt")));

        int result = await check.RunAsync(Path.Combine(folder, "vantage.exe"), "Northwind.Smoke-smoke");

        result.ShouldBe(SmokeCheck.CouldNotStart);
        File.Exists(Path.Combine(folder, "drive-raw.txt")).ShouldBeFalse();
    }

    [Fact]
    public void PretendToBeWindowsTerminalAndAnswerItsQueriesAsVantageExpects()
    {
        SmokeCheck.Options.ShouldBe(new DriverOptions(PretendWindowsTerminal: true, AnswerQueries: true, XtversionDelayMs: 0));
    }

    public void Dispose() => Directory.Delete(folder, recursive: true);
}