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
/// The icon source is the tool's own icon, which a build without one simply lacks.
/// </summary>
public sealed class TerminalFragment(string fragmentsRoot, string iconSource) : ITerminalFragment
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static TerminalFragment ForCurrentUser(string iconSource) => new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows Terminal", "Fragments"), iconSource);

    private string FragmentFolder => Path.Combine(fragmentsRoot, "Vantage");

    public string Location => Path.Combine(FragmentFolder, "vantage.json");

    private string IconCopy => Path.Combine(FragmentFolder, "vantage.ico");

    public bool Exists => File.Exists(Location);

    public async Task WriteAsync(TerminalProfiles profiles, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(FragmentFolder);
        bool withIcon = TryCopyIcon();
        JsonObject fragment = new()
        {
            ["profiles"] = new JsonArray(
                Profile(TerminalProfiles.VantageProfileName, $"\"{profiles.VantageExecutable}\"", profiles.VantageSound, withIcon),
                Profile(TerminalProfiles.SessionProfileName, TerminalProfiles.SessionCommandLine, profiles.SessionSound, withIcon)),
        };

        await File.WriteAllTextAsync(Location, fragment.ToJsonString(Options), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
    }

    /// <summary>Copies the tool's icon beside the fragment, where it survives a tool update; false when the tool carries none.</summary>
    private bool TryCopyIcon()
    {
        if (!File.Exists(iconSource))
        {
            return false;
        }

        File.Copy(iconSource, IconCopy, overwrite: true);

        return true;
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

    private JsonObject Profile(string name, string commandLine, BellSound sound, bool withIcon)
    {
        JsonObject profile = new()
        {
            ["name"] = name,
            ["commandline"] = commandLine,
            ["bellStyle"] = new JsonArray([.. TerminalProfiles.BellStyle.Select(style => (JsonNode?)JsonValue.Create(style))]),
        };
        // Windows Terminal's own sound is its default, so it is written by leaving bellSound out.
        if (sound is BellSound.File file)
        {
            profile["bellSound"] = file.Path;
        }

        if (withIcon)
        {
            profile["icon"] = IconCopy;
        }

        return profile;
    }
}