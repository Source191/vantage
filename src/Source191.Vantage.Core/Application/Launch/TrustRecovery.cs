using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Launch;

/// <summary>
/// Claude Code refuses to start a background session in a directory whose workspace trust has not been accepted,
/// and trust can only be granted interactively. This opens a plain Claude tab there so it can be.
/// </summary>
public static class TrustRecovery
{
    public static TabLaunch TabFor(string directory, string claudeExecutable)
    {
        string name = new RepositoryPath(directory).Name;
        string script = string.Join('\n',
            $"Write-Host 'Vantage: accept the trust prompt for {name}, then exit Claude and press n in Vantage to retry.' -ForegroundColor Cyan",
            PowerShellScript.Invoke(claudeExecutable, []));

        return new TabLaunch($"Trust {name}", TabPalette.Default[2], directory, script);
    }

}