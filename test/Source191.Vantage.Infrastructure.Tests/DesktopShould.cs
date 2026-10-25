using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>Starts real child processes to see what a started or opened program inherits and whether the call waits for it.</summary>
[Trait("Category", "Integration")]
public sealed class DesktopShould : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "vantage-desktop-" + Guid.NewGuid().ToString("N"));
    private static readonly string[] Markers = ["CLAUDECODE", "NO_COLOR", "CLAUDE_PID", "CLAUDE_CODE_SESSION_ID"];

    private readonly Dictionary<string, string?> previousMarkers = Markers.ToDictionary(name => name, name => Environment.GetEnvironmentVariable(name));

    public DesktopShould()
    {
        Directory.CreateDirectory(folder);
        foreach (string name in Markers)
        {
            Environment.SetEnvironmentVariable(name, "1");
        }
    }

    [Fact]
    public async Task StartAProgramWithoutWaitingOrClaudeMarkers()
    {
        string report = Path.Combine(folder, "environment.txt");
        string script = $"Start-Sleep -Milliseconds 1500; Get-ChildItem env: | ForEach-Object Name | Set-Content '{report}'";

        await new Desktop(new ProcessRunner()).StartAsync(new ProcessRequest("pwsh", ["-NoProfile", "-Command", script], WorkingDirectory: null), TestContext.Current.CancellationToken);

        File.Exists(report).ShouldBeFalse("the call returned before the child had finished");
        await WaitForAsync(report);
        string[] names = await File.ReadAllLinesAsync(report, TestContext.Current.CancellationToken);
        names.ShouldNotContain("CLAUDECODE");
        names.Select(name => name.ToUpperInvariant()).ShouldContain("PATH");
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("Northwind & Co's tools")]
    [InlineData("100% ^ (a) !x! ;,=")]
    [InlineData("[1] é☃")]
    public async Task OpenAFileWithoutClaudeMarkers(string name)
    {
        string report = Path.Combine(folder, "environment.txt");
        string file = Path.Combine(folder, name + ".cmd");
        await File.WriteAllTextAsync(file, $"@set > \"{report}\"", TestContext.Current.CancellationToken);

        await new Desktop(new ProcessRunner()).OpenAsync(file, TestContext.Current.CancellationToken);

        await WaitForAsync(report);
        string[] lines = await File.ReadAllLinesAsync(report, TestContext.Current.CancellationToken);
        foreach (string marker in Markers)
        {
            lines.ShouldNotContain(line => line.StartsWith(marker + "=", StringComparison.OrdinalIgnoreCase), $"{marker} reached the opened program");
        }

        lines.ShouldContain(line => line.StartsWith("PATH=", StringComparison.OrdinalIgnoreCase));
        Environment.GetEnvironmentVariable("CLAUDECODE").ShouldBe("1", "Vantage's own environment is left as it was");
    }

    [Fact]
    public async Task ThrowWithTheReasonWhenTheFileIsGone()
    {
        string gone = Path.Combine(folder, "Northwind.Orders.slnx");

        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() => new Desktop(new ProcessRunner()).OpenAsync(gone, TestContext.Current.CancellationToken));

        failure.Message.ShouldNotBeNullOrWhiteSpace();
    }

    public void Dispose()
    {
        foreach ((string name, string? value) in previousMarkers)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

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