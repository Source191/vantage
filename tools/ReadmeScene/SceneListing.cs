using Source191.Torni.Domain;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Source191.Torni.ReadmeScene;

/// <summary>
/// What <c>claude agents --json --all</c> answers for the scene: its sessions and automated runs, each given a made-up
/// process id while it has one, and any session started since, listed as starting once and working after that.
/// </summary>
public sealed class SceneListing
{
    private const int FirstProcessId = 40_000;
    private const int ProcessIdStep = 4;

    private static readonly JsonSerializerOptions Options = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly Lock gate = new();
    private readonly TimeProvider clock;
    private readonly List<ListedSession> sessions;
    private readonly List<(SceneAutomatedRun Run, string Folder, string SessionId, int ProcessId)> automatedRuns;
    private readonly Dictionary<string, int> listingsSinceStart = [];
    private int nextProcessId;

    public SceneListing(Scene scene, SceneLayout layout, TimeProvider clock)
    {
        this.clock = clock;
        nextProcessId = FirstProcessId;
        sessions = [.. scene.Sessions.Select(session => new ListedSession(session, layout.FolderOf(session), SessionIdOf(session.Name), HasProcess(session.State) ? NextProcessId() : null))];
        automatedRuns = [.. scene.AutomatedRuns.Select((run, index) => (run, layout.Repository(run.Repository), SessionIdOf($"automated-{index}"), NextProcessId()))];
        ClaudeVersion = scene.ClaudeVersion;
    }

    public string ClaudeVersion { get; }

    public string Json()
    {
        lock (gate)
        {
            ListingRow[] rows = [.. sessions.Select(Row), .. automatedRuns.Select(run => new ListingRow(run.ProcessId, null, run.Folder, "interactive", run.Run.StartedAt(clock).ToUnixTimeMilliseconds(), run.SessionId, null, null, null, null))];
            foreach (string started in listingsSinceStart.Keys)
            {
                listingsSinceStart[started]++;
            }

            return JsonSerializer.Serialize(rows, Options);
        }
    }

    /// <summary>Lists a session started through <c>claude --bg -n</c> from the next listing on, and answers its job id.</summary>
    public string Start(string name, string folder)
    {
        lock (gate)
        {
            SceneSession session = new(name, Path.GetFileName(folder), SceneKind.Background, SceneState.Starting, 0);
            ListedSession listed = new(session, folder, SessionIdOf(name), NextProcessId());
            sessions.Insert(0, listed);
            listingsSinceStart[listed.SessionId] = 0;

            return JobIdOf(listed.SessionId);
        }
    }

    /// <summary>When a scene process started and what it uses, or null for a process the scene does not hold.</summary>
    public ProcessUsage? UsageOf(int processId)
    {
        lock (gate)
        {
            ListedSession? session = sessions.FirstOrDefault(listed => listed.ProcessId == processId);
            if (session is not null)
            {
                return new ProcessUsage(session.Session.StartedAt(clock), MemorySize.FromMegabytes(session.Session.MemoryMegabytes), "claude");
            }

            return automatedRuns.Where(run => run.ProcessId == processId)
                .Select(run => new ProcessUsage(run.Run.StartedAt(clock), MemorySize.FromMegabytes(run.Run.MemoryMegabytes), "claude"))
                .FirstOrDefault();
        }
    }

    /// <summary>Every listed session's id, for the transcript files that let Enter open a tab for it.</summary>
    public IReadOnlyList<string> SessionIds()
    {
        lock (gate)
        {
            return [.. sessions.Select(session => session.SessionId)];
        }
    }

    /// <summary>Every process id the scene holds.</summary>
    public IReadOnlyList<int> ProcessIds()
    {
        lock (gate)
        {
            return [.. sessions.Select(session => session.ProcessId).OfType<int>(), .. automatedRuns.Select(run => run.ProcessId)];
        }
    }

    private ListingRow Row(ListedSession listed)
    {
        SceneSession session = listed.Session;
        SceneState state = listingsSinceStart.TryGetValue(listed.SessionId, out int listings) && listings > 0 ? SceneState.Working : session.State;
        (string? status, string? claudeState) = Words(state);
        bool interactive = session.Kind == SceneKind.Interactive;

        return new ListingRow(
            listed.ProcessId,
            interactive ? null : JobIdOf(listed.SessionId),
            listed.Folder,
            interactive ? "interactive" : "background",
            session.StartedAt(clock).ToUnixTimeMilliseconds(),
            listed.SessionId,
            session.Name,
            status,
            claudeState,
            session.WaitingFor.Length == 0 ? null : session.WaitingFor);
    }

    // The listing's status and state for each scene state, as ClaudeStates reads them.
    private static (string? Status, string? State) Words(SceneState state) => state switch
    {
        SceneState.Starting => (null, null),
        SceneState.Working => ("busy", "working"),
        SceneState.Waiting => ("waiting", "blocked"),
        SceneState.Idle => ("idle", "working"),
        SceneState.Done => ("idle", "done"),
        SceneState.Failed => (null, "failed"),
        SceneState.Stopped => (null, "stopped"),
    };

    private static bool HasProcess(SceneState state) => state is not (SceneState.Failed or SceneState.Stopped);

    private int NextProcessId()
    {
        int processId = nextProcessId;
        nextProcessId += ProcessIdStep;

        return processId;
    }

    private static string SessionIdOf(string name) => new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(name)).AsSpan(0, 16)).ToString("D");

    private static string JobIdOf(string sessionId) => sessionId[..8];
}