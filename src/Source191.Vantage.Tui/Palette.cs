using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Source191.Vantage.Tui;

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
    /// <summary>Grey text: #868180, 5.09:1 on the background, so it meets WCAG AA wherever it is drawn as a foreground.</summary>
    public static readonly Color Grey = new(134, 129, 128);

    /// <summary>
    /// The background behind white text in a dialog's unfocused field and a list's chosen row once focus moves on:
    /// #606060, which white text is 5.04:1 from and which stands 3.11:1 from the console's background.
    /// </summary>
    public static readonly Color FieldBackground = new(96, 96, 96);
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
    /// visible (white on the field background) after focus moves on, so the choice can be seen from any field in the dialog.
    /// </summary>
    public static Scheme Focusable(Color foreground)
    {
        Attribute normal = new(foreground, Background);
        Attribute focus = new(new Color(0, 0, 0), Cyan);
        Attribute active = new(White, FieldBackground);

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

    public static Scheme Highlight() => Pair(new Color(0, 0, 0), Cyan);

    /// <summary>
    /// Every tab's frame: grey lines, and the title of the tab shown (the focused one) in bold cyan. A frame drawn in each
    /// tab's own colour broke at the joins <c>Tabs</c> shares between tabs, so only the title marks the active tab.
    /// </summary>
    public static Scheme TabFrame()
    {
        Attribute lines = new(Grey, Background);
        Attribute active = new(Cyan, Background, TextStyle.Bold);

        return new Scheme { Normal = lines, HotNormal = lines, Focus = active, HotFocus = active, Active = lines, HotActive = lines, Disabled = lines };
    }

    /// <summary>The selected table row's background: the bright cyan Terminal.Gui derived from <see cref="Highlight"/> for a table that cannot take focus, now named.</summary>
    public static readonly Color SelectionBackground = new(41, 241, 255);

    private static readonly Color SelectionText = new(51, 51, 51);

    /// <summary>
    /// The selected table row, bold dark grey on bright cyan. A table that cannot take focus draws the selected row with
    /// the Active role, which Terminal.Gui otherwise derives from Normal as a lighter shade, so every role is set to the
    /// exact colours.
    /// </summary>
    public static Scheme SelectedRow() => SelectedRow(SelectionText);

    /// <summary>The selected row's scheme for a cell that keeps its own colour there, such as the TAB block.</summary>
    public static Scheme SelectedRow(Color foreground)
    {
        Attribute attribute = new(foreground, SelectionBackground, TextStyle.Bold);

        return new Scheme { Normal = attribute, Focus = attribute, HotNormal = attribute, HotFocus = attribute, Active = attribute, HotActive = attribute, Disabled = attribute };
    }

    public static Scheme Pair(Color foreground, Color background)
    {
        Attribute attribute = new(foreground, background);

        return new Scheme { Normal = attribute, Focus = attribute, HotNormal = attribute, HotFocus = attribute, Disabled = attribute };
    }

    /// <summary>The colour of a piece of the frame's text.</summary>
    public static Color ForTone(TextTone tone)
    {
        return tone switch
        {
            TextTone.Plain => White,
            TextTone.Amber => Amber,
            TextTone.Cyan => Cyan,
            TextTone.Green => Green,
            TextTone.Red => Red,
            TextTone.Grey => Grey,
            TextTone.Orange => Orange,
        };
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
            DisplayStateKind.Stopped => Grey,
            DisplayStateKind.Unknown => Grey,
        };
    }
}