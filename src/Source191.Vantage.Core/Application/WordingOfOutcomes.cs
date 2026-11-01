using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Application;

/// <summary>The sentences for what handlers and <c>vantage here</c> report; <c>WordingOfOutcomesShould</c> holds each one.</summary>
public static partial class Wording
{
    public static string Describe(Outcome outcome)
    {
        return outcome switch
        {
            StopSessionOutcome { TabClosed: true } stopped => $"Stopped {stopped.Name} and closed its tab.",
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
            FolderPathCopied copied => $"Copied {copied.Path} to the clipboard.",
            ClipboardBusy => "The clipboard is in use by another program; try again.",
            OpenSolutionOutcome.Opened solution => $"Opened {solution.FileName}.",
            OpenSolutionOutcome.NoApplication none => $"No application opens {none.Extension} files; install one, or choose one in Windows' Default apps.",
            OpenPullRequestOutcome opened => $"Opened {PullRequestNumber(opened.Host, opened.Number)} in the browser.",
            OpenInEditorOutcome editor => Describe(editor),
            OpenTabOutcome tab => Describe(tab),
            StartSessionOutcome start => Describe(start),
            SaveSettingsOutcome => "Settings saved.",
            ToggleUpdateCheckOutcome toggled => toggled.On ? "The update check is on: Vantage asks nuget.org at most once a day." : "The update check is off.",
            ProblemReportOutcome.Opened => "Opened a problem report in your browser; describe the problem there and submit it.",
            ProblemReportOutcome.NotOpened notOpened => $"Could not open the browser: {notOpened.Reason} Press ! and choose Copy text instead.",
            ProblemReportOutcome.Copied => "Copied the problem report to the clipboard.",
            UpdateStartedOutcome update => $"Updating to {update.Newest} in a new tab; Vantage starts again there when it is done.",
            _ => outcome.ToString(),
        };
    }

    /// <summary>How the status line marks what a handler reports; a case with no arm here is a notice.</summary>
    public static StatusTone ToneOf(Outcome outcome)
    {
        return outcome switch
        {
            StopSessionOutcome => StatusTone.Success,
            AdoptSessionOutcome adopt => ToneOf(adopt),
            RenameSessionOutcome rename => ToneOf(rename),
            CloseEmptyTerminalsOutcome close => ToneOf(close),
            ResumeSessionsOutcome resume => resume.Resumed == resume.Chosen && resume.Notes.Count == 0 ? StatusTone.Success : StatusTone.Notice,
            DeleteSessionOutcome deletion => ToneOf(deletion),
            DeleteBranchOutcome => StatusTone.Success,
            RemoveWorktreeOutcome removal => ToneOf(removal),
            PruneRepositoryOutcome => StatusTone.Success,
            OpenShellOutcome => StatusTone.Success,
            FolderPathCopied => StatusTone.Success,
            ClipboardBusy => StatusTone.Notice,
            OpenSolutionOutcome.Opened => StatusTone.Success,
            OpenSolutionOutcome.NoApplication => StatusTone.Failure,
            OpenPullRequestOutcome => StatusTone.Success,
            OpenInEditorOutcome editor => ToneOf(editor),
            OpenTabOutcome tab => ToneOf(tab),
            StartSessionOutcome start => ToneOf(start),
            SaveSettingsOutcome => StatusTone.Success,
            ToggleUpdateCheckOutcome => StatusTone.Success,
            ProblemReportOutcome.Opened => StatusTone.Success,
            ProblemReportOutcome.NotOpened => StatusTone.Failure,
            ProblemReportOutcome.Copied => StatusTone.Success,
            UpdateStartedOutcome => StatusTone.Success,
            _ => StatusTone.Notice,
        };
    }

    private static StatusTone ToneOf(AdoptSessionOutcome outcome)
    {
        return outcome switch
        {
            AdoptSessionOutcome.Moved => StatusTone.Success,
            AdoptSessionOutcome.Failed => StatusTone.Failure,
            _ => StatusTone.Notice,
        };
    }

    private static StatusTone ToneOf(RenameSessionOutcome outcome)
    {
        return outcome switch
        {
            RenameSessionOutcome.Renamed => StatusTone.Success,
            RenameSessionOutcome.Restored or RenameSessionOutcome.Unsupervised => StatusTone.Failure,
            _ => StatusTone.Notice,
        };
    }

    private static StatusTone ToneOf(DeleteSessionOutcome outcome)
    {
        return outcome switch
        {
            DeleteSessionOutcome.Deleted or DeleteSessionOutcome.RemovedWorktreeAndBranch or DeleteSessionOutcome.BranchNotMerged => StatusTone.Success,
            DeleteSessionOutcome.RemovedWorktree removed => ToneOf(removed.Worktree),
            _ => StatusTone.Notice,
        };
    }

    private static StatusTone ToneOf(RemoveWorktreeOutcome outcome)
    {
        return outcome switch
        {
            RemoveWorktreeOutcome.Refused or RemoveWorktreeOutcome.InUse => StatusTone.Notice,
            _ => StatusTone.Success,
        };
    }

