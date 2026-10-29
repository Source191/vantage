using System.Text.RegularExpressions;

namespace Source191.Vantage.Domain;

/// <summary>
/// What a batch file may be given as an argument. Windows runs a .cmd through cmd.exe, which reads the whole command
/// line again: a double quote toggles quoting, an ampersand, a pipe, an angle bracket or a caret outside quotes starts
/// a command, and %NAME% expands everywhere. Only letters, digits, space, dot, underscore, hyphen and slash go through.
/// </summary>
public static partial class BatchFileArgument
{
    public static bool IsSafe(string text) => Plain().IsMatch(text);

    [GeneratedRegex("^[A-Za-z0-9 ._/-]+$")]
    private static partial Regex Plain();
}