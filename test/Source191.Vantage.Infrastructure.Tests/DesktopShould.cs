using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>Starts a real PowerShell child to see what a started program inherits and whether the call waits for it.</summary>
[Trait("Category", "Integration")]
public sealed class DesktopShould : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "vantage-desktop-" + Guid.NewGuid().ToString("N"));
    private readonly string? previousMarker = Environment.GetEnvironmentVariable("CLAUDECODE");

    public DesktopShould()
    {
        Directory.CreateDirectory(folder);
        Environment.SetEnvironmentVariable("CLAUDECODE", "1");
    }

    [Fact]
    public async Task StartAProgramWithoutWaitingOrClaudeMarkers()
    {
        string report = Path.Combine(folder, "environment.txt");
        string script = $"Start-Sleep -Milliseconds 1500; Get-ChildItem env: | ForEach-Object Name | Set-Content '{report}'";

        await new Desktop().StartAsync(new ProcessRequest("pwsh", ["-NoProfile", "-Command", script], WorkingDirectory: null), TestContext.Current.CancellationToken);

        File.Exists(report).ShouldBeFalse("the call returned before the child had finished");
        await WaitForAsync(report);
        string[] names = await File.ReadAllLinesAsync(report, TestContext.Current.CancellationToken);
        names.ShouldNotContain("CLAUDECODE");
        names.Select(name => name.ToUpperInvariant()).ShouldContain("PATH");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CLAUDECODE", previousMarker);
        Directory.Delete(folder, recursive: true);
    }

    private static async Task WaitForAsync(string file)
    {
        for (int attempt = 0; attempt < 100 && !File.Exists(file); attempt++)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        File.Exists(file).ShouldBeTrue("the child never wrote its environment");
        await Task.Delay(200, TestContext.Current.CancellationToken);
    }
}