using Source191.Torni.Application.Views;

namespace Source191.Torni.Application.Detail;

/// <summary>The Sessions tab's pane: a session's facts, keyed by the session's id.</summary>
public sealed class SessionDetails(DetailLoader loader, TimeProvider clock) : IDetailSource<SessionRow>
{
    public int Rows => Wording.DetailFactRows;

    public object KeyOf(SessionRow row) => row.Session.SessionId;

    public async Task<DetailContent> ReadAsync(SessionRow row, CancellationToken cancellationToken) =>
        Wording.Detail(await loader.SessionAsync(row, cancellationToken), clock.GetUtcNow(), clock.LocalTimeZone);
}