    private static StatusTone ToneOf(OpenInEditorOutcome outcome)
    {
        return outcome switch
        {
            OpenInEditorOutcome.Opened => StatusTone.Success,
            OpenInEditorOutcome.EditorNotFound or OpenInEditorOutcome.EditorNotStarted => StatusTone.Failure,
            _ => StatusTone.Notice,
        };
    }

    private static StatusTone ToneOf(OpenTabOutcome outcome)
    {
        return outcome switch
        {
            OpenTabOutcome.Opened { ColourReused: false } or OpenTabOutcome.SwitchedTo => StatusTone.Success,
            _ => StatusTone.Notice,
        };
    }

    /// <summary>Done only when every empty tab was closed and no session was skipped or gone.</summary>
    private static StatusTone ToneOf(CloseEmptyTerminalsOutcome outcome)
    {
        bool closedSome = outcome.Closed.Count > 0 || outcome.Ended.Count > 0;
        bool leftSome = outcome.Skipped.Count > 0 || outcome.Gone.Count > 0;

        return closedSome && !leftSome ? StatusTone.Success : StatusTone.Notice;
    }

    private static StatusTone ToneOf(StartSessionOutcome outcome)
    {
        return outcome switch
        {
            StartSessionOutcome.StartedAndOpened opened => ToneOf(opened.Tab),
            StartSessionOutcome.NotTrusted or StartSessionOutcome.Failed => StatusTone.Failure,
            _ => StatusTone.Notice,
        };
    }

