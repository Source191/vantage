namespace Source191.Vantage.Domain;

/// <summary>
/// The short name of a program a session started, for the memory breakdown: the package a runner such as npx or uvx
/// was asked for, the program a command shell wraps, else the executable's own name. The label is the only thing kept
/// from a command line, which may carry secrets.
/// </summary>
public static class ProgramLabel
{
    private const string CommandShell = "cmd";
    private const string ExecutableExtension = ".exe";
    private static readonly string[] PackageRunners = ["npx", "uvx"];

    public static string From(string name, string commandLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(commandLine);

        // cmd.exe passes quotes through as ^" and wraps its command in quotes; neither says anything about the program.
        string[] tokens = commandLine.Replace("^", string.Empty, StringComparison.Ordinal).Replace("\"", string.Empty, StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return PackageAfterRunner(tokens) ?? WrappedExecutable(name, tokens) ?? name;
    }

    private static string? PackageAfterRunner(string[] tokens)
    {
        int runner = Array.FindIndex(tokens, token => PackageRunners.Contains(token, StringComparer.OrdinalIgnoreCase));
        if (runner < 0)
        {
            return null;
        }

        string? package = tokens.Skip(runner + 1).FirstOrDefault(token => !token.StartsWith('-'));

        return package is null ? null : WithoutVersion(package);
    }

    /// <summary>"@scope/name@1.2.3" and "name@latest" lose the version; the "@" that starts a scope stays.</summary>
    private static string WithoutVersion(string package)
    {
        int version = package.LastIndexOf('@');

        return version > 0 ? package[..version] : package;
    }

    /// <summary>A command shell's label is the first executable it names other than itself.</summary>
    private static string? WrappedExecutable(string name, string[] tokens)
    {
        if (!string.Equals(name, CommandShell, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return tokens
            .Where(token => token.EndsWith(ExecutableExtension, StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault(executable => !string.Equals(executable, CommandShell, StringComparison.OrdinalIgnoreCase));
    }
}