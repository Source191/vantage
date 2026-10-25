using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// Every tab Vantage opens, built in one place: the script it runs, its title, folder and colour. Values reach the
/// script only through <see cref="PowerShellScript.Quote"/>, and the two executable paths live here, nowhere else.
/// </summary>
public sealed class TabScripts(string claudeExecutable, string vantageExecutable)
{
    private static readonly TabColour TrustColour = TabPalette.Default[2];

    /// <summary>The last palette colour, so a plain shell never looks like a session.</summary>
    private static readonly TabColour ShellColour = TabPalette.Default[^1];

    /// <summary>
    /// A session's tab: the shell claims the registration with its own $PID (so it outlives the short-lived claim-tab
    /// process), clears what the profile printed, then attaches full screen.
    /// </summary>
    public TabLaunch Attach(BackgroundSession session, TabRegistration registration) => new TabLaunch(
        session.Name,
        registration.Colour,
        session.WorkingDirectory,
        string.Join('\n',
            $"{PowerShellScript.Invoke(vantageExecutable, ["claim-tab", registration.Id.ToString("N")])} $PID",
            "Clear-Host",
            PowerShellScript.Invoke(claudeExecutable, ["attach", session.JobId.Value])));

    /// <summary>
    /// A plain Claude tab in an untrusted folder, so the trust prompt can be accepted interactively. The folder name is
    /// user text, so the whole message is one quoted value, never script source.
    /// </summary>
    public TabLaunch Trust(FolderPath directory) => new TabLaunch(
        $"Trust {directory.Name}",
        TrustColour,
        directory,
        string.Join('\n',
            $"Write-Host {PowerShellScript.Quote($"Vantage: accept the trust prompt for {directory.Name}, then exit Claude and press n in Vantage to retry.")} -ForegroundColor Cyan",
            PowerShellScript.Invoke(claudeExecutable, [])));

    /// <summary>
    /// The update tab: waits for the Vantage that opened it to exit (a running vantage.exe locks its files), updates the
    /// tool, then starts Vantage again only when the update succeeded; otherwise dotnet's output stays on screen with a hint.
    /// </summary>
    public TabLaunch Update(int vantageProcessId, FolderPath startingDirectory) => new TabLaunch(
        "Update Vantage",
        ShellColour,
        startingDirectory,
        string.Join('\n',
            $"Wait-Process -Id {PowerShellScript.Quote(vantageProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture))} -ErrorAction SilentlyContinue",
            PowerShellScript.Invoke("dotnet", ["tool", "update", "-g", UpdateCheck.PackageId]),
            $"if ($LASTEXITCODE -eq 0) {{ {PowerShellScript.Invoke(vantageExecutable, [])} }} else {{ Write-Host {PowerShellScript.Quote(Wording.UpdateFailedHint)} -ForegroundColor Yellow }}"));

    /// <summary>A plain PowerShell tab in a worktree.</summary>
    public static TabLaunch Shell(FolderPath worktree) => new TabLaunch(worktree.Name, ShellColour, worktree, "Clear-Host");
}