using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using System.Diagnostics.CodeAnalysis;
using IOPath = System.IO.Path;

namespace Source191.Torni.Application;

/// <summary>
/// Everything under the repository roots and the worktree root that is, or was, a worktree: git's own listing per repository, plus the
/// leftovers git has forgotten (orphans under .claude\worktrees, folders under a root or the worktree root whose registration is gone, empty folders
/// that worktree tools leave behind). Read-only: nothing here prunes, deletes or touches git state.
/// </summary>
public sealed class WorktreeInventory(IGit git, IFolders folders, CurrentSettings settings, PullRequestCatalogue pullRequests, DiagnosticLog log)
{
    private readonly WorktreeInspector inspector = new(git);
    private readonly OncePerFolder unreadableRepositories = new(log, FailureSite.Load, CodeName.For(typeof(WorktreeInventory)));
    private readonly OncePerFolder unreadableWorktrees = new(log, FailureSite.Load, CodeName.For(typeof(WorktreeInventory)));

    private const string GitDirPrefix = "gitdir:";

    /// <summary>What a child of a repository root is: a repository, a worktree whose registration may be gone, an empty folder a tool left behind, or none of these.</summary>
    private enum FolderKind
    {
        Repository,
        Candidate,
        EmptyLeftover,
        Other,
    }

    public async Task<IReadOnlyList<WorktreeRow>> ScanAsync(IReadOnlyList<Session> sessions, CancellationToken cancellationToken)
    {
        Settings snapshot = settings.Value;
        List<WorktreeRow> rows = [];
        HashSet<FolderPath> registered = [];
        List<FolderPath> repositories = [];
        List<FolderPath> candidates = [];

        foreach (FolderPath root in snapshot.RepositoryRoots.Where(root => folders.DirectoryExists(root.Value)))
        {
            foreach (FolderPath child in Paths(folders.Directories(root.Value)).OrderBy(child => child.Key, StringComparer.Ordinal))
            {
                switch (Classify(child))
                {
                    case FolderKind.Repository:
                        repositories.Add(child);
                        break;
                    case FolderKind.Candidate:
                        candidates.Add(child);
                        break;
                    case FolderKind.EmptyLeftover:
                        rows.Add(Leftover(root, child, new WorktreeObservation.Unregistered(new GitPointer.NoGitFile(), UnderClaudeWorktrees: false, EmptyLeftover: true), ownerExists: false));
                        break;
                    case FolderKind.Other:
                        break;
                }
            }
        }

        candidates.AddRange(WorktreeRootCandidates(snapshot));

        foreach (FolderPath repository in repositories)
        {
            rows.AddRange(await RegisteredRowsAsync(repository, registered, cancellationToken));
            rows.AddRange(OrphanRows(repository, registered));
        }

        rows.AddRange(candidates.Where(candidate => !registered.Contains(candidate)).Select(BrokenWorktree));

        // Never awaited: the rows show what the catalogue already holds, and the next rescan picks up what lands.
        _ = pullRequests.RefreshStaleAsync(BranchesOf(rows));

        return [.. rows.Select(row => WithPullRequest(row with { Session = LiveSessionIn(row.Path, sessions) }))];
    }

    private FolderKind Classify(FolderPath child)
    {
        string gitPath = IOPath.Combine(child.Value, ".git");
        if (folders.DirectoryExists(gitPath))
        {
            return FolderKind.Repository;
        }

        if (folders.FileExists(gitPath))
        {
            return FolderKind.Candidate;
        }

        return IsEmptyLeftover(child) ? FolderKind.EmptyLeftover : FolderKind.Other;
    }

    /// <summary>Each row's repository and branch, for the rows whose head is on one.</summary>
    private static IEnumerable<(FolderPath Checkout, BranchName Branch)> BranchesOf(IEnumerable<WorktreeRow> rows)
    {
        foreach (WorktreeRow row in rows)
        {
            if (row.Head is Head.OnBranch on)
            {
                yield return (row.Repository, on.Branch);
            }
        }
    }

    /// <summary>A leftover's head is Unknown, so the catalogue answers no branch to look up, as the row's default does.</summary>
    private WorktreeRow WithPullRequest(WorktreeRow row) => row with { PullRequest = pullRequests.Find(row.Repository, row.Head) };

    /// <summary>The paths among folder listings that are full paths; anything else is skipped.</summary>
    private static IEnumerable<FolderPath> Paths(IEnumerable<string> texts) => texts.Parsed<FolderPath>();