    /// <summary>What <c>vantage here</c> prints after the start: where it started, or why it could not.</summary>
    public static string Here(LaunchOutcome outcome, string name, string directory)
    {
        return outcome switch
        {
            LaunchOutcome.Started started => $"Started {name} ({started.JobId}) in {directory}.",
            LaunchOutcome.WorkspaceNotTrusted => $"Could not start: {directory} is not trusted yet. Run claude here once and accept the prompt.",
            LaunchOutcome.Failed failed => $"Could not start: {failed.Message}",
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>Where another editor is chosen, named after any editor problem the editor key reports.</summary>
    private const string WhereEditorsAreChosen = "in Open folder with on the Config tab (3)";

    private static string Describe(OpenInEditorOutcome outcome)
    {
        return outcome switch
        {
            OpenInEditorOutcome.Opened opened => $"Opened {opened.FolderName} in {EditorName(opened.Editor)}.",
            OpenInEditorOutcome.EditorNotFound missing => $"Could not find {EditorName(missing.Editor)}; choose another {WhereEditorsAreChosen}.",
            OpenInEditorOutcome.EditorNotStarted refused => $"{EditorName(refused.Editor)} would not start: {refused.Reason.Trim().TrimEnd('.')}. Choose another {WhereEditorsAreChosen}.",
            OpenInEditorOutcome.FolderMissing gone => $"{gone.FolderName} no longer exists, so nothing was opened.",
            _ => outcome.ToString(),
        };
    }

    private static string Describe(AdoptSessionOutcome outcome)
    {
        return outcome switch
        {
            AdoptSessionOutcome.NoMessages empty =>
                $"{empty.Name} has no messages yet, so there is nothing to move. Send it something in its tab first, or close that tab and start a session from Vantage.",
            AdoptSessionOutcome.Moved { TabClosed: true } moved => $"Moved {moved.Name} under Vantage and closed its old tab.",
            AdoptSessionOutcome.Moved moved => $"Moved {moved.Name} under Vantage.",
            AdoptSessionOutcome.Failed failed => $"Could not move {failed.Name} under Vantage: {failed.Message}",
            AdoptSessionOutcome.ProcessGone gone => $"Could not move {gone.Name}: it no longer runs under its listed process. Refresh and try again.",
            _ => outcome.ToString(),
        };
    }

    private static string Describe(RenameSessionOutcome outcome)
    {
        return outcome switch
        {
            RenameSessionOutcome.NoMessages empty => $"{empty.Name} has no messages yet, so it cannot be renamed; send it something first.",
            RenameSessionOutcome.Renamed { TabMoved: true } renamed => $"Renamed {renamed.OldName} to {renamed.NewName} and moved its tab; it restarts idle and keeps its conversation.",
            RenameSessionOutcome.Renamed renamed => $"Renamed {renamed.OldName} to {renamed.NewName}; it restarts idle and keeps its conversation.",
            RenameSessionOutcome.Restored { TabClosed: true } restored => $"Could not rename {restored.OldName}: {restored.Failure}; it is back under its old name and its tab is closed: Enter opens a new one.",
            RenameSessionOutcome.Restored restored => $"Could not rename {restored.OldName}: {restored.Failure}; it is back under its old name.",
            RenameSessionOutcome.Unsupervised lost =>
                $"Could not rename {lost.OldName}: {lost.Failure}; resume it with claude --bg --resume {lost.SessionId} ({lost.RestoreFailure}).",
            _ => outcome.ToString(),
        };
    }

    private static string Describe(CloseEmptyTerminalsOutcome outcome)
    {
        IEnumerable<string> skipped = outcome.Skipped.Select(session => $"{session.Name} has messages (press b to move it under Vantage)")
            .Concat(outcome.Gone.Select(name => $"{name} no longer runs under its listed process (refresh and try again)"));
        if (outcome.Closed.Count == 0 && outcome.Ended.Count == 0)
        {
            return skipped.Any() ? $"Nothing closed: {string.Join("; ", skipped)}." : "Nothing to close.";
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

        if (skipped.Any())
        {
            parts.Add($"Skipped: {string.Join("; ", skipped)}.");
        }

        return string.Join(' ', parts);
    }

    private static string Describe(ResumeSessionsOutcome outcome)
    {
        IEnumerable<string> notes = outcome.Notes.Select(note => note switch
        {
            ResumeNote.NeverSentAMessage => $"{note.Name}: never sent a message, so there is nothing to resume; d deletes it",
            ResumeNote.Failed failed => $"{note.Name}: {failed.Failure}; resume it with claude --bg --resume {failed.SessionId.Value}",
            _ => throw new UnreachableException(),
        });
        string suffix = outcome.Notes.Count == 0 ? string.Empty : $" Notes: {string.Join("; ", notes)}";

        string tabs = outcome.Resumed == 0 ? string.Empty : ", each in its own tab";

        return $"Resumed {outcome.Resumed} of {outcome.Chosen}{tabs}.{suffix}";
    }

    private static string Describe(DeleteSessionOutcome outcome)
    {
        return outcome switch
        {
            DeleteSessionOutcome.Deleted deleted => $"{Gone(deleted, deleted.Name)}.",
            DeleteSessionOutcome.KeptWorktree { Reason: RemovalReason.LeftoverFilesWouldBeLost } kept => $"{Gone(kept, kept.Name)}; kept its worktree because {Describe(kept.Reason)}; force remove (D) on the Worktrees tab deletes it.",
            DeleteSessionOutcome.KeptWorktree kept => $"{Gone(kept, kept.Name)}; kept its worktree because {Describe(kept.Reason)}.",
            DeleteSessionOutcome.RemovedWorktreeAndBranch removed => $"{Gone(removed, removed.Name)}{(removed.TabClosed ? "," : string.Empty)} and removed the worktree and branch {removed.Branch}.",
            DeleteSessionOutcome.BranchNotMerged unmerged => $"{Gone(unmerged, unmerged.Name)}{(unmerged.TabClosed ? "," : string.Empty)} and removed the worktree; branch {unmerged.Branch.Branch} is not merged.",
            DeleteSessionOutcome.RemovedWorktree removed => $"{Gone(removed, removed.Name)}; {Lowered(Describe(removed.Worktree))}",
            DeleteSessionOutcome.WorktreeInUse inUse => $"{Gone(inUse, inUse.Name)}; its worktree folder {inUse.FolderName} is still in use by a tab or program: close it and remove the folder from the worktrees view.",
            _ => outcome.ToString(),
        };
    }

    private static string Describe(RemoveWorktreeOutcome outcome)
    {
        return outcome switch
        {
            RemoveWorktreeOutcome.Refused refused => $"Not removed: {Describe(refused.Reason)}.",
            RemoveWorktreeOutcome.InUse inUse => $"Not removed: the folder {inUse.FolderName} is still in use by a tab or program; close it and remove again.",
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
            StartSessionOutcome.StartedAndOpened opened => $"{Started(opened.Name, opened.JobId.Value)} {Describe(opened.Tab)}",
            StartSessionOutcome.NotListedYet unlisted => $"{Started(unlisted.Name, unlisted.JobId.Value)} It is not listed yet: press Enter on it to open its tab.",
            StartSessionOutcome.NotTrusted untrusted =>
                $"{untrusted.Directory.Name} is not a trusted workspace yet. A Claude tab was opened there: accept the trust prompt, exit, then retry.",
            StartSessionOutcome.Failed failed => $"Could not start: {failed.Message}",
            _ => outcome.ToString(),
        };
    }

    private static string Started(string name, string jobId) => $"Started {name} ({jobId}).";

    private static string Registrations(int number) => number == 1 ? "1 stale registration" : $"{number} stale registrations";

    private static string Count(int number, string noun) => number == 1 ? $"1 {noun}" : $"{number} {noun}s";

    private static string EditorName(EditorChoice editor)
    {
        return editor.Preset switch
        {
            EditorPreset.VsCode => "VS Code",
            EditorPreset.VisualStudio => "Visual Studio",
            EditorPreset.Rider => "Rider",
            EditorPreset.Explorer => "Explorer",
            EditorPreset.Custom => Path.GetFileNameWithoutExtension(editor.Executable),
        };
    }

    /// <summary>How a delete sentence opens: the session deleted, and its tab closed when there was one.</summary>
    private static string Gone(DeleteSessionOutcome outcome, string name) => outcome.TabClosed ? $"Deleted {name} and closed its tab" : $"Deleted {name}";

    /// <summary>A sentence carried on after a semicolon: its first letter lowered.</summary>
    private static string Lowered(string sentence) => sentence.Length == 0 ? sentence : char.ToLowerInvariant(sentence[0]) + sentence[1..];
}