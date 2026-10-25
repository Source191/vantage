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
        string head = string.Empty;
        string? branch = null;
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
                    head = value;
                    break;
                case "branch":
                    branch = value.StartsWith(RefsHeads, StringComparison.Ordinal) ? value[RefsHeads.Length..] : value;
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

        BranchName? branchName = BranchName.TryParse(branch, out BranchName? parsed) ? parsed : null;

        return new GitWorktreeEntry(path, head, branchName, isMain, lockReason, prunableReason);
    }

    private static (string Key, string Value) SplitLine(string line)
    {
        int space = line.IndexOf(' ', StringComparison.Ordinal);

        return space < 0 ? (line, string.Empty) : (line[..space], line[(space + 1)..]);
    }
}