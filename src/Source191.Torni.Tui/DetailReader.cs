using Source191.Torni.Application;
using Source191.Torni.Application.Detail;
using Source191.Torni.Application.Diagnostics;
using System.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Torni.Tui;

/// <summary>
/// A tab's detail pane under its table and the lifecycle of what it shows: the selected row's detail is asked for while the tab
/// is shown (a hidden tab draws nothing of its own, or it lands on top of the shown one), when the selection moved or for every
/// refresh, read off the UI thread and shown there unless a newer answer or another row came first. A tab with no pane has the
/// <see cref="None"/> reader, which does nothing; the pane's has its source, the order of its requests and the token that
/// drops a read still running when the tab is disposed.
/// </summary>
internal abstract class DetailReader<TRow>
{
    private DetailReader()
    {
    }

    /// <summary>What the key of the row last asked for equals when no row is: no row's key equals it.</summary>
    public abstract object Current { get; }

    /// <summary>The views the tab adds to its own: the pane, or none.</summary>
    public abstract IReadOnlyList<View> Views { get; }

    /// <summary>
    /// Asks for the selected row's detail while the tab is shown: when the selection moved, or for every refresh; an empty table
    /// empties the pane.
    /// </summary>
    public abstract void AskFor(TRow? selected, bool shown, bool refreshed);

    /// <summary>A tab shown again reads its selected row's detail afresh.</summary>
    public abstract void AskAgain();

    /// <summary>Drops a read still running; the tab calls it before it disposes its view.</summary>
    public abstract void Release();

    /// <summary>The reader for a tab's detail: the pane under the table (which then stops short of it), or none.</summary>
    public static DetailReader<TRow> For(TableDetail<TRow> detail, TableView table, IApplication app, DiagnosticLog log, ITabHost host) => detail switch
    {
        TableDetail<TRow>.Pane pane => new Reading(pane.Source, table, app, log, host),
        TableDetail<TRow>.None => new None(),
        _ => throw new UnreachableException(),
    };

    /// <summary>No pane: nothing is added under the table, which fills the tab.</summary>
    private sealed class None : DetailReader<TRow>
    {
        private readonly object nothing = new();

        public override object Current => nothing;

        public override IReadOnlyList<View> Views => [];

        public override void AskFor(TRow? selected, bool shown, bool refreshed)
        {
        }

        public override void AskAgain()
        {
        }

        public override void Release()
        {
        }
    }

    private sealed class Reading : DetailReader<TRow>, IDisposable
    {
        private readonly IDetailSource<TRow> source;
        private readonly IApplication app;
        private readonly DiagnosticLog log;
        private readonly ITabHost host;
        private readonly DetailPane pane;
        private readonly DetailRequests requests = new();
        private readonly CancellationTokenSource lifetime = new();

        /// <summary>What <see cref="key"/> holds when the next redraw must ask for the selected row's detail again; no row's key equals it.</summary>
        private readonly object askAgain = new();

        /// <summary>The key of the row whose detail was last asked for, or <see cref="askAgain"/>.</summary>
        private object key;

        public Reading(IDetailSource<TRow> source, TableView table, IApplication app, DiagnosticLog log, ITabHost host)
        {
            this.source = source;
            this.app = app;
            this.log = log;
            this.host = host;
            key = askAgain;
            pane = new DetailPane(source.Rows);
            pane.Y = Pos.AnchorEnd(pane.Rows);
            table.Height = Dim.Fill(pane.Rows);
        }

        public override object Current => key;

        public override IReadOnlyList<View> Views => [pane];

        public override void AskAgain() => key = askAgain;

        public override void AskFor(TRow? selected, bool shown, bool refreshed)
        {
            if (!shown)
            {
                return;
            }

            if (selected is not TRow row)
            {
                requests.Cleared();
                key = askAgain;
                pane.Show(DetailContent.Empty);
                return;
            }

            object asked = source.KeyOf(row);
            if (!refreshed && Equals(asked, key))
            {
                return;
            }

            key = asked;
            Read(row, requests.Ask(asked));
        }

        public override void Release() => Dispose();

        public void Dispose()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }

        /// <summary>Reads one request's detail off the UI thread and shows it there, unless a newer answer or another row came first.</summary>
        private void Read(TRow row, DetailRequest request)
        {
            CancellationToken token = lifetime.Token;
            Task.Run(async () =>
            {
                DetailAnswer read = await Attempt.RunAsync<DetailAnswer>(log, FailureSite.Load, CodeName.For(typeof(DetailContent)), () => ReadAsync(row, token), _ => new DetailAnswer.Dropped());
                if (read is not DetailAnswer.Show { Content: DetailContent content } || token.IsCancellationRequested)
                {
                    return;
                }

                app.Invoke(() => Show(request, content));
            }, token);
        }

        /// <summary>The row's detail as read, or dropped when the tab was disposed while it was being read.</summary>
        private async Task<DetailAnswer> ReadAsync(TRow row, CancellationToken token)
        {
            try
            {
                return new DetailAnswer.Show(await source.ReadAsync(row, token));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return new DetailAnswer.Dropped();
            }
        }

        private void Show(DetailRequest request, DetailContent content)
        {
            if (lifetime.IsCancellationRequested || requests.Accept(request, content) is not DetailAnswer.Show shown)
            {
                return;
            }

            pane.Show(shown.Content);
            pane.SuperView?.SetNeedsDraw();
            host.FrameChanged();
        }
    }
}