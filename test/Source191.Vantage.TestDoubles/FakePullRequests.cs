using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>
/// Answers each repository with its canned listing (an empty one by default) and counts the calls. Hold keeps every
/// call open until Release, so a test can watch what happens while a lookup is still running.
/// </summary>
public sealed class FakePullRequests : IPullRequests
{
    private readonly Lock gate = new();
    private readonly Dictionary<RemoteRepository, PullRequestListing> listings = [];
    private readonly List<RemoteRepository> calls = [];
    private TaskCompletionSource held = Released();

    public IReadOnlyList<RemoteRepository> Calls
    {
        get
        {
            lock (gate)
            {
                return [.. calls];
            }
        }
    }

    public FakePullRequests Answer(RemoteRepository repository, PullRequestListing listing)
    {
        lock (gate)
        {
            listings[repository] = listing;
        }

        return this;
    }

    public FakePullRequests Hold()
    {
        held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        return this;
    }

    public void Release()
    {
        held.TrySetResult();
    }

    public async Task<PullRequestListing> ListAsync(RemoteRepository repository, FolderPath checkout, CancellationToken cancellationToken)
    {
        Task wait;
        lock (gate)
        {
            calls.Add(repository);
            wait = held.Task;
        }

        await wait.WaitAsync(cancellationToken);
        lock (gate)
        {
            return listings.TryGetValue(repository, out PullRequestListing? listing) ? listing : new PullRequestListing.Listed([]);
        }
    }

    private static TaskCompletionSource Released()
    {
        TaskCompletionSource released = new();
        released.SetResult();
        return released;
    }
}
