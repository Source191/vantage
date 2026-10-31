using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// The sessions of the Sessions tab's latest listing, kept for the worktrees scan so it runs no listing of its own.
/// </summary>
public sealed class LatestSessions(IClaudeCli claude)
{
    private readonly IClaudeCli claude = claude ?? throw new ArgumentNullException(nameof(claude));
    private readonly SharedSnapshot<IReadOnlyList<Session>> sessions = new([]);
    private readonly TaskCompletionSource firstListing = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<SessionListing> ListAsync(CancellationToken cancellationToken)
    {
        SessionListing listing = await claude.ListSessionsAsync(cancellationToken);
        sessions.Replace(listing.Sessions);
        firstListing.TrySetResult();

        return listing;
    }

    public async Task<IReadOnlyList<Session>> SessionsAsync(CancellationToken cancellationToken)
    {
        await firstListing.Task;

        return sessions.Value;
    }
}