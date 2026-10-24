using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using IOPath = System.IO.Path;

namespace Source191.Vantage.Application;

/// <summary>
/// Everything under the repository roots that is, or was, a worktree: git's own listing per repository, plus the
/// leftovers git has forgotten (orphans under .claude\worktrees, siblings whose registration is gone, empty folders
/// that worktree tools leave behind). Read-only: nothing here prunes, deletes or touches git state.
/// </summary>
public sealed class WorktreeInventory(IGit git, IFolders folders, IReadOnlyList<string> roots)
{
    private const string GitDirPrefix = "gitdir:";
    private const string WorktreesSegment = ".git\\worktrees\\";

    public async Task<IReadOnlyList<WorktreeRow>> ScanAsync(IReadOnlyList<Session> sessions, CancellationToken cancellationToken)
    {
        List<WorktreeRow> rows = [];
        HashSet<string> registered = new(StringComparer.OrdinalIgnoreCase);
        List<string> repositories = [];
        List<string> candidates = [];

        foreach (string root in roots.Where(folders.DirectoryExists))
        {
            foreach (string child in folders.Directories(root).Order(StringComparer.OrdinalIgnoreCase))
            {
                string gitPath = IOPath.Combine(child, ".git");
                if (folders.DirectoryExists(gitPath))
                {
                    repositories.Add(child);
                }
                else if (folders.FileExists(gitPath))
                {
                    candidates.Add(child);
                }
                else if (IsEmptyLeftover(child))
                {
                    rows.Add(Leftover(root, child, new WorktreeObservation(false, true, null, false, null, EmptyLeftoverFolder: true), ownerExists: false));
                }
            }
        }

        foreach (string repository in repositories)
        {
            rows.AddRange(await RegisteredRowsAsync(repository, registered, cancellationToken));
            rows.AddRange(OrphanRows(repository, registered));
        }

        rows.AddRange(candidates.Where(candidate => !registered.Contains(Normalise(candidate))).Select(BrokenSibling));

        return [.. rows.Select(row => WithLiveSession(row, LiveSessionIn(row.Path, sessions)))];
    }

    private async Task<IReadOnlyList<WorktreeRow>> RegisteredRowsAsync(string repository, HashSet<string> registered, CancellationToken cancellationToken)
    {
        IReadOnlyList<GitWorktreeEntry> entries;
        try
        {
            entries = await git.ListWorktreesAsync(repository, cancellationToken);
        }
        catch (GitException)
        {
            // One unusable folder must not blank the whole view.
            return [];
        }

        List<WorktreeRow> rows = [];
        foreach (GitWorktreeEntry entry in entries.Where(entry => !entry.IsMain))
        {
            registered.Add(Normalise(entry.Path));
            bool folderExists = entry.PrunableReason is null && folders.DirectoryExists(entry.Path);
            WorktreeHealth health = WorktreeHealth.From(new WorktreeObservation(true, folderExists, entry.LockReason, false, null, false));
            rows.Add(folderExists
                ? await InspectedAsync(repository, entry, health, cancellationToken)
                : new WorktreeRow(repository, entry.Path, entry.Branch, health, 0, 0, null, null, null, 0, OwnerExists: true, FolderExists: false));
        }

        return rows;
    }

    private async Task<WorktreeRow> InspectedAsync(string repository, GitWorktreeEntry entry, WorktreeHealth health, CancellationToken cancellationToken)
    {
        try
        {
            return new WorktreeRow(
                repository,
                entry.Path,
                entry.Branch,
                health,
                await git.CountChangedFilesAsync(entry.Path, cancellationToken),
                await git.CountUnpushedCommitsAsync(entry.Path, cancellationToken),
                await git.LastCommitAtAsync(entry.Path, cancellationToken),
                SessionId: null,
                SessionName: null,
                LeftoverFileCount: 0,
                OwnerExists: true,
                FolderExists: true);
        }
        catch (GitException)
        {
            return new WorktreeRow(repository, entry.Path, entry.Branch, health, 0, 0, null, null, null, 0, OwnerExists: true, FolderExists: true);
        }
    }