    /// <summary>
    /// The worktree root's children with a .git file: when no repository lists them, broken, or elsewhere when their gitdir
    /// still exists (see <see cref="BrokenWorktree"/>). A root that is also a repository root was scanned already.
    /// </summary>
    private IEnumerable<FolderPath> WorktreeRootCandidates(Settings snapshot)
    {
        FolderPath worktreeRoot = snapshot.WorktreeRoot;
        if (snapshot.RepositoryRoots.Contains(worktreeRoot) || !folders.DirectoryExists(worktreeRoot.Value))
        {
            return [];
        }

        return Paths(folders.Directories(worktreeRoot.Value))
            .Where(child => folders.FileExists(IOPath.Combine(child.Value, ".git")))
            .OrderBy(child => child.Key, StringComparer.Ordinal);
    }

    private async Task<IReadOnlyList<WorktreeRow>> RegisteredRowsAsync(FolderPath repository, HashSet<FolderPath> registered, CancellationToken cancellationToken)
    {
        // One unusable folder must not blank the whole view.
        IReadOnlyList<GitWorktreeEntry> entries = await unreadableRepositories.RunAsync<IReadOnlyList<GitWorktreeEntry>>(repository, () => git.ListWorktreesAsync(repository.Value, cancellationToken), IsGitFailure, _ => []);

        List<WorktreeRow> rows = [];
        foreach (GitWorktreeEntry entry in entries.Where(entry => !entry.IsMain))
        {
            if (!FolderPath.TryParse(entry.Path, out FolderPath? path))
            {
                continue;
            }

            registered.Add(path);
            // The disc decides whether the folder exists; git's prunable says only that its .git file is gone, and such a
            // folder may still hold work, so it is counted like any leftover.
            bool folderExists = folders.DirectoryExists(path.Value);
            WorktreeHealth health = WorktreeHealth.From(new WorktreeObservation.Registered(folderExists, entry.Lock, entry.IsPrunable));
            rows.Add(folderExists && !entry.IsPrunable
                ? await InspectedAsync(repository, path, entry, health, cancellationToken)
                : new WorktreeRow(repository, path, entry.Head, health, 0, 0, new LastCommit.Unknown(), new WorktreeSession.None(), CountLeftoverFiles(path), OwnerExists: true, FolderExists: folderExists));
        }

        return rows;
    }

    private async Task<WorktreeRow> InspectedAsync(FolderPath repository, FolderPath path, GitWorktreeEntry entry, WorktreeHealth health, CancellationToken cancellationToken)
    {
        return await unreadableWorktrees.RunAsync(
            path,
            async () =>
            {
                WorktreeCounts counts = await inspector.CountAsync(path, cancellationToken);

                return new WorktreeRow(
                    repository,
                    path,
                    entry.Head,
                    health,
                    counts.DirtyFiles,
                    counts.UnpushedCommits,
                    counts.LastCommitAt,
                    new WorktreeSession.None(),
                    LeftoverFileCount: 0,
                    OwnerExists: true,
                    FolderExists: true);
            },
            IsGitFailure,
            _ => new WorktreeRow(repository, path, entry.Head, health, 0, 0, new LastCommit.Unknown(), new WorktreeSession.None(), 0, OwnerExists: true, FolderExists: true));
    }

