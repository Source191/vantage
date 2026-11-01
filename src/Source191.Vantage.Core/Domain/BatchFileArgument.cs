using System.Buffers;

namespace Source191.Vantage.Domain;

/// <summary>
/// What a batch file may be given as an argument.
/// <see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw">Windows
/// runs a .cmd through cmd.exe</see>, which
/// <see href="https://learn.microsoft.com/windows-server/administration/windows-commands/cmd">reads the whole command
/// line again</see>: a double quote toggles quoting, an ampersand, a pipe, an angle bracket or a caret outside quotes starts
/// a command, and %NAME% expands everywhere. Only letters, digits, space, dot, underscore, hyphen and slash go through.
/// </summary>
public static class BatchFileArgument
{
    private static readonly SearchValues<char> Plain = SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ._/-");

    public static bool IsSafe(string text) => text.Length > 0 && !text.AsSpan().ContainsAnyExcept(Plain);
}