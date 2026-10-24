using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Screens;

/// <summary>
/// Turns listed sessions into table rows by asking git about each working directory (cached, because that is
/// three processes per directory), reading descriptions from the state store, and checking for live tabs.
/// </summary>
public sealed class SessionRowEnricher(IGit git, StateStore store, IProcessRegistry processes, TimeProvider clock, ITranscripts transcripts)
{
    private static readonly TimeSpan CacheWindow = TimeSpan.FromSeconds(30);

    private readonly Dictionary<string, (DateTimeOffset CachedAt, Location Location)> locations = new(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<SessionRow>> EnrichAsync(IReadOnlyList<Session> sessions, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> descriptions = await store.ReadDescriptionsAsync();
        HashSet<string> sessionsWithLiveTabs = await LiveTabSessionsAsync();

        List<SessionRow> rows = [];
        foreach (Session session in sessions)
        {
            Location location = await LocateAsync(session.WorkingDirectory, cancellationToken);
            rows.Add(new SessionRow(
                session,
                location.Repository,
                location.Branch,
                location.IsWorktree,
                descriptions.GetValueOrDefault(session.SessionId),
                sessionsWithLiveTabs.Contains(session.SessionId),
                transcripts.HasMessages(session.SessionId)));
        }

        return rows;
    }

    private async Task<HashSet<string>> LiveTabSessionsAsync()
    {
        IReadOnlyList<TabRegistration> all = await store.ReadTabsAsync();
        IReadOnlyList<TabRegistration> live = new TabRegistrations(all).Live(clock.GetUtcNow(), processes);
        foreach (TabRegistration stale in all.Except(live))
        {
            await store.RemoveTabAsync(stale.Id);
        }

        return [.. live.Select(registration => registration.SessionId)];
    }

    private async Task<Location> LocateAsync(string directory, CancellationToken cancellationToken)
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

    private async Task<Location> AskGitAsync(string directory, CancellationToken cancellationToken)
    {
        string? root = await git.RepositoryRootAsync(directory, cancellationToken);
        if (root is null)
        {
            return new Location(new RepositoryPath(directory).Name, Branch: null, IsWorktree: false);
        }

        string? main = await git.MainCheckoutOfAsync(directory, cancellationToken);
        string? branch = await git.CurrentBranchAsync(directory, cancellationToken);
        bool isWorktree = main is not null && !string.Equals(main, root, StringComparison.OrdinalIgnoreCase);

        return new Location(new RepositoryPath(main ?? root).Name, branch, isWorktree);
    }

    private sealed record Location(string Repository, string? Branch, bool IsWorktree);
}