    private static bool IsGitFailure(Exception exception) => exception is GitException;

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
            GitPointer pointer = folders.FileExists(gitFile) ? TargetOf(folder) : new GitPointer.NoGitFile();
            WorktreeObservation observation = new WorktreeObservation.Unregistered(pointer, UnderClaudeWorktrees: true, EmptyLeftover: false);
            yield return Leftover(repository, folder, observation, ownerExists: true);
        }
    }

    /// <summary>
    /// A direct child of a root or the worktree root with a .git file that no repository under the roots lists. Its
    /// .git file names a folder that exists: a live checkout of a repository outside the roots (a linked worktree, one
    /// of a bare repository, or a checkout made with a separate git directory), so elsewhere, never a leftover. It names
    /// a folder that is gone, or none: broken, its registration gone.
    /// </summary>
    private WorktreeRow BrokenWorktree(FolderPath folder)
    {
        if (TryGitDirOf(folder, out FolderPath? gitDir) && folders.DirectoryExists(gitDir.Value))
        {
            FolderPath repository = RepositoryOf(gitDir);
            WorktreeObservation elsewhere = new WorktreeObservation.Unregistered(new GitPointer.RegisteredElsewhere(repository), UnderClaudeWorktrees: false, EmptyLeftover: false);

            return Leftover(repository, folder, elsewhere, ownerExists: true);
        }

        FolderPath owner = gitDir is not null && IsRegistration(gitDir) ? RepositoryOf(gitDir) : folder.Parent ?? folder;
        bool ownerExists = folders.DirectoryExists(IOPath.Combine(owner.Value, ".git"));
        WorktreeObservation observation = new WorktreeObservation.Unregistered(new GitPointer.TargetGone(), UnderClaudeWorktrees: false, EmptyLeftover: false);

        return Leftover(owner, folder, observation, ownerExists);
    }

    /// <summary>
    /// The folder the .git file's gitdir line names, a relative one read from the .git file's own folder as git does;
    /// false when the file names none or the text is no path.
    /// </summary>
    private bool TryGitDirOf(FolderPath folder, [MaybeNullWhen(false)] out FolderPath gitDir)
    {
        string? text = folders.Lines(IOPath.Combine(folder.Value, ".git"))
            .Where(candidate => candidate.StartsWith(GitDirPrefix, StringComparison.Ordinal))
            .Select(line => line[GitDirPrefix.Length..].Trim().Replace(IOPath.AltDirectorySeparatorChar, IOPath.DirectorySeparatorChar))
            .FirstOrDefault();

        return FolderPath.TryParse(text is null || IOPath.IsPathFullyQualified(text) ? text : IOPath.Combine(folder.Value, text), out gitDir);
    }

    /// <summary>A registration sits at <c>&lt;git directory&gt;\worktrees\&lt;name&gt;</c>.</summary>
    private static bool IsRegistration(FolderPath gitDir) => gitDir.Parent is FolderPath worktrees && string.Equals(worktrees.Name, "worktrees", StringComparison.OrdinalIgnoreCase) && worktrees.Parent is not null;

    /// <summary>
    /// The repository a gitdir belongs to, read from its shape: a registration's git directory is two levels up, and is
    /// the repository itself when it is a bare one or the parent of a folder named .git; a gitdir that is no
    /// registration is a git directory of its own (a checkout made with <c>--separate-git-dir</c>).
    /// </summary>
    private static FolderPath RepositoryOf(FolderPath gitDir)
    {
        FolderPath gitDirectory = IsRegistration(gitDir) && gitDir.Parent?.Parent is FolderPath twoUp ? twoUp : gitDir;

        return string.Equals(gitDirectory.Name, ".git", StringComparison.OrdinalIgnoreCase) && gitDirectory.Parent is FolderPath repository ? repository : gitDirectory;
    }

    private WorktreeRow Leftover(FolderPath repository, FolderPath folder, WorktreeObservation observation, bool ownerExists) => new(repository, folder, new Head.Unknown(), WorktreeHealth.From(observation), 0, 0, new LastCommit.Unknown(), new WorktreeSession.None(), CountLeftoverFiles(folder), ownerExists, FolderExists: folders.DirectoryExists(folder.Value));

    /// <summary>Whether the registration a folder's .git file points at still exists; never names an owner.</summary>
    private GitPointer TargetOf(FolderPath folder) => TryGitDirOf(folder, out FolderPath? gitDir) && folders.DirectoryExists(gitDir.Value) ? new GitPointer.TargetExists() : new GitPointer.TargetGone();

    private bool IsEmptyLeftover(FolderPath folder)
    {
        bool leftoverName = folder.Name.EndsWith(".worktrees", StringComparison.OrdinalIgnoreCase);

        return leftoverName && folders.IsEmpty(folder.Value);
    }

    private int CountLeftoverFiles(FolderPath folder) => LeftoverFiles.Count(folders, folder);

    /// <summary>The first session with a process working in the worktree or a folder inside it.</summary>
    private static WorktreeSession LiveSessionIn(FolderPath worktree, IReadOnlyList<Session> sessions)
    {
        return sessions
            .Where(session => session is Session.Interactive or Session.Background { Process: JobProcess.Running })
            .Where(session => session.WorkingDirectory.IsUnder(worktree))
            .Select(WorktreeSession (session) => new WorktreeSession.Running(session.SessionId, session.Name))
            .DefaultIfEmpty(new WorktreeSession.None())
            .First();
    }
}