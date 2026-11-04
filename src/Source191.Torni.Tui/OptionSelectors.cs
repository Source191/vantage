using Terminal.Gui.Views;

namespace Source191.Torni.Tui;

/// <summary>
/// The option selectors that choose one member of an enum: their labels are the members described in declaration order,
/// and an index converts to a member by position in that same order, never by a cast, so a reordered or renumbered enum
/// cannot pick the wrong label.
/// </summary>
internal static class OptionSelectors
{
    public static IReadOnlyList<string> Labels<TEnum>(Func<TEnum, string> describe)
        where TEnum : struct, Enum
        => [.. Enum.GetValues<TEnum>().Select(describe)];

    public static int Index<TEnum>(TEnum member)
        where TEnum : struct, Enum
        => Array.IndexOf(Enum.GetValues<TEnum>(), member);

    /// <summary>The member at a selector's value; a selector with nothing chosen answers the first member.</summary>
    public static TEnum At<TEnum>(int? index)
        where TEnum : struct, Enum
        => Enum.GetValues<TEnum>()[index ?? 0];

    public static OptionSelector For<TEnum>(int x, int y, Func<TEnum, string> describe)
        where TEnum : struct, Enum
        => new() { X = x, Y = y, Labels = [.. Labels(describe)], Value = 0 };
}