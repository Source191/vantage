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
        HashSet<FolderPath> registered = [];
        List<FolderPath> repositories = [];
        List<FolderPath> candidates = [];

        foreach (FolderPath root in Paths(roots.Where(folders.DirectoryExists)))
        {
            foreach (FolderPath child in Paths(folders.Directories(root.Value)).OrderBy(child => child.Key, StringComparer.Ordinal))
            {
                string gitPath = IOPath.Combine(child.Value, ".git");
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

        foreach (FolderPath repository in repositories)
        {
            rows.AddRange(await RegisteredRowsAsync(repository, registered, cancellationToken));
            rows.AddRange(OrphanRows(repository, registered));
        }

        rows.AddRange(candidates.Where(candidate => !registered.Contains(candidate)).Select(BrokenSibling));

        return [.. rows.Select(row => WithLiveSession(row, LiveSessionIn(row.Path, sessions)))];
    }

    /// <summary>The paths among folder listings and configured roots that are full paths; anything else is skipped.</summary>
    private static IEnumerable<FolderPath> Paths(IEnumerable<string> texts) => texts.Select(FolderPath.TryParse).OfType<FolderPath>();

    private async Task<IReadOnlyList<WorktreeRow>> RegisteredRowsAsync(FolderPath repository, HashSet<FolderPath> registered, CancellationToken cancellationToken)
    {
        IReadOnlyList<GitWorktreeEntry> entries;
        try
        {
            entries = await git.ListWorktreesAsync(repository.Value, cancellationToken);
        }
        catch (GitException)
        {
            // One unusable folder must not blank the whole view.
            return [];
        }

        List<WorktreeRow> rows = [];
        foreach (GitWorktreeEntry entry in entries.Where(entry => !entry.IsMain))
        {
            if (FolderPath.TryParse(entry.Path) is not FolderPath path)
            {
                continue;
            }

            registered.Add(path);
            bool folderExists = entry.PrunableReason is null && folders.DirectoryExists(path.Value);
            WorktreeHealth health = WorktreeHealth.From(new WorktreeObservation(true, folderExists, entry.LockReason, false, null, false));
            rows.Add(folderExists
                ? await InspectedAsync(repository, path, entry, health, cancellationToken)
                : new WorktreeRow(repository, path, entry.Branch, health, 0, 0, null, null, null, 0, OwnerExists: true, FolderExists: false));
        }

        return rows;
    }

    private async Task<WorktreeRow> InspectedAsync(FolderPath repository, FolderPath path, GitWorktreeEntry entry, WorktreeHealth health, CancellationToken cancellationToken)
    {
        try
        {
            return new WorktreeRow(
                repository,
                path,
                entry.Branch,
                health,
                await git.CountChangedFilesAsync(path.Value, cancellationToken),
                await git.CountUnpushedCommitsAsync(path.Value, cancellationToken),
                await git.LastCommitAtAsync(path.Value, cancellationToken),
                SessionId: null,
                SessionName: null,
                LeftoverFileCount: 0,
                OwnerExists: true,
                FolderExists: true);
        }
        catch (GitException)
        {
            return new WorktreeRow(repository, path, entry.Branch, health, 0, 0, null, null, null, 0, OwnerExists: true, FolderExists: true);
        }
    }

    private IEnumerable<WorktreeRow> OrphanRows(FolderPath repository, HashSet<FolderPath> registered)
    {
        FolderPath claudeWorktrees = repository.Combine(IOPath.Combine(".claude", "worktrees"));
        if (!folders.DirectoryExists(claudeWorktrees.Value))
        {
            yield break;
        }

        foreach (FolderPath folder in Paths(folders.Directories(claudeWorktrees.Value)).OrderBy(folder => folder.Key, StringComparer.Ordinal))
        {
            if (registered.Contains(folder))
            {
                continue;
            }

            string gitFile = IOPath.Combine(folder.Value, ".git");
            string? target = folders.FileExists(gitFile) ? GitDirOf(gitFile) : null;
            bool? targetExists = folders.FileExists(gitFile) ? target is not null && folders.DirectoryExists(target) : null;
            WorktreeObservation observation = new(false, true, null, UnderClaudeWorktrees: true, targetExists, false);
            yield return Leftover(repository, folder, observation, ownerExists: true);
        }
    }

    /// <summary>A direct child of a root with a .git file that no repository under the roots lists.</summary>
    private WorktreeRow BrokenSibling(FolderPath folder)
    {
        string? gitDir = GitDirOf(IOPath.Combine(folder.Value, ".git"));
        FolderPath owner = OwnerOf(gitDir) ?? folder.Parent ?? folder;
        bool ownerExists = folders.DirectoryExists(IOPath.Combine(owner.Value, ".git"));
        WorktreeObservation observation = new(false, true, null, false, GitFileTargetExists: gitDir is not null && folders.DirectoryExists(gitDir), false);

        return Leftover(owner, folder, observation, ownerExists);
    }

    private WorktreeRow Leftover(FolderPath repository, FolderPath folder, WorktreeObservation observation, bool ownerExists) => new WorktreeRow(repository, folder, null, WorktreeHealth.From(observation), 0, 0, null, null, null, CountLeftoverFiles(folder), ownerExists, FolderExists: folders.DirectoryExists(folder.Value));

    private string? GitDirOf(string gitFile)
    {
        string? line = folders.Lines(gitFile).FirstOrDefault(candidate => candidate.StartsWith(GitDirPrefix, StringComparison.Ordinal));

        return line is null ? null : IOPath.GetFullPath(line[GitDirPrefix.Length..].Trim().Replace('/', IOPath.DirectorySeparatorChar));
    }

    private static FolderPath? OwnerOf(string? gitDir)
    {
        int at = gitDir?.IndexOf(WorktreesSegment, StringComparison.OrdinalIgnoreCase) ?? -1;

        return at < 0 ? null : FolderPath.TryParse(gitDir![..at]);
    }

    private bool IsEmptyLeftover(FolderPath folder)
    {
        bool leftoverName = folder.Name.EndsWith(".worktrees", StringComparison.OrdinalIgnoreCase);

        return leftoverName && folders.IsEmpty(folder.Value);
    }

    /// <summary>
    /// Files git no longer tracks, so would be lost with the folder; the .git pointer file itself does not count. Counting
    /// stops at the Domain limit so a leftover with a dependencies tree inside does not slow every scan.
    /// </summary>
    private int CountLeftoverFiles(FolderPath folder)
    {
        if (!folders.DirectoryExists(folder.Value))
        {
            return 0;
        }

        return folders.Files(folder.Value)
            .Where(file => !string.Equals(IOPath.GetFileName(file), ".git", StringComparison.OrdinalIgnoreCase))
            .Take(WorktreeRemovalFacts.LeftoverFileLimit)
            .Count();
    }

    private static Session? LiveSessionIn(FolderPath worktree, IReadOnlyList<Session> sessions)
    {
        return sessions
            .Where(session => session is InteractiveSession or BackgroundSession { ProcessId: not null })
            .FirstOrDefault(session => session.WorkingDirectory.IsUnder(worktree));
    }

    private static WorktreeRow WithLiveSession(WorktreeRow row, Session? live) => row with { SessionId = live?.SessionId, SessionName = live?.Name };
}