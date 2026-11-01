using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Terminal.Gui.Drawing;

namespace Source191.Vantage.Tui;

/// <summary>The Config tab as the shared table screen shows it: a setting or a piece of setup per row, the setup in grey.</summary>
internal sealed class ConfigScreenDefinition(TimeProvider clock) : IScreenDefinition<ConfigViewState, ConfigRow>
{
    private const string Ellipsis = "…";

    private readonly TimeProvider clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <summary>Never used to refresh: the tab reads the setup when it is shown and after each save.</summary>
    public TimeSpan RefreshEvery => TimeSpan.FromMinutes(5);

    /// <summary>The Config tab has no pane: a setting's row says all there is.</summary>
    public TableDetail<ConfigRow> Detail => new TableDetail<ConfigRow>.None();

    /// <summary>What the setting does or the setup is for, the same words the status line gives the selected row.</summary>
    public string RowTip(ConfigRow row) => Wording.ConfigDescription(row);


    public string WaitingText => "Reading the settings";

    public string LoadFailurePrefix => "reading failed";

    public string HelpText => Wording.ConfigHelp;

    public IReadOnlyList<ColumnHelp> Columns => Wording.ConfigColumns;

    public int? ElasticColumn => 1;

    public string Cell(ConfigRow row, int column, DateTimeOffset now, int width)
    {
        return column switch
        {
            0 => Wording.ConfigSetting(row),
            _ => Middle(Wording.ConfigValue(row, now, clock.LocalTimeZone), width),
        };
    }

    public CellPaint CellPaint(ConfigRow row, int column) => new CellPaint.None();

    public Color RowColour(ConfigRow row) => row is ConfigRow.Setup ? Palette.Grey : Palette.White;

    public string TabTitle(ConfigViewState state) => Wording.TabTitleFor(ConsoleTab.Config);

    public IReadOnlyList<TextPart> Counts(ConfigViewState state) => Wording.ConfigCounts(state.Rows);

    public KeyBar Bar(ConfigViewState state, bool updateKnown) => Wording.ConfigBar(state, updateKnown);

    public ConfigViewState WithMessage(ConfigViewState state, StatusMessage message) => state with { Message = message };

    public (ConfigViewState State, IReadOnlyList<ViewEffect> Effects) Reduce(ConfigViewState state, ViewInput input)
    {
        ConfigViewUpdate update = ConfigView.Reduce(state, input);

        return (update.State, update.Effects);
    }

    public void OnInput(ConfigViewState before, ViewInput input)
    {
        // Nothing listens to the Config tab's inputs.
    }

    /// <summary>A path too long for the cell keeps its start and its end, so the drive and the file or folder name both show.</summary>
    private static string Middle(string text, int width)
    {
        if (text.Length <= width || width <= Ellipsis.Length)
        {
            return text.Length <= width ? text : text[..Math.Max(width, 0)];
        }

        int start = (width - Ellipsis.Length) / 2;
        int end = width - Ellipsis.Length - start;

        return $"{text[..start]}{Ellipsis}{text[^end..]}";
    }
}