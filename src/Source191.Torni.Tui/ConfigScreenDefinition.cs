using Source191.Torni.Application;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Terminal.Gui.Drawing;

namespace Source191.Torni.Tui;

/// <summary>The Config tab as the shared table screen shows it: a setting or a piece of setup per row, the setup grey under an orange Setup heading.</summary>
internal sealed class ConfigScreenDefinition(TimeProvider clock) : IScreenDefinition<ConfigViewState, ConfigRow>
{
    public ConsoleTab Tab => ConsoleTab.Config;

    /// <summary>The tab reads the setup when it is shown and after each save, never on a timer.</summary>
    public TabRefresh Refresh => new TabRefresh.Never();

    /// <summary>What the setting does or the setup is for, shown only as the row's tooltip.</summary>
    public string RowTip(ConfigRow row) => Wording.ConfigDescription(row);

    public string WaitingText => Wording.WaitingText(ConsoleTab.Config);

    public string LoadFailurePrefix => Wording.LoadFailurePrefix(ConsoleTab.Config);

    public string HelpText => Wording.ConfigHelp;

    public IReadOnlyList<TableColumn<ConfigRow>> Columns { get; } =
    [
        TableColumn<ConfigRow>.Plain(Wording.SettingColumn, (row, _, _) => Wording.ConfigSetting(row), new ColumnFit.Natural()),
        TableColumn<ConfigRow>.Plain(Wording.ValueColumn, (row, now, width) => TextFit.ElideMiddle(Wording.ConfigValue(row, now, clock.LocalTimeZone), width), new ColumnFit.Elastic()),
    ];
    public Color RowColour(ConfigRow row) => row switch
    {
        ConfigRow.Heading => Palette.Orange,
        ConfigRow.Setup => Palette.Grey,
        _ => Palette.White,
    };

    public IReadOnlyList<TextPart> Counts(ConfigViewState state) => Wording.ConfigCounts(state.List.Rows);

    public KeyBar Bar(ConfigViewState state, bool updateKnown) => Wording.ConfigBar(updateKnown);

    public ConfigViewState WithMessage(ConfigViewState state, StatusMessage message) => state with { Message = message };

    public ViewUpdate<ConfigViewState> Reduce(ConfigViewState state, ViewInput input) => ConfigView.Reduce(state, input);
}