using System.Text;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>Builds a Windows command line the C runtime parses back into the same arguments.</summary>
public static class CommandLine
{
    public static string Build(string exe, IReadOnlyList<string> arguments) => string.Join(' ', arguments.Prepend(exe).Select(Quote));

    public static string Quote(string argument)
    {
        if (argument.Length > 0 && !argument.Any(c => char.IsWhiteSpace(c) || c == '"'))
        {
            return argument;
        }

        StringBuilder quoted = new("\"");
        int backslashes = 0;
        foreach (char c in argument)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }

            int escaped = c == '"' ? backslashes * 2 + 1 : backslashes;
            quoted.Append('\\', escaped).Append(c);
            backslashes = 0;
        }

        return quoted.Append('\\', backslashes * 2).Append('"').ToString();
    }
}