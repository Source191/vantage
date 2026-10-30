using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>The Config tab's rows: the settings in the order the tab lists them, then the setup, read or still being read.</summary>
public static class ConfigRows
{
    public static IReadOnlyList<ConfigRow> For(Settings settings, SetupFacts? facts) => [];
}
