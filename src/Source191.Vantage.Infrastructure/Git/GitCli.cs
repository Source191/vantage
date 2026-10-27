using Source191.Vantage.Ports;
using System.ComponentModel;
using System.Globalization;

namespace Source191.Vantage.Adapters.Git;

public sealed class GitCli(IProcessRunner processes) : IGit
{
    // Windows' "The directory name is invalid": the working directory given to CreateProcess does not exist.
    private const int ErrorDirectory = 267;

    public async Task<IReadOnlyList<GitWorktreeEntry>> ListWorktreesAsync(string repository, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunOrThrowAsync(repository, ["worktree", "list", "--porcelain"], cancellationToken);

        return GitWorktreeListing.Parse(result.StandardOutput);
    }

    public Task AddWorktreeAsync(string repository, string path, string branch, string baseRef, CancellationToken cancellationToken) => RunOrThrowAsync(repository, ["worktree", "add", path, "-b", branch, baseRef], cancellationToken);

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

        // An unreadable count is a failed command too, so the scan's catch for GitException covers it.
        return int.TryParse(result.StandardOutput.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int count)
            ? count
            : throw new GitException($"git rev-list --count gave '{result.StandardOutput.Trim()}', not a number");
    }

    public async Task<DateTimeOffset?> LastCommitAtAsync(string worktree, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunAsync(worktree, ["log", "-1", "--format=%cI"], cancellationToken);

        return result.ExitCode == 0 && DateTimeOffset.TryParse(result.StandardOutput.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset at)
            ? at
            : null;
    }

    public Task DeleteBranchAsync(string repository, string branch, bool force, CancellationToken cancellationToken) => RunOrThrowAsync(repository, ["branch", force ? "-D" : "-d", branch], cancellationToken);

    public async Task<string?> DefaultRemoteBranchAsync(string repository, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunAsync(repository, ["symbolic-ref", "--short", "refs/remotes/origin/HEAD"], cancellationToken);

        return result.ExitCode == 0 ? result.StandardOutput.Trim() : null;
    }

    public async Task<bool> IsMergedAsync(string repository, string branch, string into, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunAsync(repository, ["merge-base", "--is-ancestor", branch, into], cancellationToken);

        return result.ExitCode == 0;
    }

    public async Task<string?> CurrentBranchAsync(string directory, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunAsync(directory, ["branch", "--show-current"], cancellationToken);
        string branch = result.StandardOutput.Trim();

        return result.ExitCode == 0 && branch.Length > 0 ? branch : null;
    }

    public async Task<string?> RepositoryRootAsync(string directory, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunAsync(directory, ["rev-parse", "--show-toplevel"], cancellationToken);

        return result.ExitCode == 0 ? ToWindowsPath(result.StandardOutput) : null;
    }

    public async Task<string?> MainCheckoutOfAsync(string directory, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunAsync(directory, ["rev-parse", "--path-format=absolute", "--git-common-dir"], cancellationToken);
        if (result.ExitCode != 0)
        {
            return null;
        }

        return Path.GetDirectoryName(ToWindowsPath(result.StandardOutput));
    }

    public async Task<string?> OriginUrlAsync(string repository, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunAsync(repository, ["remote", "get-url", "origin"], cancellationToken);

        return result.ExitCode == 0 ? result.StandardOutput.Trim() : null;
    }

    public async Task<IReadOnlyList<string>> ListIgnoredFilesAsync(string repository, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunOrThrowAsync(repository, ["ls-files", "--others", "--ignored", "--exclude-standard"], cancellationToken);

        return Lines(result.StandardOutput);
    }

    private async Task<ProcessResult> RunOrThrowAsync(string directory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunAsync(directory, arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new GitException($"git {string.Join(' ', arguments)} failed: {result.StandardError.Trim()}");
        }

        return result;
    }

    // A folder deleted by hand (a removed worktree) stops git starting at all, so it answers like any failed git run.
    // Any other start failure (git not installed is ERROR_FILE_NOT_FOUND) still surfaces.
    private async Task<ProcessResult> RunAsync(string directory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        try
        {
            return await processes.RunAsync(new ProcessRequest("git", arguments, directory), cancellationToken);
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == ErrorDirectory)
        {
            return new ProcessResult(ExitCode: -1, StandardOutput: string.Empty, StandardError: $"the folder {directory} does not exist");
        }
    }

    private static List<string> Lines(string output) => [.. output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    private static string ToWindowsPath(string output) => output.Trim().Replace('/', Path.DirectorySeparatorChar);
}