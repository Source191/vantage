using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;

namespace Source191.Vantage.Application;

/// <summary>The sentences for what handlers and <c>vantage here</c> report; <c>WordingOfOutcomesShould</c> holds each one.</summary>
public static partial class Wording
{
    public static string Describe(Outcome outcome)
    {
        return outcome switch
        {
            StopSessionOutcome stopped => $"Stopped {stopped.Name}.",
            AdoptSessionOutcome adopt => Describe(adopt),
            RenameSessionOutcome rename => Describe(rename),
            CloseEmptyTerminalsOutcome close => Describe(close),
            ResumeSessionsOutcome resume => Describe(resume),
            DeleteSessionOutcome deletion => Describe(deletion),
            DeleteBranchOutcome => "Branch deleted.",
            RemoveWorktreeOutcome removal => Describe(removal),
            PruneRepositoryOutcome prune => $"Pruned {Registrations(prune.Pruned)} from {prune.RepositoryName}.",
            OpenShellOutcome shell => $"Opened a shell in {shell.FolderName}.",
            OpenTabOutcome tab => Describe(tab),
            StartSessionOutcome start => Describe(start),
            SaveSettingsOutcome => "Settings saved.",
            _ => outcome.ToString(),
        };
    }

    /// <summary>What <c>vantage here</c> prints after the start: where it started, or why it could not.</summary>
    public static string Here(LaunchOutcome outcome, string name, string directory)
    {
        return outcome.Kind switch
        {
            LaunchOutcomeKind.Started => $"Started {name} ({outcome.JobId}) in {directory}.",
            LaunchOutcomeKind.WorkspaceNotTrusted => $"Could not start: {directory} is not trusted yet. Run claude here once and accept the prompt.",
            LaunchOutcomeKind.Failed => $"Could not start: {outcome.Message}",
        };
    }

    private static string Describe(AdoptSessionOutcome outcome)
    {
        return outcome switch
        {
            AdoptSessionOutcome.NoMessages empty =>
                $"{empty.Name} has no messages yet, so there is nothing to move. Send it something in its tab first, or close that tab and start a session from Vantage.",
            AdoptSessionOutcome.Moved moved => $"Moved {moved.Name} under Vantage; its old tab is now a plain shell.",
            AdoptSessionOutcome.Failed failed => $"Could not move {failed.Name} under Vantage: {failed.Message}",
            _ => outcome.ToString(),
        };
    }

    private static string Describe(RenameSessionOutcome outcome)
    {
        return outcome switch
        {
            RenameSessionOutcome.NoMessages empty => $"{empty.Name} has no messages yet, so it cannot be renamed; send it something first.",
            RenameSessionOutcome.Renamed renamed => $"Renamed {renamed.OldName} to {renamed.NewName}; it restarts idle and keeps its conversation.",
            RenameSessionOutcome.Restored restored => $"Could not rename {restored.OldName}: {restored.Failure}; it is back under its old name.",
            RenameSessionOutcome.Unsupervised lost =>
                $"Could not rename {lost.OldName}: {lost.Failure}; resume it with claude --bg --resume {lost.SessionId} ({lost.RestoreFailure}).",
            _ => outcome.ToString(),
        };
    }

    private static string Describe(CloseEmptyTerminalsOutcome outcome)
    {
        IEnumerable<string> skipped = outcome.Skipped.Select(session => $"{session.Name} has messages (press b to move it under Vantage)");
        if (outcome.Closed.Count == 0 && outcome.Ended.Count == 0)
        {
            return outcome.Skipped.Count == 0 ? "Nothing to close." : $"Nothing closed: {string.Join("; ", skipped)}.";
        }

        List<string> parts = [];
        if (outcome.Closed.Count > 0)
        {
            parts.Add($"Closed {Count(outcome.Closed.Count, "tab")}: {string.Join(", ", outcome.Closed)}.");
        }

        if (outcome.Ended.Count > 0)
        {
            parts.Add($"Ended {Count(outcome.Ended.Count, "session")}: {string.Join(", ", outcome.Ended)} (not in a Windows Terminal tab, so nothing to close).");
        }

        if (outcome.Skipped.Count > 0)
        {
            parts.Add($"Skipped: {string.Join("; ", skipped)}.");
        }

        return string.Join(' ', parts);
    }

