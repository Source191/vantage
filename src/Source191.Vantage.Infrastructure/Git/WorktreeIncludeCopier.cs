using Microsoft.Extensions.FileSystemGlobbing;
using Source191.Vantage.Ports;
namespace Source191.Vantage.Adapters.Git;

/// <summary>
/// Copies the gitignored files a repository's .worktreeinclude names into a new worktree, the way Claude Code
/// does for the worktrees it creates itself. Patterns use the globbing subset of .gitignore syntax.
/// </summary>
public sealed class WorktreeIncludeCopier(IGit git) : IWorktreeIncludeCopier
{
    private const string IncludeFileName = ".worktreeinclude";

    public async Task<IReadOnlyList<string>> CopyAsync(string repository, string worktree, CancellationToken cancellationToken)
    {
        string includeFile = Path.Combine(repository, IncludeFileName);
        if (!File.Exists(includeFile))
        {
            return [];
        }

        IReadOnlyList<string> patterns = await ReadPatternsAsync(includeFile, cancellationToken);
        IReadOnlyList<string> ignored = await git.ListIgnoredFilesAsync(repository, cancellationToken);
        Matcher matcher = new();
        matcher.AddIncludePatterns(patterns.Select(ToGlob));

        List<string> copied = [];
        foreach (string relative in matcher.Match(ignored).Files.Select(match => match.Path))
        {
            string source = Path.Combine(repository, relative.Replace('/', Path.DirectorySeparatorChar));
            string target = Path.Combine(worktree, relative.Replace('/', Path.DirectorySeparatorChar));
            if (Path.GetDirectoryName(target) is string folder)
            {
                Directory.CreateDirectory(folder);
            }

            File.Copy(source, target, overwrite: true);
            copied.Add(relative);
        }

        return copied;
    }

    /// <summary>
    /// .gitignore rules differ from globs in two ways that matter here: a pattern with no slash matches at any
    /// depth, and a trailing slash names a whole directory. A leading slash anchors to the root, as a glob already does.
    /// </summary>
    private static string ToGlob(string pattern)
    {
        string glob = pattern.TrimStart('/');
        if (glob.EndsWith('/'))
        {
            glob += "**";
        }

        bool anchored = pattern.StartsWith('/');
        bool hasSlash = glob.TrimEnd('/').Contains('/');

        return anchored || hasSlash ? glob : $"**/{glob}";
    }

    private static async Task<IReadOnlyList<string>> ReadPatternsAsync(string includeFile, CancellationToken cancellationToken)
    {
        string[] lines = await File.ReadAllLinesAsync(includeFile, cancellationToken);

        return [.. lines
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))];
    }
}