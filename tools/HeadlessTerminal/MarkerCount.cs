using System.Text;
using System.Text.Json;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>Counts plain-text markers in a capture and shows the text around the first one.</summary>
public static class MarkerCount
{
    public static int Of(string plain, string marker)
    {
        int count = 0;
        int index = -1;
        while ((index = plain.IndexOf(marker, index + 1, StringComparison.Ordinal)) >= 0)
        {
            count++;
        }

        return count;
    }

    public static string Around(string plain, string marker, int before = 100, int after = 700)
    {
        int first = plain.IndexOf(marker, StringComparison.Ordinal);

        return first < 0 ? string.Empty : plain[Math.Max(0, first - before)..Math.Min(plain.Length, first + after)];
    }

    public static int PrintFile(string rawFile, string[] markers)
    {
        string plain = EscapeSequences.Strip(File.ReadAllText(rawFile, Encoding.UTF8));
        foreach (string marker in markers)
        {
            Console.WriteLine($"{JsonSerializer.Serialize(marker)} {Of(plain, marker)}");
        }

        if (markers.Length > 0)
        {
            Console.WriteLine(JsonSerializer.Serialize(Around(plain, markers[0])));
        }

        return 0;
    }
}