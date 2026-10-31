using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
namespace Source191.Vantage.Adapters.Git;

/// <summary>
/// Parses "git worktree list --porcelain": blank-line separated blocks of "worktree", "HEAD", then
/// "branch" or "detached", with optional "locked [reason]" and "prunable reason" lines.
/// </summary>
public static class GitWorktreeListing
{
    private const string RefsHeads = "refs/heads/";

    public static IReadOnlyList<GitWorktreeEntry> Parse(string porcelain)
    {
        string[] blocks = porcelain
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return [.. blocks.Select((block, index) => ParseBlock(block, isMain: index == 0))];
    }

    private static GitWorktreeEntry ParseBlock(string block, bool isMain)
    {
        string path = string.Empty;
        string commit = string.Empty;
        Head head = new Head.Unknown();
        string? lockReason = null;
        string? prunableReason = null;

        foreach (string line in block.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            (string key, string value) = SplitLine(line);
            switch (key)
            {
                case "worktree":
                    path = value.Replace('/', System.IO.Path.DirectorySeparatorChar);
                    break;
                case "HEAD":
                    commit = value;
                    break;
                case "branch":
                    head = HeadOn(value);
                    break;
                case "detached":
                    head = new Head.Detached();
                    break;
                case "locked":
                    lockReason = value;
                    break;
                case "prunable":
                    prunableReason = value;
                    break;
                default:
                    break;
            }
        }

        return new GitWorktreeEntry(path, commit, head, isMain, lockReason, prunableReason);
    }

    /// <summary>The branch a "branch" line names, without its refs/heads/ prefix; Unknown when that is not a branch name.</summary>
    private static Head HeadOn(string reference)
    {
        string branch = reference.StartsWith(RefsHeads, StringComparison.Ordinal) ? reference[RefsHeads.Length..] : reference;

        return BranchName.TryParse(branch, out BranchName? name) ? new Head.OnBranch(name) : new Head.Unknown();
    }

    private static (string Key, string Value) SplitLine(string line)
    {
        int space = line.IndexOf(' ', StringComparison.Ordinal);

        return space < 0 ? (line, string.Empty) : (line[..space], line[(space + 1)..]);
    }
}