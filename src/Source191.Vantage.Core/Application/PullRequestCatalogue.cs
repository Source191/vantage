using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Collections.Concurrent;

namespace Source191.Vantage.Application;

/// <summary>
/// Each branch's pull requests, looked up per main checkout and branch and never waited for by a row: Find answers
/// from what has already landed (Looking before the first answer), and RefreshStaleAsync starts a lookup for each
/// branch whose answer is older than the window and has none running, keeping the old answer until the new one lands.
/// A listed answer is kept for a minute; an unavailable one for three, so a signed-out CLI is not asked every minute
/// per row. The origin remote is read again with each lookup, so a repository that moves host is followed.
/// </summary>
public sealed class PullRequestCatalogue(IGit git, IPullRequests hosts, TimeProvider clock) : IDisposable
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan UnavailableWindow = TimeSpan.FromMinutes(3);

    private readonly ConcurrentDictionary<string, Answer> answers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Lazy<Task>> running = new(StringComparer.Ordinal);

    // A burst of rows becomes waves of four host processes rather than dozens at once.
    private readonly SemaphoreSlim slots = new(4);

    public PullRequestLookup Find(FolderPath checkout, BranchName? branch) => (branch, branch is not null && answers.TryGetValue(KeyOf(checkout, branch), out Answer? answer) ? answer : null) switch
    {
        (null, _) => new PullRequestLookup.Missing(PullRequestGap.NoBranch),
        (_, null) => new PullRequestLookup.Missing(PullRequestGap.Looking),
        (BranchName named, { Listing: PullRequestListing.Listed listed }) => ForBranch(listed, named),
        (_, { Listing: PullRequestListing.Unavailable unavailable, Host: var host }) => new PullRequestLookup.Missing(unavailable.Gap, host),
        _ => new PullRequestLookup.Missing(PullRequestGap.Failed),
    };

    public Task RefreshStaleAsync(IEnumerable<(FolderPath Checkout, BranchName Branch)> branches)
    {
        DateTimeOffset now = clock.GetUtcNow();
        List<Task> started = [];
        foreach ((FolderPath checkout, BranchName branch) in branches.DistinctBy(pair => KeyOf(pair.Checkout, pair.Branch)))
        {
            string key = KeyOf(checkout, branch);
            if (answers.TryGetValue(key, out Answer? answer) && now - answer.At < answer.Kept)
            {
                continue;
            }

            started.Add(running.GetOrAdd(key, _ => new Lazy<Task>(() => RefreshAsync(checkout, branch))).Value);
        }

        return Task.WhenAll(started);
    }

    private static PullRequestLookup ForBranch(PullRequestListing.Listed listed, BranchName branch) =>
        PullRequests.ForBranch([.. listed.PullRequests.Where(row => row.Branch == branch).Select(row => row.PullRequest)]) is PullRequest found
            ? new PullRequestLookup.Found(found)
            : new PullRequestLookup.Missing(PullRequestGap.NoPullRequest);

    public void Dispose() => slots.Dispose();

    private static string KeyOf(FolderPath checkout, BranchName branch) => $"{checkout.Key}|{branch.Value}";

    private async Task RefreshAsync(FolderPath checkout, BranchName branch)
    {
        string key = KeyOf(checkout, branch);
        try
        {
            (PullRequestListing listing, PullRequestHost? host) = await ListAsync(checkout, branch);
            answers[key] = new Answer(listing, host, clock.GetUtcNow());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Nothing awaits a refresh started by a row, so a failure is kept as an answer the footer can explain.
            answers[key] = new Answer(new PullRequestListing.Unavailable(PullRequestGap.Failed), Host: null, clock.GetUtcNow());
        }
        finally
        {
            running.TryRemove(key, out _);
        }
    }

    private async Task<(PullRequestListing Listing, PullRequestHost? Host)> ListAsync(FolderPath checkout, BranchName branch)
    {
        string? url = await git.OriginUrlAsync(checkout.Value, CancellationToken.None);
        if (url is null)
        {
            return (new PullRequestListing.Unavailable(PullRequestGap.NoRemote), null);
        }

        if (RemoteRepository.Parse(url) is not RemoteRepository repository)
        {
            return (new PullRequestListing.Unavailable(PullRequestGap.UnknownHost), null);
        }

        await slots.WaitAsync();
        try
        {
            return (await hosts.ListAsync(repository, checkout, branch, CancellationToken.None), repository.Host);
        }
        finally
        {
            slots.Release();
        }
    }

    private sealed record Answer(PullRequestListing Listing, PullRequestHost? Host, DateTimeOffset At)
    {
        public TimeSpan Kept => Listing is PullRequestListing.Listed ? Window : UnavailableWindow;
    }
}