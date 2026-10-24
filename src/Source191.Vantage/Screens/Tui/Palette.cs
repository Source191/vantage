using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Source191.Vantage.Screens.Tui;

/// <summary>
/// The console colours. This is the only place colour values live; everything else speaks in display states.
/// </summary>
internal static class Palette
{
    public static readonly Color Background = new(12, 12, 12);
    public static readonly Color Amber = new(255, 185, 0);
    public static readonly Color Cyan = new(0, 183, 195);
    public static readonly Color Green = new(22, 198, 12);
    public static readonly Color Red = new(231, 72, 86);
    public static readonly Color Grey = new(122, 117, 116);
    public static readonly Color Magenta = new(227, 0, 140);
    public static readonly Color Blue = new(59, 142, 234);
    public static readonly Color White = new(230, 230, 230);
    public static readonly Color Orange = new(247, 99, 12);

    public static Scheme Plain(Color foreground)
    {
        Attribute attribute = new(foreground, Background);

        return new Scheme { Normal = attribute, Focus = attribute, HotNormal = attribute, HotFocus = attribute, Disabled = attribute };
    }

    /// <summary>
    /// For views that take focus: the focused field or list row is black on cyan, and a list's selected row stays
    /// visible (white on grey) after focus moves on, so the choice can be seen from any field in the dialog.
    /// </summary>
    public static Scheme Focusable(Color foreground)
    {
        Attribute normal = new(foreground, Background);
        Attribute focus = new(new Color(0, 0, 0), Cyan);
        Attribute active = new(White, Grey);

        return new Scheme
        {
            Normal = normal,
            Focus = focus,
            HotNormal = normal,
            HotFocus = focus,
            Active = active,
            HotActive = active,
            Editable = normal,
            Disabled = new Attribute(Grey, Background),
        };
    }

    public static Scheme Highlight()
    {
        return Pair(new Color(0, 0, 0), Cyan);
    }

    public static Scheme Pair(Color foreground, Color background)
    {
        Attribute attribute = new(foreground, background);

        return new Scheme { Normal = attribute, Focus = attribute, HotNormal = attribute, HotFocus = attribute, Disabled = attribute };
    }

    public static Color ForState(DisplayStateKind kind)
    {
        return kind switch
        {
            DisplayStateKind.NeedsInput => Amber,
            DisplayStateKind.Working => Cyan,
            DisplayStateKind.Idle => White,
            DisplayStateKind.Completed => Green,
            DisplayStateKind.Failed => Red,
            _ => Grey,
        };
    }

    public static bool IsDimmed(DisplayStateKind kind)
    {
        return kind is DisplayStateKind.Completed or DisplayStateKind.Failed or DisplayStateKind.Stopped or DisplayStateKind.Unknown;
    }
}