    private static string Describe(ResumeSessionsOutcome outcome)
    {
        IEnumerable<string> notes = outcome.Notes.Select(note => note.Failure is null
            ? $"{note.Name}: never sent a message, so there is nothing to resume; d deletes it"
            : $"{note.Name}: {note.Failure}");
        string suffix = outcome.Notes.Count == 0 ? string.Empty : $" Notes: {string.Join("; ", notes)}";

        return $"Resumed {outcome.Resumed} of {outcome.Chosen}.{suffix}";
    }

    private static string Describe(DeleteSessionOutcome outcome)
    {
        return outcome switch
        {
            DeleteSessionOutcome.Deleted deleted => $"Deleted {deleted.Name}.",
            DeleteSessionOutcome.KeptWorktree kept => $"Deleted {kept.Name}; kept its worktree because {Describe(kept.Reason)}.",
            DeleteSessionOutcome.RemovedWorktreeAndBranch removed => $"Deleted {removed.Name} and removed the worktree and branch {removed.Branch}.",
            DeleteSessionOutcome.BranchNotMerged unmerged => $"Deleted {unmerged.Name} and removed the worktree; branch {unmerged.Branch.Branch} is not merged.",
            _ => outcome.ToString(),
        };
    }

    private static string Describe(RemoveWorktreeOutcome outcome)
    {
        return outcome switch
        {
            RemoveWorktreeOutcome.Refused refused => $"Not removed: {Describe(refused.Reason)}.",
            RemoveWorktreeOutcome.Removed removed => $"Removed {removed.FolderName}.",
            RemoveWorktreeOutcome.RemovedAndDeletedBranch removed => $"Removed {removed.FolderName} and deleted its merged branch {removed.Branch}.",
            RemoveWorktreeOutcome.BranchKept { CreatedByVantage: true } kept => $"Removed {kept.FolderName}; branch {kept.Branch.Branch} is not merged.",
            RemoveWorktreeOutcome.BranchKept kept => $"Removed {kept.FolderName}; branch {kept.Branch.Branch} was not created by Vantage, so it is kept.",
            RemoveWorktreeOutcome.DeletedFolder deleted => $"Deleted the folder {deleted.FolderName}.",
            RemoveWorktreeOutcome.DeletedFolderAndPruned pruned =>
                $"Deleted the folder {pruned.FolderName} and pruned {Registrations(pruned.Pruned)} from {pruned.RepositoryName}.",
            _ => outcome.ToString(),
        };
    }

    private static string Describe(OpenTabOutcome outcome)
    {
        return outcome switch
        {
            OpenTabOutcome.Opened { ColourReused: true } opened =>
                $"Opened {opened.Name} in a {opened.Colour.ToLowerInvariant()} tab (every palette colour is in use, so one was reused).",
            OpenTabOutcome.Opened opened => $"Opened {opened.Name} in a {opened.Colour.ToLowerInvariant()} tab.",
            OpenTabOutcome.SwitchedTo switched => $"Switched to the tab of {switched.Name}.",
            OpenTabOutcome.NotFound missing => $"{missing.Name} is open in another tab, but Vantage could not find it. Is it in another window?",
            _ => outcome.ToString(),
        };
    }

    private static string Describe(StartSessionOutcome outcome)
    {
        return outcome switch
        {
            StartSessionOutcome.Started started => Started(started.Name, started.JobId.Value),
            StartSessionOutcome.StartedAndOpened opened => $"{Started(opened.Name, opened.JobId.Value)} {Describe(opened.Tab)}",
            StartSessionOutcome.NotListedYet unlisted => $"{Started(unlisted.Name, unlisted.JobId.Value)} It is not listed yet: press o on it to open a tab.",
            StartSessionOutcome.NotTrusted untrusted =>
                $"{untrusted.Directory.Name} is not a trusted workspace yet. A Claude tab was opened there: accept the trust prompt, exit, then retry.",
            StartSessionOutcome.Failed failed => $"Could not start: {failed.Message}",
            _ => outcome.ToString(),
        };
    }

    private static string Started(string name, string jobId) => $"Started {name} ({jobId}).";

    private static string Registrations(int number) => number == 1 ? "1 stale registration" : $"{number} stale registrations";

    private static string Count(int number, string noun) => number == 1 ? $"1 {noun}" : $"{number} {noun}s";
}