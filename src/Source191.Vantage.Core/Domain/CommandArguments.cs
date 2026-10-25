using System.Text;

namespace Source191.Vantage.Domain;

/// <summary>
/// A custom editor's arguments as the settings dialog shows them on one line: spaces separate, double quotes keep
/// text with spaces together. The list is what is stored, so this is the only place a line becomes arguments.
/// </summary>
public static class CommandArguments
{
    public static IReadOnlyList<string> Split(string line)
    {
        List<string> arguments = [];
        StringBuilder current = new();
        bool quoted = false;
        bool started = false;

        foreach (char character in line)
        {
            switch (character)
            {
                case '"':
                    quoted = !quoted;
                    started = true;
                    break;
                case ' ' when !quoted:
                    Finish(arguments, current, ref started);
                    break;
                default:
                    current.Append(character);
                    started = true;
                    break;
            }
        }

        Finish(arguments, current, ref started);

        return arguments;
    }

    public static string Join(IReadOnlyList<string> arguments) => string.Join(' ', arguments.Select(argument => argument.Contains(' ') ? $"\"{argument}\"" : argument));

    private static void Finish(List<string> arguments, StringBuilder current, ref bool started)
    {
        if (started)
        {
            arguments.Add(current.ToString());
        }

        current.Clear();
        started = false;
    }
}
