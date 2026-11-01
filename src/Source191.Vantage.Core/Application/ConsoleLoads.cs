using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// The console's loads: a Sessions refresh, a Worktrees scan and a Config read, each a <see cref="ViewInput"/> for its
/// tab, and the Config rows shown while the setup is being read. The scan reads the Sessions tab's latest listing and
/// runs none of its own.
/// </summary>
public sealed class ConsoleLoads(LatestSessions sessions, SessionRowEnricher enricher, WorktreeInventory inventory, SetupFactsReader setup, CurrentSettings settings)
{
    private readonly LatestSessions sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    private readonly SessionRowEnricher enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
    private readonly WorktreeInventory inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    private readonly SetupFactsReader setup = setup ?? throw new ArgumentNullException(nameof(setup));
    private readonly CurrentSettings settings = settings ?? throw new ArgumentNullException(nameof(settings));

    /// <summary>The Sessions tab's rows and the automated runs of the same listing.</summary>
    public async Task<ViewInput> LoadSessionsAsync(CancellationToken cancellationToken)
    {
        SessionListing listing = await sessions.ListAsync(cancellationToken);
        IReadOnlyList<SessionRow> rows = await enricher.EnrichAsync(listing.Sessions, cancellationToken);

        return new RowsLoaded(rows) { AutomatedRuns = enricher.Measure(listing.AutomatedRuns) };
    }

    /// <summary>The Worktrees tab's rows, with the sessions of the Sessions tab's latest listing, waiting for the first.</summary>
    public async Task<ViewInput> ScanWorktreesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Session> listed = await sessions.SessionsAsync(cancellationToken);

        return new WorktreeRowsLoaded(await inventory.ScanAsync(listed, cancellationToken));
    }

    /// <summary>The Config tab's rows with the setup read now.</summary>
    public async Task<ViewInput> ReadConfigAsync(CancellationToken cancellationToken) =>
        new ConfigRowsLoaded(ConfigRows.For(settings.Value, await setup.ReadAsync(cancellationToken)));

    /// <summary>The Config tab's rows before the setup has been read, with the settings as they are now.</summary>
    public IReadOnlyList<ConfigRow> ConfigRowsWhileReading() => ConfigRows.For(settings.Value, SetupFacts.Reading);
}