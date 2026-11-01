using System.Globalization;
using System.Text.Json;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// Parses "delayMs:keys,delayMs:keys,..." where each delay is relative to the previous step and keys use JSON
/// string escapes, so "\r" is Enter, "\u001a" is Ctrl+Z and "\u001b[B" is Down.
/// </summary>
public static class KeystrokeScript
{
    public static IReadOnlyList<Keystroke> Parse(string script)
    {
        List<Keystroke> steps = [];
        int at = 0;
        foreach (string step in script.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = step.IndexOf(':', StringComparison.Ordinal);
            if (colon < 0)
            {
                throw new FormatException($"A step needs a delay and keys separated by a colon: {step}");
            }

            at += int.Parse(step[..colon], CultureInfo.InvariantCulture);
            string keys = JsonSerializer.Deserialize<string>($"\"{step[(colon + 1)..]}\"") ?? string.Empty;
            steps.Add(new Keystroke(at, keys));
        }

        return steps;
    }
}