    private IEnumerable<WorktreeRow> OrphanRows(string repository, HashSet<string> registered)
    {
        string claudeWorktrees = IOPath.Combine(repository, ".claude", "worktrees");
        if (!folders.DirectoryExists(claudeWorktrees))
        {
            yield break;
        }

        foreach (string folder in folders.Directories(claudeWorktrees).Order(StringComparer.OrdinalIgnoreCase))
        {
            if (registered.Contains(Normalise(folder)))
            {
                continue;
            }

            string gitFile = IOPath.Combine(folder, ".git");
            string? target = folders.FileExists(gitFile) ? GitDirOf(gitFile) : null;
            bool? targetExists = folders.FileExists(gitFile) ? target is not null && folders.DirectoryExists(target) : null;
            WorktreeObservation observation = new(false, true, null, UnderClaudeWorktrees: true, targetExists, false);
            yield return Leftover(repository, folder, observation, ownerExists: true);
        }
    }

    /// <summary>A direct child of a root with a .git file that no repository under the roots lists.</summary>
    private WorktreeRow BrokenSibling(string folder)
    {
        string? gitDir = GitDirOf(IOPath.Combine(folder, ".git"));
        string owner = OwnerOf(gitDir) ?? IOPath.GetDirectoryName(folder) ?? folder;
        bool ownerExists = folders.DirectoryExists(IOPath.Combine(owner, ".git"));
        WorktreeObservation observation = new(false, true, null, false, GitFileTargetExists: gitDir is not null && folders.DirectoryExists(gitDir), false);

        return Leftover(owner, folder, observation, ownerExists);
    }

    private WorktreeRow Leftover(string repository, string folder, WorktreeObservation observation, bool ownerExists) => new WorktreeRow(repository, folder, null, WorktreeHealth.From(observation), 0, 0, null, null, null, CountLeftoverFiles(folder), ownerExists, FolderExists: folders.DirectoryExists(folder));

    private string? GitDirOf(string gitFile)
    {
        string? line = folders.Lines(gitFile).FirstOrDefault(candidate => candidate.StartsWith(GitDirPrefix, StringComparison.Ordinal));

        return line is null ? null : IOPath.GetFullPath(line[GitDirPrefix.Length..].Trim().Replace('/', IOPath.DirectorySeparatorChar));
    }

    private static string? OwnerOf(string? gitDir)
    {
        int at = gitDir?.IndexOf(WorktreesSegment, StringComparison.OrdinalIgnoreCase) ?? -1;

        return at < 0 ? null : gitDir![..at].TrimEnd(IOPath.DirectorySeparatorChar);
    }

    private bool IsEmptyLeftover(string folder)
    {
        string name = IOPath.GetFileName(folder);
        bool leftoverName = name.EndsWith(".worktrees", StringComparison.OrdinalIgnoreCase);

        return leftoverName && folders.IsEmpty(folder);
    }

    /// <summary>
    /// Files git no longer tracks, so would be lost with the folder; the .git pointer file itself does not count. Counting
    /// stops at the Domain limit so a leftover with a dependencies tree inside does not slow every scan.
    /// </summary>
    private int CountLeftoverFiles(string folder)
    {
        if (!folders.DirectoryExists(folder))
        {
            return 0;
        }

        return folders.Files(folder)
            .Where(file => !string.Equals(IOPath.GetFileName(file), ".git", StringComparison.OrdinalIgnoreCase))
            .Take(WorktreeRemovalFacts.LeftoverFileLimit)
            .Count();
    }

    private static Session? LiveSessionIn(string worktree, IReadOnlyList<Session> sessions)
    {
        string prefix = Normalise(worktree) + IOPath.DirectorySeparatorChar;

        return sessions
            .Where(session => session.ProcessId is not null)
            .FirstOrDefault(session => (Normalise(session.WorkingDirectory) + IOPath.DirectorySeparatorChar).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static WorktreeRow WithLiveSession(WorktreeRow row, Session? live) => row with { SessionId = live?.SessionId, SessionName = live?.Name };

    private static string Normalise(string path) => IOPath.GetFullPath(path).TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar);
}