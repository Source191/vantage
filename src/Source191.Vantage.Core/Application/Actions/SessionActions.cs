using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

public sealed record BranchToConfirm(string Repository, string Branch);

public sealed record DeleteOutcome(string Message, BranchToConfirm? BranchToConfirm);

public sealed record ResumePlan(IReadOnlyList<SessionRow> Automatic, IReadOnlyList<SessionRow> ToChoose);

/// <summary>Stop, delete and resume, with the worktree safety rules applied when a deleted session lived in a Vantage worktree.</summary>
public sealed class SessionActions(IClaudeCli claude, IGit git, ICreatedWorktrees createdWorktrees, IProcessRegistry processes, ITranscripts transcripts)
{
    /// <summary>
    /// Moves a session started as plain claude in another tab under the supervisor: end its process, then resume the
    /// same conversation with --bg and its name. The old tab drops back to its shell prompt. A session that has never
    /// been sent a message has no transcript, so there is nothing to resume and its process is left alone.
    /// </summary>
    public async Task<string> AdoptAsync(SessionRow row, CancellationToken cancellationToken)
    {
        Session session = row.Session;
        if (session.ProcessId is null)
        {
            return $"{session.Name} has no process to move; it may have exited.";
        }

        if (!transcripts.HasMessages(session.SessionId))
        {
            return $"{session.Name} has no messages yet, so there is nothing to move. Send it something in its tab first, or close that tab and start a session from Vantage.";
        }

        processes.Terminate(session.ProcessId.Value);
        ResumeResult result = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, NameOf(session), cancellationToken);

