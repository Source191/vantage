using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application;

/// <summary>
/// The console's loads: a Sessions refresh, a Worktrees scan and a Config read, each a <see cref="ViewInput"/> for its
/// tab, and the Config rows shown while the setup is being read. The scan reads the Sessions tab's latest listing and
/// runs none of its own. The listing, the enrichment and the scan are each timed as a refresh part; the scan's wait for the
/// first listing is not part of its time.
/// </summary>
public sealed class ConsoleLoads(LatestSessions sessions, SessionRowEnricher enricher, WorktreeInventory inventory, SetupFactsReader setup, CurrentSettings settings, RefreshCounts refreshes)
{
    /// <summary>The Sessions tab's rows and the automated runs of the same listing.</summary>
    public async Task<ViewInput> LoadSessionsAsync(CancellationToken cancellationToken)
    {
        SessionListing listing = await refreshes.TimeAsync(RefreshPart.SessionsListing, () => sessions.ListAsync(cancellationToken));
        IReadOnlyList<SessionRow> rows = await refreshes.TimeAsync(RefreshPart.SessionsEnrichment, () => enricher.EnrichAsync(listing.Sessions, cancellationToken));

        return new RowsLoaded(rows) { AutomatedRuns = enricher.Measure(listing.AutomatedRuns) };
    }

    /// <summary>The Worktrees tab's rows, with the sessions of the Sessions tab's latest listing, waiting for the first.</summary>
    public async Task<ViewInput> ScanWorktreesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Session> listed = await sessions.SessionsAsync(cancellationToken);

        return new WorktreeRowsLoaded(await refreshes.TimeAsync(RefreshPart.WorktreesScan, () => inventory.ScanAsync(listed, cancellationToken)));
    }

    /// <summary>The Config tab's rows with the setup read now.</summary>
    public async Task<ViewInput> ReadConfigAsync(CancellationToken cancellationToken) =>
        new ConfigRowsLoaded(ConfigRows.For(settings.Value, await setup.ReadAsync(cancellationToken)));

    /// <summary>The Config tab's rows before the setup has been read, with the settings as they are now.</summary>
    public IReadOnlyList<ConfigRow> ConfigRowsWhileReading() => ConfigRows.For(settings.Value, SetupFacts.Reading);
}