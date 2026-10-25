using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Collections.Concurrent;

namespace Source191.Vantage.Application;

/// <summary>
/// Each repository's pull requests, listed at most once every three minutes per main checkout and never waited for by
/// a row: Find answers from what has already landed (Looking before the first answer), and RefreshStaleAsync starts a
/// listing for each checkout whose answer is older than the window and has none running, keeping the old answer until
/// the new one lands. Unavailable answers are kept for the same window, so a signed-out CLI is not asked every refresh.
/// The origin remote is read again with each listing, so a repository that moves host is followed.
/// </summary>
public sealed class PullRequestCatalogue(IGit git, IPullRequests hosts, TimeProvider clock)
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(3);

    private readonly ConcurrentDictionary<string, Answer> answers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Lazy<Task>> running = new(StringComparer.Ordinal);

    public PullRequestLookup Find(FolderPath checkout, BranchName? branch) => (branch, answers.TryGetValue(checkout.Key, out Answer? answer) ? answer.Listing : null) switch
    {
        (null, _) => new PullRequestLookup.Missing(PullRequestGap.NoBranch),
        (_, null) => new PullRequestLookup.Missing(PullRequestGap.Looking),
        (BranchName named, PullRequestListing.Listed listed) => ForBranch(listed, named),
        (_, PullRequestListing.Unavailable unavailable) => new PullRequestLookup.Missing(unavailable.Gap),
        _ => new PullRequestLookup.Missing(PullRequestGap.Failed),
    };

    public Task RefreshStaleAsync(IEnumerable<FolderPath> checkouts)
    {
        DateTimeOffset now = clock.GetUtcNow();
        List<Task> started = [];
        foreach (FolderPath checkout in checkouts.DistinctBy(checkout => checkout.Key))
        {
            if (answers.TryGetValue(checkout.Key, out Answer? answer) && now - answer.At < Window)
            {
                continue;
            }

            started.Add(running.GetOrAdd(checkout.Key, _ => new Lazy<Task>(() => RefreshAsync(checkout))).Value);
        }

        return Task.WhenAll(started);
    }

    private static PullRequestLookup ForBranch(PullRequestListing.Listed listed, BranchName branch) =>
        PullRequests.ForBranch([.. listed.PullRequests.Where(row => row.Branch == branch).Select(row => row.PullRequest)]) is PullRequest found
            ? new PullRequestLookup.Found(found)
            : new PullRequestLookup.Missing(PullRequestGap.NoPullRequest);

    private async Task RefreshAsync(FolderPath checkout)
    {
        try
        {
            PullRequestListing listing = await ListAsync(checkout);
            answers[checkout.Key] = new Answer(listing, clock.GetUtcNow());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Nothing awaits a refresh started by a row, so a failure is kept as an answer the footer can explain.
            answers[checkout.Key] = new Answer(new PullRequestListing.Unavailable(PullRequestGap.Failed), clock.GetUtcNow());
        }
        finally
        {
            running.TryRemove(checkout.Key, out _);
        }
    }

    private async Task<PullRequestListing> ListAsync(FolderPath checkout)
    {
        string? url = await git.OriginUrlAsync(checkout.Value, CancellationToken.None);
        if (url is null)
        {
            return new PullRequestListing.Unavailable(PullRequestGap.NoRemote);
        }

        return RemoteRepository.Parse(url) is RemoteRepository repository
            ? await hosts.ListAsync(repository, checkout, CancellationToken.None)
            : new PullRequestListing.Unavailable(PullRequestGap.UnknownHost);
    }

    private sealed record Answer(PullRequestListing Listing, DateTimeOffset At);
}
