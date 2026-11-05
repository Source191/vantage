using Source191.Torni.Domain;
using Source191.Torni.Ports;
namespace Source191.Torni.Adapters.Claude;

/// <summary>
/// Looks for <c>~\.claude\projects\&lt;project&gt;\&lt;sessionId&gt;.jsonl</c>. The project folder name encodes the
/// working directory, so every project folder is checked rather than reproducing that encoding.
/// </summary>
public sealed class ClaudeTranscripts(string claudeHome) : ITranscripts
{
    public static ClaudeTranscripts ForCurrentUser() => new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude"));

    public bool HasMessages(SessionId sessionId)
    {
        string projects = Path.Combine(claudeHome, "projects");
        if (!Directory.Exists(projects))
        {
            return false;
        }

        return Directory.EnumerateDirectories(projects)
            .Any(project => File.Exists(Path.Combine(project, $"{sessionId.Value}.jsonl")));
    }
}