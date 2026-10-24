using Source191.Vantage.Adapters.Processes;
using System.Globalization;

namespace Source191.Vantage.Adapters.Git;

public sealed class GitCli(IProcessRunner processes) : IGit
{
    public async Task<IReadOnlyList<GitWorktreeEntry>> ListWorktreesAsync(string repository, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunOrThrowAsync(repository, ["worktree", "list", "--porcelain"], cancellationToken);

        return GitWorktreeListing.Parse(result.StandardOutput);
    }

    public Task AddWorktreeAsync(string repository, string path, string branch, string baseRef, CancellationToken cancellationToken)
    {
        return RunOrThrowAsync(repository, ["worktree", "add", path, "-b", branch, baseRef], cancellationToken);
    }

    public Task RemoveWorktreeAsync(string repository, string path, bool force, CancellationToken cancellationToken)
    {
        List<string> arguments = ["worktree", "remove"];
        if (force)
        {
            arguments.Add("--force");
        }

        arguments.Add(path);

        return RunOrThrowAsync(repository, arguments, cancellationToken);
    }

    public async Task<int> PruneWorktreesAsync(string repository, CancellationToken cancellationToken)
    {
        // Git 2.55 on Windows prints nothing for --verbose, so the count comes from the listings either side.
        // Without --expire now, git keeps stale registrations younger than gc.worktreePruneExpire (three months).
        int before = (await ListWorktreesAsync(repository, cancellationToken)).Count;
        await RunOrThrowAsync(repository, ["worktree", "prune", "--expire", "now"], cancellationToken);
        int after = (await ListWorktreesAsync(repository, cancellationToken)).Count;

        return before - after;
    }

    public async Task<int> CountChangedFilesAsync(string worktree, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunOrThrowAsync(worktree, ["status", "--porcelain"], cancellationToken);

        return Lines(result.StandardOutput).Count;
    }

    public async Task<int> CountUnpushedCommitsAsync(string worktree, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunOrThrowAsync(worktree, ["rev-list", "--count", "HEAD", "--not", "--remotes"], cancellationToken);

        return int.Parse(result.StandardOutput.Trim(), CultureInfo.InvariantCulture);
    }

    public async Task<DateTimeOffset?> LastCommitAtAsync(string worktree, CancellationToken cancellationToken)
    {
        ProcessResult result = await processes.RunAsync(Request(worktree, ["log", "-1", "--format=%cI"]), cancellationToken);

        return result.ExitCode == 0 && DateTimeOffset.TryParse(result.StandardOutput.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset at)
            ? at
            : null;
    }

    public Task DeleteBranchAsync(string repository, string branch, bool force, CancellationToken cancellationToken)
    {
        return RunOrThrowAsync(repository, ["branch", force ? "-D" : "-d", branch], cancellationToken);
    }

    public async Task<string?> DefaultRemoteBranchAsync(string repository, CancellationToken cancellationToken)
    {
        ProcessResult result = await processes.RunAsync(Request(repository, ["symbolic-ref", "--short", "refs/remotes/origin/HEAD"]), cancellationToken);

        return result.ExitCode == 0 ? result.StandardOutput.Trim() : null;
    }

    public async Task<bool> IsMergedAsync(string repository, string branch, string into, CancellationToken cancellationToken)
    {
        ProcessResult result = await processes.RunAsync(Request(repository, ["merge-base", "--is-ancestor", branch, into]), cancellationToken);

        return result.ExitCode == 0;
    }

    public async Task<string?> CurrentBranchAsync(string directory, CancellationToken cancellationToken)
    {
        ProcessResult result = await processes.RunAsync(Request(directory, ["branch", "--show-current"]), cancellationToken);
        string branch = result.StandardOutput.Trim();

        return result.ExitCode == 0 && branch.Length > 0 ? branch : null;
    }

    public async Task<string?> RepositoryRootAsync(string directory, CancellationToken cancellationToken)
    {
        ProcessResult result = await processes.RunAsync(Request(directory, ["rev-parse", "--show-toplevel"]), cancellationToken);

        return result.ExitCode == 0 ? ToWindowsPath(result.StandardOutput) : null;
    }

    public async Task<string?> MainCheckoutOfAsync(string directory, CancellationToken cancellationToken)
    {
        ProcessResult result = await processes.RunAsync(Request(directory, ["rev-parse", "--path-format=absolute", "--git-common-dir"]), cancellationToken);
        if (result.ExitCode != 0)
        {
            return null;
        }

        return Path.GetDirectoryName(ToWindowsPath(result.StandardOutput));
    }

    public async Task<IReadOnlyList<string>> ListIgnoredFilesAsync(string repository, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunOrThrowAsync(repository, ["ls-files", "--others", "--ignored", "--exclude-standard"], cancellationToken);

        return Lines(result.StandardOutput);
    }

    private async Task<ProcessResult> RunOrThrowAsync(string directory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        ProcessResult result = await processes.RunAsync(Request(directory, arguments), cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new GitException($"git {string.Join(' ', arguments)} failed: {result.StandardError.Trim()}");
        }

        return result;
    }

    private static ProcessRequest Request(string directory, IReadOnlyList<string> arguments)
    {
        return new ProcessRequest("git", arguments, directory);
    }

    private static List<string> Lines(string output)
    {
        return [.. output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    private static string ToWindowsPath(string output)
    {
        return output.Trim().Replace('/', Path.DirectorySeparatorChar);
    }
}