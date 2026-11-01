namespace Source191.Vantage.Application.Diagnostics;

/// <summary>The subcommands and switches of each program that may appear in a diagnostic record; anything else is left out.</summary>
public static class ChildCommandWords
{
    public static readonly IReadOnlySet<string> Git = new HashSet<string>(StringComparer.Ordinal)
    {
        "worktree", "list", "add", "remove", "prune", "status", "rev-list", "log", "branch", "symbolic-ref", "merge-base", "rev-parse", "remote", "get-url", "ls-files", "-c", "core.quotePath=false",
    };

    public static readonly IReadOnlySet<string> Claude = new HashSet<string>(StringComparer.Ordinal)
    {
        "agents", "attach", "stop", "rm", "--version", "--bg", "--resume", "--json", "--all",
    };

    public static readonly IReadOnlySet<string> Gh = new HashSet<string>(StringComparer.Ordinal) { "pr", "list", "auth", "status" };

    public static readonly IReadOnlySet<string> Az = new HashSet<string>(StringComparer.Ordinal) { "repos", "pr", "list", "extension", "account", "show" };

    public static readonly IReadOnlySet<string> Wt = new HashSet<string>(StringComparer.Ordinal) { "-w", "0", "new-tab" };

    public static readonly IReadOnlySet<string> Dotnet = new HashSet<string>(StringComparer.Ordinal) { "tool", "update", "-g" };

    public static readonly IReadOnlySet<string> None = new HashSet<string>(StringComparer.Ordinal);
}