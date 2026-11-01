using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Application;

/// <summary>
/// Turns listed sessions into table rows by asking git about each working directory (cached, because that is
/// three processes per directory) and checking for live tabs.
/// </summary>
public sealed class SessionRowEnricher(IGit git, ITabRegistry tabs, IProcessRegistry processes, TimeProvider clock, ITranscripts transcripts, PullRequestCatalogue pullRequests)
{
    private static readonly TimeSpan CacheWindow = TimeSpan.FromSeconds(30);

    // Refreshes can overlap (the timer and the refresh after an action), so the cache must take concurrent writes.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<FolderPath, (DateTimeOffset CachedAt, Location Location)> locations = new();

    public async Task<IReadOnlyList<SessionRow>> EnrichAsync(IReadOnlyList<Session> sessions, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<SessionId, TabColour> liveTabs = await LiveTabsAsync();
        IReadOnlyList<ProcessFacts> processTable = processes.Snapshot();

        List<SessionRow> rows = [];
        List<(FolderPath Checkout, BranchName Branch)> branches = [];
        foreach (Session session in sessions)
        {
            Location location = await LocateAsync(session.WorkingDirectory, cancellationToken);
            LiveTab tab = liveTabs.TryGetValue(session.SessionId, out TabColour? colour) ? new LiveTab.Open(colour) : new LiveTab.None();
            SessionRow row = new(session, location.Repository, location.Head, tab, transcripts.HasMessages(session.SessionId.Value))
            {
                Memory = MemoryOf(session, processTable),
                Checkout = location.Checkout,
            };
            rows.Add(location.Checkout is null ? row : row with { PullRequest = pullRequests.Find(location.Checkout, location.Head) });
            branches.AddRange(location is { Checkout: FolderPath checkout, Head: Head.OnBranch on } ? [(checkout, on.Branch)] : []);
        }

        // Never awaited: the rows show what the catalogue already holds, and a later refresh picks up what lands.
        _ = pullRequests.RefreshStaleAsync(branches);

        return rows;
    }

    /// <summary>How many runs there are and their trees' memory together, measured as a session's memory is; a run whose process has gone adds nothing.</summary>
    public AutomatedRuns Measure(IReadOnlyList<AutomatedRun> runs)
    {
        IReadOnlyList<ProcessFacts> processTable = processes.Snapshot();
        MemorySize memory = runs
            .Select(run => ProcessTree.Under(run.ProcessId, processTable, processes.UsageOf) is ProcessTreeLookup.Found found ? found.Tree.Total : MemorySize.Zero)
            .Aggregate(MemorySize.Zero, (sum, size) => sum + size);

        return new AutomatedRuns(runs.Count, memory);
    }

    /// <summary>The private working set of the session's process tree; nothing for a session without a process or whose process has gone.</summary>
    private SessionMemory MemoryOf(Session session, IReadOnlyList<ProcessFacts> processTable)
    {
        int? processId = session switch
        {
            BackgroundSession background => background.ProcessId,
            InteractiveSession interactive => interactive.ProcessId,
            _ => throw new UnreachableException(),
        };

        return processId is int root && ProcessTree.Under(root, processTable, processes.UsageOf) is ProcessTreeLookup.Found found ? new SessionMemory.Measured(found.Tree.Total) : new SessionMemory.NothingToMeasure();
    }

    /// <summary>Each session with a live Vantage tab and that tab's colour; stale registrations are removed on the way.</summary>
    private async Task<IReadOnlyDictionary<SessionId, TabColour>> LiveTabsAsync()
    {
        IReadOnlyList<TabRegistration> all = await tabs.ReadTabsAsync();
        IReadOnlyList<TabRegistration> live = new TabRegistrations(all).Live(clock.GetUtcNow(), processes);
        foreach (TabRegistration stale in all.Except(live))
        {
            await tabs.RemoveTabAsync(stale.Id);
        }

        Dictionary<SessionId, TabColour> colours = [];
        foreach (TabRegistration registration in live)
        {
            if (registration.Owner is TabOwner.ForSession owner)
            {
                colours.TryAdd(owner.Id, registration.Colour);
            }
        }

        return colours;
    }

    private async Task<Location> LocateAsync(FolderPath directory, CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.GetUtcNow();
        if (locations.TryGetValue(directory, out (DateTimeOffset CachedAt, Location Location) cached) && now - cached.CachedAt < CacheWindow)
        {
            return cached.Location;
        }

        Location location = await AskGitAsync(directory, cancellationToken);
        locations[directory] = (now, location);

        return location;
    }

    private async Task<Location> AskGitAsync(FolderPath directory, CancellationToken cancellationToken)
    {
        string? root = await git.RepositoryRootAsync(directory.Value, cancellationToken);
        if (root is null)
        {
            return new Location(directory.Name, new Head.Unknown(), Checkout: null);
        }

        string? main = await git.MainCheckoutOfAsync(directory.Value, cancellationToken);
        string? branch = await git.CurrentBranchAsync(directory.Value, cancellationToken);
        FolderPath? mainCheckout = FolderPath.TryParse(main, out FolderPath? parsedMain) ? parsedMain : null;
        FolderPath? rootPath = FolderPath.TryParse(root, out FolderPath? parsedRoot) ? parsedRoot : null;
        Head head = BranchName.TryParse(branch, out BranchName? current) ? new Head.OnBranch(current) : new Head.Unknown();
        FolderPath? repository = mainCheckout ?? rootPath;

        return new Location(repository?.Name ?? root, head, repository);
    }

    /// <summary>Checkout is the main checkout (or the root when git names none), where the pull request lookup runs. Head is Unknown outside a repository and where git names no branch, since the port cannot tell a detached head from a failure.</summary>
    private sealed record Location(string Repository, Head Head, FolderPath? Checkout);
}