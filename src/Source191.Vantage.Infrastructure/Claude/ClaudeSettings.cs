using Source191.Vantage.Ports;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Source191.Vantage.Adapters.Claude;

/// <summary>
/// The one setting Vantage touches in <c>~\.claude\settings.json</c>. The file is parsed, the property set in place
/// (so every other key keeps its position and value) and written back with two-space indentation, bare LF line endings
/// and the trailing newline it had, as Claude Code writes it. A file that is not a JSON object is never rewritten.
/// </summary>
public sealed class ClaudeSettings(string claudeHome) : IClaudeSettings
{
    private const string Property = "preferredNotifChannel";
    private const string TerminalBell = "terminal_bell";

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = true,
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static ClaudeSettings ForCurrentUser() => new ClaudeSettings(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude"));

    public string Location => Path.Combine(claudeHome, "settings.json");

    public async Task<NotificationChannelState> ReadNotificationChannelAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(Location))
        {
            return new NotificationChannelState.Missing();
        }

        try
        {
            JsonObject settings = Parse(await File.ReadAllTextAsync(Location, cancellationToken));

            return settings[Property] is JsonNode value ? new NotificationChannelState.Configured(value.ToString()) : new NotificationChannelState.Unset();
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            // The system's own words report what failed; they are shown as the reason and never acted on.
            return new NotificationChannelState.Unreadable(exception.Message);
        }
    }

    public async Task EnableTerminalBellAsync(CancellationToken cancellationToken)
    {
        string text = File.Exists(Location) ? await File.ReadAllTextAsync(Location, cancellationToken) : "{}\n";
        JsonObject settings = ParseOrThrowIoException(text);

        settings[Property] = TerminalBell;
        Directory.CreateDirectory(claudeHome);

        // Written beside the file and moved over it, so a crash or a full disk cannot leave settings.json truncated.
        string temporary = Location + ".vantage-tmp";
        await File.WriteAllBytesAsync(temporary, Serialise(settings, text.EndsWith('\n')), cancellationToken);
        File.Move(temporary, Location, overwrite: true);
    }

    private static JsonObject Parse(string text)
    {
        try
        {
            JsonObject settings = JsonNode.Parse(text) as JsonObject ?? throw new JsonException("the top level is not a JSON object");

            // JsonObject builds its key table on first lookup and a repeated key only fails then, so look up now.
            _ = settings[Property];

            return settings;
        }
        catch (ArgumentException exception)
        {
            // A repeated key is thrown as an ArgumentException; to the caller it is the same as any other bad JSON.
            throw new JsonException(exception.Message, exception);
        }
    }

    private JsonObject ParseOrThrowIoException(string text)
    {
        try
        {
            return Parse(text);
        }
        catch (JsonException exception)
        {
            throw new IOException($"{Location} could not be read ({exception.Message}), so it was left alone.", exception);
        }
    }

    private static byte[] Serialise(JsonObject settings, bool trailingNewline)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream, WriterOptions))
        {
            settings.WriteTo(writer);
        }

        string json = Utf8.GetString(stream.ToArray());

        return Utf8.GetBytes(trailingNewline ? json + "\n" : json);
    }
}