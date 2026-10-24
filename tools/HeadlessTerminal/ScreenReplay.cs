using System.Globalization;
using System.Text;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// Replays a capture into a character grid, honouring cursor positioning, erase-characters, erase-line, clear-screen
/// and relative cursor moves, so Terminal.Gui's cell-by-cell output reads as lines. The replay stops just after the
/// last occurrence of a marker (or at the alternate-screen exit that follows it), so a screen can be read as it was.
/// </summary>
public sealed class ScreenReplay(int columns, int rows)
{
    private readonly char[][] grid = Enumerable.Range(0, rows).Select(_ => Enumerable.Repeat(' ', columns).ToArray()).ToArray();
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
        char next = capture[i + 1];
        if (next == '[')
        {
            return ControlSequence(capture, i);
        }

        if (next is ']' or 'P')
        {
            int bel = capture.IndexOf('\x07', i);
            int st = capture.IndexOf("\x1b\\", i, StringComparison.Ordinal);
            int terminator = st >= 0 && (bel < 0 || st < bel) ? st + 1 : bel;

            return terminator < 0 ? capture.Length : terminator + 1;
        }

        return i + 2;
    }

    private int ControlSequence(string capture, int i)
    {
        int j = i + 2;
        while (j < capture.Length && !IsFinal(capture[j]))
        {
            j++;
        }

        if (j >= capture.Length)
        {
            return capture.Length;
        }

        int[] numbers = capture[(i + 2)..j].TrimStart('?', '<', '>', '=').Split(';').Select(n => int.TryParse(n, out int value) ? value : 0).ToArray();
        int first = numbers.Length > 0 && numbers[0] > 0 ? numbers[0] : 1;
        int second = numbers.Length > 1 && numbers[1] > 0 ? numbers[1] : 1;
        Apply(capture[j], first, second, numbers);

        return j + 1;
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

    private static bool IsFinal(char ch) => char.IsAsciiLetter(ch) || ch is '_' or '@' or '`';
}