using System.Diagnostics;
using System.Runtime.Versioning;

namespace Source191.Vantage.HeadlessTerminal.Tests;

/// <summary>Runs the real driver as a child with its stdout piped, as CI does, and reads the capture it wrote.</summary>
[SupportedOSPlatform("windows")]
public sealed class PseudoConsoleShould
{
    [Fact]
    public async Task CaptureWhatTheChildAndItsChildWriteWhenTheDriversOwnStdoutIsPiped()
    {
        string folder = Directory.CreateTempSubdirectory("pseudo-console-").FullName;
        try
        {
            string driverStdout = await RunDriverAsync(folder);

            string capture = await File.ReadAllTextAsync(Path.Combine(folder, "drive-raw.txt"), TestContext.Current.CancellationToken);
            capture.ShouldContain("NETOUT");
            capture.ShouldContain("GRANDCHILD");
            // The driver echoes the capture's last 300 characters itself; a marker on any other line is a leak.
            string leaked = string.Join(Environment.NewLine, driverStdout.Split(Environment.NewLine).Where(line => !line.StartsWith("last 300 plain chars", StringComparison.Ordinal)));
            leaked.ShouldNotContain("NETOUT");
            leaked.ShouldNotContain("GRANDCHILD");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static async Task<string> RunDriverAsync(string workingDirectory)
    {
        string driver = Path.Combine(AppContext.BaseDirectory, "headless-terminal.dll");
        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        string[] arguments = [driver, "drive", "pwsh", "500:\\r", "-NoProfile", "-Command", "[Console]::Out.WriteLine('NETOUT'); cmd /c echo GRANDCHILD; Start-Sleep 1"];
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)!;
        string output = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        return output;
    }
}