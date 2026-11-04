using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application;

/// <summary>
/// Every tab Torni opens, built in one place: the script it runs, its title, folder and colour. Values reach the
/// script only through <see cref="PowerShellScript.Quote"/>, and the two executable paths live here, nowhere else.
/// </summary>
public sealed class TabScripts(string claudeExecutable, string torniExecutable)
{
    private static readonly TabColour TrustColour = TabPalette.Green;

    /// <summary>The last palette colour, so a plain shell never looks like a session.</summary>
    private static readonly TabColour ShellColour = TabPalette.Last;

    /// <summary>Torni's winget package, exactly, from the winget source alone: the package the check followed.</summary>
    private static readonly string[] WingetPackage = ["--id", UpdateCheck.PackageId, "--exact", "--source", UpdateCheck.Source];

    /// <summary>
    /// A session's tab: the shell claims the registration with its own $PID (so it outlives the short-lived claim-tab
    /// process), clears what the profile printed, then attaches full screen. A detach, a stop or a removal ends the attach
    /// with 0 (measured with CLI 2.1.312), so the shell exits and Windows Terminal closes the tab; a failed attach keeps the prompt.
    /// </summary>
    public TabLaunch Attach(Session.Background session, TabRegistration registration) => new(
        Wording.Name(session.Name),
        registration.Colour,
        session.WorkingDirectory,
        string.Join('\n',
            $"{PowerShellScript.Invoke(torniExecutable, ["claim-tab", registration.Id.ToString("N")])} $PID",
            "Clear-Host",
            PowerShellScript.Invoke(claudeExecutable, ["attach", session.JobId.Value]),
            "if ($LASTEXITCODE -eq 0) { exit }"));

    /// <summary>
    /// A plain Claude tab in an untrusted folder, so the trust prompt can be accepted interactively. The folder name is
    /// user text, so the whole message is one quoted value, never script source.
    /// </summary>
    public TabLaunch Trust(FolderPath directory) => new(
        Wording.TrustTabTitle(directory.Name),
        TrustColour,
        directory,
        string.Join('\n',
            $"Write-Host {PowerShellScript.Quote(Wording.TrustTabMessage(directory.Name))} -ForegroundColor Cyan",
            PowerShellScript.Invoke(claudeExecutable, [])));

    /// <summary>
    /// The update tab: waits for the Torni that opened it to exit (a running torni.exe locks its files), upgrades the
    /// winget package from the winget source alone (the one the check followed), then starts Torni again only when the
    /// upgrade succeeded; otherwise winget's output stays on screen with a hint.
    /// </summary>
    public TabLaunch Update(int torniProcessId, FolderPath startingDirectory, string winget) => new(
        Wording.UpdateTabTitle,
        ShellColour,
        startingDirectory,
        string.Join('\n',
            WaitFor(torniProcessId),
            PowerShellScript.Invoke(winget, ["upgrade", .. WingetPackage]),
            $"if ($LASTEXITCODE -eq 0) {{ {PowerShellScript.Invoke(torniExecutable, [])} }} else {{ Write-Host {PowerShellScript.Quote(Wording.UpdateFailedHint)} -ForegroundColor Yellow }}"));

    /// <summary>A plain PowerShell tab in a worktree.</summary>
    public static TabLaunch Shell(FolderPath worktree) => new(worktree.Name, ShellColour, worktree, "Clear-Host");

    private static string WaitFor(int processId) =>
        $"Wait-Process -Id {PowerShellScript.Quote(processId.ToString(System.Globalization.CultureInfo.InvariantCulture))} -ErrorAction SilentlyContinue";
}