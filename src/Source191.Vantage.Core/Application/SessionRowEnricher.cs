using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// Turns listed sessions into table rows by asking git about each working directory (cached, because that is
/// three processes per directory) and checking for live tabs.
/// </summary>
public sealed class SessionRowEnricher(IGit git, ITabRegistry tabs, IProcessRegistry processes, TimeProvider clock, ITranscripts transcripts)
{
    private static readonly TimeSpan CacheWindow = TimeSpan.FromSeconds(30);

    // Refreshes can overlap (the timer and the refresh after an action), so the cache must take concurrent writes.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<FolderPath, (DateTimeOffset CachedAt, Location Location)> locations = new();

    public async Task<IReadOnlyList<SessionRow>> EnrichAsync(IReadOnlyList<Session> sessions, CancellationToken cancellationToken)
    {
        HashSet<SessionId> sessionsWithLiveTabs = await LiveTabSessionsAsync();

        List<SessionRow> rows = [];
        foreach (Session session in sessions)
        {
            Location location = await LocateAsync(session.WorkingDirectory, cancellationToken);
            rows.Add(new SessionRow(
                session,
                location.Repository,
                location.Branch,
                location.IsWorktree,
                sessionsWithLiveTabs.Contains(session.SessionId),
                transcripts.HasMessages(session.SessionId.Value)));
        }

        return rows;
    }

    private async Task<HashSet<SessionId>> LiveTabSessionsAsync()
    {
        IReadOnlyList<TabRegistration> all = await tabs.ReadTabsAsync();
        IReadOnlyList<TabRegistration> live = new TabRegistrations(all).Live(clock.GetUtcNow(), processes);
        foreach (TabRegistration stale in all.Except(live))
        {
            await tabs.RemoveTabAsync(stale.Id);
        }

        return [.. live.Select(registration => registration.Owner).OfType<TabOwner.ForSession>().Select(owner => owner.Id)];
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
            return new Location(directory.Name, Branch: null, IsWorktree: false);
        }

        string? main = await git.MainCheckoutOfAsync(directory.Value, cancellationToken);
        string? branch = await git.CurrentBranchAsync(directory.Value, cancellationToken);
        FolderPath? mainCheckout = FolderPath.TryParse(main);
        bool isWorktree = mainCheckout is not null && mainCheckout != FolderPath.TryParse(root);

        return new Location((mainCheckout ?? FolderPath.TryParse(root))?.Name ?? root, BranchName.TryParse(branch), isWorktree);
    }

    private sealed record Location(string Repository, BranchName? Branch, bool IsWorktree);
}