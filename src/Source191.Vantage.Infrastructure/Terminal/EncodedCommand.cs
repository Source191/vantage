using System.Text;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>
/// The <see href="https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_pwsh">-EncodedCommand
/// form PowerShell takes</see>: the script as Base64 of its UTF-16 bytes.
/// </summary>
public static class EncodedCommand
{
    public static string Encode(string script) => Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
}