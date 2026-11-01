using Terminal.Gui.Drawing;
using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>
/// The Vantage icon for the console's top left corner: the pixels of assets\icon\vantage-48.rgba (written by
/// tools\New-IconPixels.ps1), drawn by an ImageView fitted to one row. It shows only where the terminal draws images
/// (<see href="https://vt100.net/docs/vt3xx-gp/chapter14.html">Sixel</see> or
/// <see href="https://sw.kovidgoyal.net/kitty/graphics-protocol/">Kitty</see>); cell-based drawing would turn each pixel into a cell, so without image support it stays hidden.
/// </summary>
internal static class VantageIcon
{
    public const int Height = 1;

    /// <summary>The cells a terminal that reports no cell size gets: Terminal.Gui's own default of 10 by 20 pixels makes a row two cells wide.</summary>
    private const int DefaultWidth = 2;

    private const string Resource = "Source191.Vantage.Tui.vantage-48.rgba";

    public static ImageView Create()
    {
        ImageView view = new() { X = 0, Y = 0, Width = DefaultWidth, Height = Height, Visible = false, CanFocus = false, UseRasterGraphics = true, Image = Pixels() };

        return view;
    }

    /// <summary>
    /// Shows the icon where the terminal draws images, given the terminal's cell size in pixels, as many cells wide as its
    /// row is tall in pixels so the square icon is not stretched (a fixed five cells widened it and left its right edge
    /// unlike its left), and answers the cells the settings row starts after: the icon and a cell of space.
    /// </summary>
    public static int Fit(ImageView icon, System.Drawing.Size cell)
    {
        ArgumentNullException.ThrowIfNull(icon);
        icon.Visible = true;
        int width = cell.Width > 0 && cell.Height > 0 ? (int)Math.Round((double)Height * cell.Height / cell.Width) : DefaultWidth;
        int cells = Math.Clamp(width, 1, 3);
        icon.Width = cells;

        return cells + 1;
    }

    /// <summary>Hides the icon where the terminal draws no images, and answers the cells the settings row starts after: none.</summary>
    public static int Hide(ImageView icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        icon.Visible = false;

        return 0;
    }

    /// <summary>Transparent pixels take the console's background, so the icon sits on it whatever the terminal does with transparency.</summary>
    private static Color[,] Pixels()
    {
        using Stream stream = typeof(VantageIcon).Assembly.GetManifestResourceStream(Resource)
            ?? throw new InvalidOperationException($"The icon resource {Resource} is missing from the Tui assembly.");
        int width = stream.ReadByte();
        int height = stream.ReadByte();
        byte[] rgba = new byte[width * height * 4];
        stream.ReadExactly(rgba);
        Color[,] pixels = new Color[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int at = ((y * width) + x) * 4;
                pixels[x, y] = Blend(rgba[at], rgba[at + 1], rgba[at + 2], rgba[at + 3]);
            }
        }

        return pixels;
    }

    private static Color Blend(byte red, byte green, byte blue, byte alpha)
    {
        Color background = Palette.Background;

        int Mix(int over, int under) => ((over * alpha) + (under * (255 - alpha))) / 255;

        return new Color(Mix(red, background.R), Mix(green, background.G), Mix(blue, background.B));
    }
}