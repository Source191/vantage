using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>Starts real child processes to see what a started or opened program inherits and whether the call waits for it.</summary>
[Trait("Category", "Integration")]
public sealed class DesktopShould : IDisposable
{
    private const string Pwsh = "pwsh.exe";

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

        await new Desktop(new ProcessRunner(), Pwsh).StartAsync(new ProcessRequest("pwsh", ["-NoProfile", "-Command", script], WorkingDirectory: null), TestContext.Current.CancellationToken);

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
        // The report is renamed into place once set has finished writing it, so a reader never sees half of it.
        await File.WriteAllTextAsync(file, $"@set > \"{report}.tmp\"\r\n@ren \"{report}.tmp\" \"{Path.GetFileName(report)}\"", TestContext.Current.CancellationToken);

        await new Desktop(new ProcessRunner(), Pwsh).OpenAsync(file, TestContext.Current.CancellationToken);

        await WaitForAsync(report);
        string[] lines = await File.ReadAllLinesAsync(report, TestContext.Current.CancellationToken);
        foreach (string marker in Markers)
        {
            lines.ShouldNotContain(line => line.StartsWith(marker + "=", StringComparison.OrdinalIgnoreCase), $"{marker} reached the opened program");
        }

        lines.ShouldContain(line => line.StartsWith("PATH=", StringComparison.OrdinalIgnoreCase));
        Environment.GetEnvironmentVariable("CLAUDECODE").ShouldBe("1", "Vantage's own environment is left as it was");
    }

    /// <summary>
    /// The opened program cannot finish until the test releases it, so a report before the release would mean the call
    /// had waited for it. The limits only bound a failure: if the call ever waited, the gate would never open and the
    /// cancellation would fail the test at 30 seconds instead of hanging it.
    /// </summary>
    [Fact]
    public async Task ReturnWithoutWaitingForTheOpenedProgramToEnd()
    {
        string file = Path.Combine(folder, "gated.cmd");
        string release = Path.Combine(folder, "release.txt");
        string report = Path.Combine(folder, "finished.txt");
        await File.WriteAllLinesAsync(file, GatedScript(release, report), TestContext.Current.CancellationToken);
        using CancellationTokenSource limit = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        limit.CancelAfter(TimeSpan.FromSeconds(30));

        await new Desktop(new ProcessRunner(), Pwsh).OpenAsync(file, limit.Token);

        File.Exists(report).ShouldBeFalse("the opened program finished before it was released");
        await File.WriteAllTextAsync(release, "go", TestContext.Current.CancellationToken);
        await WaitForAsync(report);
    }

    [Fact]
    public async Task ThrowWithTheReasonWhenTheFileIsGone()
    {
        string gone = Path.Combine(folder, "Northwind.Orders.slnx");

        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() => new Desktop(new ProcessRunner(), Pwsh).OpenAsync(gone, TestContext.Current.CancellationToken));

        failure.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task OpenThroughTheGivenPowerShell()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));

        await new Desktop(processes, @"C:\Program Files\PowerShell\7\pwsh.exe").OpenAsync(@"C:\Users\me\Northwind.Orders.slnx", TestContext.Current.CancellationToken);

        processes.CapturedRequests.Single().FileName.ShouldBe(@"C:\Program Files\PowerShell\7\pwsh.exe");
    }

    [Fact]
    public async Task ThrowNamingPwshWhenItIsNotOnThePath()
    {
        FakeProcessRunner processes = new();

        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() => new Desktop(processes, powerShell: null).OpenAsync(@"C:\Users\me\Northwind.Orders.slnx", TestContext.Current.CancellationToken));

        failure.Message.ShouldContain("pwsh.exe");
        processes.CapturedRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task PassTheChildsReasonOn()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(1, string.Empty, "Cannot find the file.\r\n"));

        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() => new Desktop(processes, Pwsh).OpenAsync(@"C:\Users\me\Northwind.Orders.slnx", TestContext.Current.CancellationToken));

        failure.Message.ShouldBe("Cannot find the file.");
    }

    [Fact]
    public async Task NameTheExitCodeWhenTheChildSaysNothing()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(5, string.Empty, string.Empty));

        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() => new Desktop(processes, Pwsh).OpenAsync(@"C:\Users\me\Northwind.Orders.slnx", TestContext.Current.CancellationToken));

        failure.Message.ShouldContain("5");
    }

    public void Dispose()
    {
        foreach ((string name, string? value) in previousMarkers)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        Directory.Delete(folder, recursive: true);
    }

    /// <summary>A .cmd that waits for the release file, then writes the report; it ends early once the test's folder has gone, so a failed test leaves nothing running.</summary>
    private string[] GatedScript(string release, string report)
    {
        return
        [
            "@echo off",
            ":wait",
            $"if exist \"{release}\" goto done",
            $"if not exist \"{folder}\" exit /b",
            "ping -n 2 127.0.0.1 > nul",
            "goto wait",
            ":done",
            $"echo done > \"{report}\"",
        ];
    }

    /// <summary>Returns as soon as the file is there; the 30 seconds only bound a child that never writes it, on however slow a runner.</summary>
    private static async Task WaitForAsync(string file)
    {
        for (int attempt = 0; attempt < 300 && !File.Exists(file); attempt++)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        File.Exists(file).ShouldBeTrue($"{Path.GetFileName(file)} never appeared within 30 seconds");
        await Task.Delay(200, TestContext.Current.CancellationToken);
    }
}