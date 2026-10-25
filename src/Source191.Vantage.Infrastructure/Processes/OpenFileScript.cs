using System.Text;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// The PowerShell a cleaned child runs to hand a file or URL to Windows. ShellExecute starts the associated application
/// with the environment its caller was created with, which cannot be set on a ShellExecute start, so the call is made from
/// a child that never had the Claude Code markers. The target enters the script only as Base64 of its UTF-8 bytes, an
/// alphabet with no character any shell reads specially, so no path or URL needs quoting.
/// </summary>
public static class OpenFileScript
{
    public static string For(string target)
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(target));

        return string.Join('\n',
            "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8",
            "try {",
            $"    $target = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{encoded}'))",
            "    $info = [System.Diagnostics.ProcessStartInfo]::new($target)",
            "    $info.UseShellExecute = $true",
            "    [System.Diagnostics.Process]::Start($info) | Out-Null",
            "} catch {",
            "    [Console]::Error.WriteLine($_.Exception.Message)",
            "    exit 1",
            "}");
    }
}