        return result.Continued
            ? $"Moved {session.Name} under Vantage; its old tab is now a plain shell."
            : $"Could not move {session.Name} under Vantage: {result.Message}";
    }

    /// <summary>
    /// The CLI cannot rename a background session, and resuming one with flags starts a copy, so the job is removed
    /// (its transcript stays on disc) and resumed by session id under the new name: same session, new label. If the
    /// resume fails the job is resumed under the old name so the session is not left unsupervised.
    /// </summary>
    public async Task<string> RenameAsync(SessionRow row, SessionName newName, CancellationToken cancellationToken)
    {
        Session session = row.Session;
        if (!transcripts.HasMessages(session.SessionId))
        {
            return $"{session.Name} has no messages yet, so it cannot be renamed; send it something first.";
        }

        await claude.RemoveAsync(session.JobId!, cancellationToken);
        ResumeResult renamed = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, newName, cancellationToken);
        if (renamed.Continued)
        {
            return $"Renamed {session.Name} to {newName.Text}; it restarts idle and keeps its conversation.";
        }

        ResumeResult restored = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, NameOf(session), cancellationToken);

        return restored.Continued
            ? $"Could not rename {session.Name}: {renamed.Message}; it is back under its old name."
            : $"Could not rename {session.Name}: {renamed.Message}; resume it with claude --bg --resume {session.SessionId} ({restored.Message}).";
    }

    /// <summary>
    /// Closes the tabs of plain claude sessions that were never sent a message: nothing to keep, so claude is ended and
    /// then the tab's shell is ended cleanly, which makes Windows Terminal close the tab. A session with messages or
    /// without a process is left alone and named in the result.
    /// </summary>
    public Task<string> CloseEmptyTerminalsAsync(IReadOnlyList<SessionRow> rows, CancellationToken cancellationToken)
    {
        List<string> closed = [];
        List<string> ended = [];
        List<string> skipped = [];
        foreach (Session session in rows.Select(row => row.Session))
        {
            if (session.ProcessId is null)
            {
                skipped.Add($"{session.Name} has no process");
                continue;
            }

            if (transcripts.HasMessages(session.SessionId))
            {
                skipped.Add($"{session.Name} has messages (press b to move it under Vantage)");
                continue;
            }

            int? shell = TerminalTab.ShellOf(session.ProcessId.Value, processes);
            processes.Terminate(session.ProcessId.Value);
            if (shell is null)
            {
                ended.Add(session.Name);
                continue;
            }

            processes.EndCleanly(shell.Value);
            closed.Add(session.Name);
        }

        return Task.FromResult(Summary(closed, ended, skipped));
    }

    private static string Summary(List<string> closed, List<string> ended, List<string> skipped)
    {
        List<string> parts = [];
        if (closed.Count > 0)
        {
            parts.Add($"Closed {closed.Count} {(closed.Count == 1 ? "tab" : "tabs")}: {string.Join(", ", closed)}.");
        }

        if (ended.Count > 0)
        {
            parts.Add($"Ended {ended.Count} {(ended.Count == 1 ? "session" : "sessions")}: {string.Join(", ", ended)} (not in a Windows Terminal tab, so nothing to close).");
        }

        if (closed.Count == 0 && ended.Count == 0)
        {
            return skipped.Count == 0 ? "Nothing to close." : $"Nothing closed: {string.Join("; ", skipped)}.";
        }

        if (skipped.Count > 0)
        {
            parts.Add($"Skipped: {string.Join("; ", skipped)}.");
        }

        return string.Join(' ', parts);
    }

    public async Task<string> StopAsync(SessionRow row, CancellationToken cancellationToken)
    {
        await claude.StopAsync(row.Session.JobId!, cancellationToken);

        return $"Stopped {row.Session.Name}.";
    }

    public async Task<DeleteOutcome> DeleteAsync(SessionRow row, bool forced, CancellationToken cancellationToken)
    {
        await claude.RemoveAsync(row.Session.JobId!, cancellationToken);
        string deleted = $"Deleted {row.Session.Name}";

        CreatedWorktree? created = (await createdWorktrees.ReadCreatedWorktreesAsync())
            .FirstOrDefault(worktree => string.Equals(worktree.Path, row.Session.WorkingDirectory, StringComparison.OrdinalIgnoreCase));
        if (created is null)
        {
            return new DeleteOutcome($"{deleted}.", BranchToConfirm: null);
        }

        RemovalDecision decision = RemovalDecision.For(await FactsAsync(created, cancellationToken), forced);
        if (decision.Outcome != RemovalOutcome.RemoveWorktree)
        {
            return new DeleteOutcome($"{deleted}; kept its worktree because {decision.Reason}.", BranchToConfirm: null);
        }

        await git.RemoveWorktreeAsync(created.Repository, created.Path, decision.Forced, cancellationToken);
        await createdWorktrees.ForgetCreatedWorktreeAsync(created.Path);
        if (decision.Branch == BranchAction.Delete)
        {
            await git.DeleteBranchAsync(created.Repository, created.Branch, force: false, cancellationToken);

            return new DeleteOutcome($"{deleted} and removed the worktree and branch {created.Branch}.", BranchToConfirm: null);
        }

        return new DeleteOutcome($"{deleted} and removed the worktree; branch {created.Branch} is not merged.", new BranchToConfirm(created.Repository, created.Branch));
    }

    public Task DeleteBranchAsync(BranchToConfirm branch, CancellationToken cancellationToken)
    {
        return git.DeleteBranchAsync(branch.Repository, branch.Branch, force: true, cancellationToken);
    }

    public static ResumePlan PlanResume(IReadOnlyList<SessionRow> rows)
    {
        return new ResumePlan(
            [.. rows.Where(row => row.Session.State.Kind == DisplayStateKind.Failed)],
            [.. rows.Where(row => row.Session.State.Kind == DisplayStateKind.Stopped)]);
    }

    public async Task<string> ResumeAsync(IReadOnlyList<SessionRow> rows, CancellationToken cancellationToken)
    {
        int continued = 0;
        List<string> notes = [];
        foreach (Session session in rows.Select(row => row.Session))
        {
            if (!transcripts.HasMessages(session.SessionId))
            {
                notes.Add($"{session.Name}: never sent a message, so there is nothing to resume; d deletes it");
                continue;
            }

            ResumeResult result = await claude.ResumeBackgroundAsync(session.SessionId, session.WorkingDirectory, NameOf(session), cancellationToken);
            continued += result.Continued ? 1 : 0;
            if (!result.Continued)
            {
                notes.Add($"{session.Name}: {result.Message}");
            }
        }

        string suffix = notes.Count == 0 ? string.Empty : $" Notes: {string.Join("; ", notes)}";

        return $"Resumed {continued} of {rows.Count}.{suffix}";
    }

    /// <summary>A listing name that the CLI would refuse (quotes, semicolons) is dropped rather than failing the resume.</summary>
    private static SessionName? NameOf(Session session)
    {
        try
        {
            return SessionName.Parse(session.Name);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private async Task<WorktreeRemovalFacts> FactsAsync(CreatedWorktree created, CancellationToken cancellationToken)
    {
        IReadOnlyList<GitWorktreeEntry> entries = await git.ListWorktreesAsync(created.Repository, cancellationToken);
        GitWorktreeEntry? entry = entries.FirstOrDefault(candidate => string.Equals(candidate.Path, created.Path, StringComparison.OrdinalIgnoreCase));
        // Git's listing is the source of truth here: a prunable entry means the folder has gone.
        WorktreeHealthKind health = entry is null
            ? WorktreeHealthKind.Broken
            : WorktreeHealth.From(new WorktreeObservation(true, entry.PrunableReason is null, entry.LockReason, false, null, false)).Kind;
        string into = await git.DefaultRemoteBranchAsync(created.Repository, cancellationToken) ?? "HEAD";

        return new WorktreeRemovalFacts(
            health,
            created.Branch,
            DirtyFileCount: await git.CountChangedFilesAsync(created.Path, cancellationToken),
            UnpushedCommitCount: await git.CountUnpushedCommitsAsync(created.Path, cancellationToken),
            LiveSessionName: null,
            BranchCreatedByVantage: true,
            BranchMerged: await git.IsMergedAsync(created.Repository, created.Branch, into, cancellationToken),
            LeftoverFileCount: 0);
    }
}