using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Collections.Concurrent;

namespace Source191.Vantage.Application;

/// <summary>
/// Each branch's pull requests, looked up per main checkout and branch and never waited for by a row: Find answers
/// from what has already landed (Looking before the first answer), and RefreshStaleAsync starts a lookup for each
/// branch whose answer is older than the window and has none running, keeping the old answer until the new one lands.
/// A listed answer is kept for a minute; an unavailable one for three, so a signed-out CLI is not asked every minute
/// per row. The origin remote is read again with each lookup, so a repository that moves host is followed. A refresh that
/// starts at least one lookup is timed until its last lookup lands; one with nothing stale, or that only joins lookups
/// already running, is not, so near-zero runs do not hide the real ones.
/// </summary>
public sealed class PullRequestCatalogue(IGit git, IPullRequests hosts, TimeProvider clock, RefreshCounts refreshes) : IDisposable
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan UnavailableWindow = TimeSpan.FromMinutes(3);

    private readonly ConcurrentDictionary<string, Answer> answers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Lazy<Task>> running = new(StringComparer.Ordinal);

    // A burst of rows becomes waves of four host processes rather than dozens at once.
    private readonly SemaphoreSlim slots = new(4);

    /// <summary>The pull request of the branch the head is on; a head on no branch has none to look up.</summary>
    public PullRequestLookup Find(FolderPath checkout, Head head) => head is Head.OnBranch on ? Find(checkout, on.Branch) : new PullRequestLookup.Missing(PullRequestGap.NoBranch);

    private PullRequestLookup Find(FolderPath checkout, BranchName branch)
    {
        if (!answers.TryGetValue(KeyOf(checkout, branch), out Answer? answer))
        {
            return new PullRequestLookup.Missing(PullRequestGap.Looking);
        }

        return answer switch
        {
            { Listing: PullRequestListing.Listed listed } => ForBranch(listed, branch),
            { Listing: PullRequestListing.Unavailable unavailable } => new PullRequestLookup.Missing(unavailable.Gap, answer.Host),
            _ => new PullRequestLookup.Missing(PullRequestGap.Failed),
        };
    }

    public Task RefreshStaleAsync(IEnumerable<(FolderPath Checkout, BranchName Branch)> branches)
    {
        long began = clock.GetTimestamp();
        DateTimeOffset now = clock.GetUtcNow();
        List<Task> lookups = [];
        bool startedOne = false;
        foreach ((FolderPath checkout, BranchName branch) in branches.DistinctBy(pair => KeyOf(pair.Checkout, pair.Branch)))
        {
            string key = KeyOf(checkout, branch);
            if (answers.TryGetValue(key, out Answer? answer) && now - answer.At < answer.Kept)
            {
                continue;
            }

            Lazy<Task> fresh = new(() => RefreshAsync(checkout, branch));
            Lazy<Task> lookup = running.GetOrAdd(key, fresh);
            startedOne |= ReferenceEquals(lookup, fresh);
            lookups.Add(lookup.Value);
        }

        Task landed = Task.WhenAll(lookups);

        return startedOne ? TimedAsync(landed, began) : landed;
    }

    private async Task TimedAsync(Task landed, long began)
    {
        await landed;
        refreshes.Add(RefreshPart.PullRequestRefresh, clock.GetElapsedTime(began));
    }

    private static PullRequestLookup ForBranch(PullRequestListing.Listed listed, BranchName branch) =>
        PullRequests.ForBranch([.. listed.PullRequests.Where(row => row.Branch == branch).Select(row => row.PullRequest)]);

    public void Dispose() => slots.Dispose();

    private static string KeyOf(FolderPath checkout, BranchName branch) => $"{checkout.Key}|{branch.Value}";

    private async Task RefreshAsync(FolderPath checkout, BranchName branch)
    {
        string key = KeyOf(checkout, branch);
        try
        {
            (PullRequestListing listing, LookupHost host) = await ListAsync(checkout, branch);
            answers[key] = new Answer(listing, host, clock.GetUtcNow());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Nothing awaits a refresh started by a row, so a failure is kept as an answer the status line can explain.
            answers[key] = new Answer(new PullRequestListing.Unavailable(PullRequestGap.Failed), new LookupHost.NotRead(), clock.GetUtcNow());
        }
        finally
        {
            running.TryRemove(key, out _);
        }
    }

    private async Task<(PullRequestListing Listing, LookupHost Host)> ListAsync(FolderPath checkout, BranchName branch)
    {
        string? url = await git.OriginUrlAsync(checkout.Value, CancellationToken.None);
        if (url is null)
        {
            return (new PullRequestListing.Unavailable(PullRequestGap.NoRemote), new LookupHost.NotRead());
        }

        if (!RemoteRepository.TryParse(url, out RemoteRepository? repository))
        {
            return (new PullRequestListing.Unavailable(PullRequestGap.UnknownHost), new LookupHost.NotRead());
        }

        await slots.WaitAsync();
        try
        {
            return (await hosts.ListAsync(repository, checkout, branch, CancellationToken.None), new LookupHost.Known(repository.Host));
        }
        finally
        {
            slots.Release();
        }
    }

    private sealed record Answer(PullRequestListing Listing, LookupHost Host, DateTimeOffset At)
    {
        public TimeSpan Kept => Listing is PullRequestListing.Listed ? Window : UnavailableWindow;
    }
}