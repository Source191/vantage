using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// The sessions of the Sessions tab's latest listing, held for the worktrees scan so it runs no listing of its own.
/// The Sessions tab lists through <see cref="ListAsync"/> and a scan reads <see cref="SessionsAsync"/>, which waits
/// until the first listing has returned, whether it succeeded or failed, so the start-up scan never competes with it.
/// The sessions are an empty list until a listing succeeds and are kept through a later failure; never null.
/// </summary>
public sealed class LatestSessions(IClaudeCli claude)
{
    private readonly IClaudeCli claude = claude ?? throw new ArgumentNullException(nameof(claude));
    private readonly SharedSnapshot<IReadOnlyList<Session>> sessions = new([]);

    // Continuations run off the listing's thread, so a waiting scan never runs inline on the Sessions tab's load.
    private readonly TaskCompletionSource firstListing = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Lists through the CLI and holds the listing's sessions; a failure reaches the caller as the CLI's own exception.</summary>
    public async Task<SessionListing> ListAsync(CancellationToken cancellationToken)
    {
        try
        {
            SessionListing listing = await claude.ListSessionsAsync(cancellationToken);
            sessions.Replace(listing.Sessions);

            return listing;
        }
        finally
        {
            // A failed listing also releases a scan waiting for the first, which then reads the sessions kept so far.
            firstListing.TrySetResult();
        }
    }

    /// <summary>The sessions of the latest listing that succeeded, once the first listing has returned.</summary>
    public async Task<IReadOnlyList<Session>> SessionsAsync(CancellationToken cancellationToken)
    {
        await firstListing.Task.WaitAsync(cancellationToken);

        return sessions.Value;
    }
}