using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.TestDoubles;

/// <summary>
/// Answers each repository with its canned listing (an empty one by default), narrowed to the branch asked for as a
/// host narrows it, and records each call's repository and branch. Hold keeps every call open until Release (or ReleaseOne,
/// for the oldest), so a test can watch what happens while a lookup is still running; WhenCalledAsync completes once
/// that many calls have been recorded.
/// </summary>
public sealed class FakePullRequests : IPullRequests
{
    private readonly Lock gate = new();
    private readonly Dictionary<RemoteRepository, PullRequestListing> listings = [];
    private readonly List<(RemoteRepository Repository, BranchName Branch)> calls = [];
    private readonly Queue<TaskCompletionSource> holds = [];
    private readonly List<(int Count, TaskCompletionSource Reached)> waiters = [];
    private bool holding;

    public IReadOnlyList<RemoteRepository> Calls
    {
        get
        {
            lock (gate)
            {
                return [.. calls.Select(call => call.Repository)];
            }
        }
    }

    public IReadOnlyList<(RemoteRepository Repository, BranchName Branch)> Requests
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
        lock (gate)
        {
            holding = true;
        }

        return this;
    }

    public void Release()
    {
        lock (gate)
        {
            holding = false;
            while (holds.Count > 0)
            {
                holds.Dequeue().TrySetResult();
            }
        }
    }

    public void ReleaseOne()
    {
        lock (gate)
        {
            if (holds.Count > 0)
            {
                holds.Dequeue().TrySetResult();
            }
        }
    }

    public Task WhenCalledAsync(int count)
    {
        lock (gate)
        {
            if (calls.Count >= count)
            {
                return Task.CompletedTask;
            }

            TaskCompletionSource reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
            waiters.Add((count, reached));
            return reached.Task;
        }
    }

    public async Task<PullRequestListing> ListAsync(RemoteRepository repository, FolderPath checkout, BranchName branch, CancellationToken cancellationToken)
    {
        Task wait;
        lock (gate)
        {
            calls.Add((repository, branch));
            wait = HoldForThisCall();
            NotifyWaiters();
        }

        await wait.WaitAsync(cancellationToken);
        lock (gate)
        {
            return listings.TryGetValue(repository, out PullRequestListing? listing) ? ForBranch(listing, branch) : new PullRequestListing.Listed([]);
        }
    }

    private Task HoldForThisCall()
    {
        if (!holding)
        {
            return Task.CompletedTask;
        }

        TaskCompletionSource hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
        holds.Enqueue(hold);
        return hold.Task;
    }

    private void NotifyWaiters()
    {
        foreach ((int Count, TaskCompletionSource Reached) waiter in waiters.Where(waiter => waiter.Count <= calls.Count))
        {
            waiter.Reached.TrySetResult();
        }

        waiters.RemoveAll(waiter => waiter.Count <= calls.Count);
    }

    private static PullRequestListing ForBranch(PullRequestListing listing, BranchName branch) => listing is PullRequestListing.Listed listed
        ? new PullRequestListing.Listed([.. listed.PullRequests.Where(row => row.Branch == branch)])
        : listing;
}