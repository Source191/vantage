namespace Source191.Vantage.Domain;

/// <summary>
/// Plays terminal output onto a grid of cells, honouring carriage return and line feed, wrapping at the right edge,
/// cursor positioning, cursor-forward and -back, erase-characters, erase-line and clear-screen; colours and every other
/// sequence are passed over. Enough of a terminal to read a screen that was drawn for one, not to run a program in.
/// </summary>
public sealed class TerminalReplay
{
    private readonly int columns;
    private readonly char[][] grid;
    private int row;
    private int column;

    private TerminalReplay(int columns, int rows)
    {
        this.columns = columns;
        grid = [.. Enumerable.Range(0, rows).Select(_ => Enumerable.Repeat(' ', columns).ToArray())];
    }

    /// <summary>Every row of the grid after the output has been played, trimmed at its end; a row never written is empty.</summary>
    public static IReadOnlyList<string> Play(string output, int columns, int rows)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        TerminalReplay replay = new(columns, rows);
        int at = 0;
        while (at < output.Length)
        {
            at = output[at] == '\x1b' && at + 1 < output.Length ? replay.Escape(output, at) : replay.Printable(output[at], at);
        }

        return [.. replay.grid.Select(line => new string(line).TrimEnd())];
    }

    private int Printable(char character, int at)
    {
        switch (character)
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

                Put(column, character);
                column++;
                break;
        }

        return at + 1;
    }

    private int Escape(string output, int at)
    {
        char next = output[at + 1];
        if (next == '[')
        {
            return ControlSequence(output, at);
        }

        if (next is ']' or 'P')
        {
            int bell = output.IndexOf('\x07', at);
            int terminator = output.IndexOf("\x1b\\", at, StringComparison.Ordinal);
            int end = terminator >= 0 && (bell < 0 || terminator < bell) ? terminator + 1 : bell;

            return end < 0 ? output.Length : end + 1;
        }

        return at + 2;
    }

    private int ControlSequence(string output, int at)
    {
        int final = at + 2;
        while (final < output.Length && !IsFinal(output[final]))
        {
            final++;
        }

        if (final >= output.Length)
        {
            return output.Length;
        }

        int[] numbers = [.. output[(at + 2)..final].TrimStart('?', '<', '>', '=').Split(';').Select(text => int.TryParse(text, out int value) ? value : 0)];
        Apply(output[final], numbers);

        return final + 1;
    }

    private void Apply(char final, int[] numbers)
    {
        int first = numbers.Length > 0 && numbers[0] > 0 ? numbers[0] : 1;
        switch (final)
        {
            case 'H' or 'f':
                row = first - 1;
                column = (numbers.Length > 1 && numbers[1] > 0 ? numbers[1] : 1) - 1;
                break;
            case 'X':
                Blank(column, column + first);
                break;
            case 'K':
                Blank(column, columns);
                break;
            case 'J' when numbers.Length > 0 && numbers[0] == 2:
                Array.ForEach(grid, line => Array.Fill(line, ' '));
                break;
            case 'C':
                column += first;
                break;
            case 'D':
                column = Math.Max(column - first, 0);
                break;
            default:
                break;
        }
    }

    private void Blank(int from, int to)
    {
        for (int cell = from; cell < to; cell++)
        {
            Put(cell, ' ');
        }
    }

    /// <summary>A cell off the grid is dropped: a screen taller or wider than the grid loses only what does not fit.</summary>
    private void Put(int cell, char character)
    {
        if (row >= 0 && row < grid.Length && cell >= 0 && cell < columns)
        {
            grid[row][cell] = character;
        }
    }

    private static bool IsFinal(char character) => character is >= '@' and <= '~';
}