using Source191.Vantage.Ports;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>
/// Vantage's Windows Terminal JSON fragment at <c>&lt;Fragments root&gt;\Vantage\vantage.json</c>. Windows Terminal reads
/// every fragment at start-up and adds the profiles in it; the user's own settings file is never opened. Written as
/// UTF-8 without a byte order mark, as fragments must be.
/// </summary>
public sealed class TerminalFragment(string fragmentsRoot) : ITerminalFragment
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static TerminalFragment ForCurrentUser() => new TerminalFragment(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows Terminal", "Fragments"));

    private string FragmentFolder => Path.Combine(fragmentsRoot, "Vantage");

    public string Location => Path.Combine(FragmentFolder, "vantage.json");

    public bool Exists => File.Exists(Location);

    public async Task WriteAsync(TerminalProfiles profiles, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(FragmentFolder);
        JsonObject fragment = new()
        {
            ["profiles"] = new JsonArray(
                Profile(TerminalProfiles.VantageProfileName, $"\"{profiles.VantageExecutable}\"", profiles.VantageSound),
                Profile(TerminalProfiles.SessionProfileName, TerminalProfiles.SessionCommandLine, profiles.SessionSound)),
        };

        await File.WriteAllTextAsync(Location, fragment.ToJsonString(Options), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
    }

    public Task<bool> RemoveAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(FragmentFolder))
        {
            return Task.FromResult(false);
        }

        Directory.Delete(FragmentFolder, recursive: true);

        return Task.FromResult(true);
    }

    private static JsonObject Profile(string name, string commandLine, string? sound)
    {
        JsonObject profile = new()
        {
            ["name"] = name,
            ["commandline"] = commandLine,
            ["bellStyle"] = new JsonArray([.. TerminalProfiles.BellStyle.Select(style => (JsonNode?)JsonValue.Create(style))]),
        };
        if (sound is not null)
        {
            profile["bellSound"] = sound;
        }

        return profile;
    }
}