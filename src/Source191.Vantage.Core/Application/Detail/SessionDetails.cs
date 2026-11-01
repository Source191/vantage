using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Application.Detail;

/// <summary>The Sessions tab's pane: a session's facts, keyed by the session's id.</summary>
public sealed class SessionDetails(DetailLoader loader, TimeProvider clock) : IDetailSource<SessionRow>
{
    private readonly DetailLoader loader = loader ?? throw new ArgumentNullException(nameof(loader));
    private readonly TimeProvider clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public int Rows => Wording.DetailFactRows;

    public object KeyOf(SessionRow row) => row?.Session.SessionId ?? throw new ArgumentNullException(nameof(row));

    public async Task<DetailContent> ReadAsync(SessionRow row, CancellationToken cancellationToken) =>
        Wording.Detail(await loader.SessionAsync(row, cancellationToken), clock.GetUtcNow(), clock.LocalTimeZone);
}