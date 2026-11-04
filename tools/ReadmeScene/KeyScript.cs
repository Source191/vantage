using System.Globalization;

namespace Source191.Torni.ReadmeScene;

/// <summary>
/// Reads a key script: one step a line, <c>&lt;delay in ms&gt; type &lt;text&gt;</c>, <c>press &lt;key&gt;</c>, <c>focus</c>
/// or <c>mark &lt;name&gt;</c>; blank lines and lines starting with <c>#</c> are skipped.
/// </summary>
public static class KeyScript
{
    public static readonly TimeSpan TypingInterval = TimeSpan.FromMilliseconds(70);

    public static IReadOnlyList<KeyStep> Parse(string script) =>
    [
        .. script.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(Step),
    ];

    public static TimeSpan Duration(IReadOnlyList<KeyStep> steps) => steps.Aggregate(TimeSpan.Zero, (total, step) => total + step.Delay + Typing(step));

    private static TimeSpan Typing(KeyStep step) => step is KeyStep.Type type ? TypingInterval * type.Text.Length : TimeSpan.Zero;

    private static KeyStep Step(string line)
    {
        string[] parts = line.Split(' ', 3);
        if (parts.Length < 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int milliseconds))
        {
            throw new FormatException($"Not a key script line: {line}");
        }

        TimeSpan delay = TimeSpan.FromMilliseconds(milliseconds);
        return parts switch
        {
            [_, "type", string text] => new KeyStep.Type(delay, text),
            [_, "press", string key] when Enum.TryParse(key, ignoreCase: false, out NamedKey named) && Enum.IsDefined(named) => new KeyStep.Press(delay, named),
            [_, "press", string key] when key.Length == 1 => new KeyStep.Type(delay, key),
            [_, "focus"] => new KeyStep.FocusTorni(delay),
            [_, "mark", string name] => new KeyStep.Mark(delay, name),
            _ => throw new FormatException($"Not a key script line: {line}"),
        };
    }
}