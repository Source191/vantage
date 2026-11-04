using System.Globalization;
using System.Text;

namespace Source191.Torni.HeadlessTerminal;

/// <summary>
/// Replays a capture into a character grid, honouring cursor positioning, erase-characters, erase-line, clear-screen
/// and relative cursor moves, so Terminal.Gui's cell-by-cell output reads as lines. The replay stops just after the
/// last occurrence of a marker (or at the alternate-screen exit that follows it), so a screen can be read as it was.
/// </summary>
public sealed class ScreenReplay(int columns, int rows)
{
    private readonly char[][] grid = [.. Enumerable.Range(0, rows).Select(_ => Enumerable.Repeat(' ', columns).ToArray())];
    private int row;
    private int column;

    public int Replayed { get; private set; }

    public IReadOnlyList<string> Lines => [.. grid.Select(line => new string(line).TrimEnd()).Where(line => line.Trim().Length > 0)];

    public int FilledCells => grid.Sum(line => line.Count(cell => cell != ' '));

    public static int EndFor(string capture, string marker)
    {
        int markerAt = capture.LastIndexOf(marker, StringComparison.Ordinal);
        if (markerAt < 0)
        {
            return capture.Length;
        }

        int teardown = capture.IndexOf("\x1b[?1049l", markerAt, StringComparison.Ordinal);

        return teardown >= 0 ? teardown : Math.Min(capture.Length, markerAt + 3000);
    }

    public ScreenReplay Replay(string capture, int end)
    {
        int i = 0;
        while (i < end)
        {
            i = capture[i] == '\x1b' && i + 1 < capture.Length ? Escape(capture, i) : Printable(capture[i], i);
        }

        Replayed = end;

        return this;
    }

    public static int PrintFile(string rawFile, string marker, string[] size)
    {
        string capture = File.ReadAllText(rawFile, Encoding.UTF8);
        int columns = size.Length > 0 ? int.Parse(size[0], CultureInfo.InvariantCulture) : 120;
        int rows = size.Length > 1 ? int.Parse(size[1], CultureInfo.InvariantCulture) : 40;
        ScreenReplay replay = new ScreenReplay(columns, rows).Replay(capture, EndFor(capture, marker));
        Console.Error.WriteLine($"replayed to {replay.Replayed} of {capture.Length} filled cells {replay.FilledCells}");
        foreach (string line in replay.Lines)
        {
            Console.WriteLine(line);
        }

        return 0;
    }

    private int Printable(char ch, int i)
    {
        switch (ch)
        {
            case '\r':
                column = 0;
                break;
            case '\n':
                row++;
                break;
            case '\x07' or '\x08':
                break;
            default:
                if (column >= columns)
                {
                    column = 0;
                    row++;
                }

                Put(row, column, ch);
                column++;
                break;
        }

        return i + 1;
    }

    private int Escape(string capture, int i)
    {
        int end = EscapeSequences.EndOf(capture, i);
        if (capture[i + 1] == '[')
        {
            ControlSequence(capture[(i + 2)..end]);
        }

        return end;
    }

    /// <summary>Applies a CSI sequence given without its introducer; one that is cut short, or whose final byte is not a letter this replay knows, changes nothing.</summary>
    private void ControlSequence(string body)
    {
        if (body.Length == 0 || body[^1] is not (>= '\x40' and <= '\x7E'))
        {
            return;
        }

        string parameters = new([.. body[..^1].Where(character => character is >= '\x30' and <= '\x3F')]);
        int[] numbers = [.. parameters.TrimStart('?', '<', '>', '=').Split(';').Select(n => int.TryParse(n, out int value) ? value : 0)];
        int first = numbers.Length > 0 && numbers[0] > 0 ? numbers[0] : 1;
        int second = numbers.Length > 1 && numbers[1] > 0 ? numbers[1] : 1;
        Apply(body[^1], first, second, numbers);
    }

    private void Apply(char final, int first, int second, int[] numbers)
    {
        switch (final)
        {
            case 'H' or 'f':
                row = first - 1;
                column = second - 1;
                break;
            case 'X':
                for (int k = 0; k < first; k++)
                {
                    Put(row, column + k, ' ');
                }

                break;
            case 'K':
                for (int k = column; k < columns; k++)
                {
                    Put(row, k, ' ');
                }

                break;
            case 'J' when numbers.Length > 0 && numbers[0] == 2:
                foreach (char[] line in grid)
                {
                    Array.Fill(line, ' ');
                }

                break;
            case 'C':
                column += first;
                break;
            case 'D':
                column -= first;
                break;
            case 'A':
                row -= first;
                break;
            case 'B':
                row += first;
                break;
            case 'G':
                column = first - 1;
                break;
            case 'd':
                row = first - 1;
                break;
            default:
                break;
        }
    }

    private void Put(int r, int c, char ch)
    {
        if (r >= 0 && r < rows && c >= 0 && c < columns)
        {
            grid[r][c] = ch;
        }
    }
}