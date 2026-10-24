using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Application.Effects;

/// <summary>What the footer says, beside the busy spinner, while an effect runs off the UI thread.</summary>
public static class WorkingText
{
    public static string For(ViewEffect effect)
    {
        return effect switch
        {
            OpenTabForSession open => $"Opening a tab for {open.Row.Session.Name}...",
            StopSession => "Stopping...",
            AdoptSession adopt => $"Moving {adopt.Row.Session.Name} under Vantage...",
            RenameSession rename => $"Renaming {rename.Row.Session.Name} to {rename.NewName.Text}...",
            CloseEmptyTerminals => "Closing...",
            ResumeSessions resume => $"Resuming {resume.Rows.Count}...",
            DeleteSession deletion => $"Deleting {deletion.Row.Session.Name}...",
            DeleteBranch branch => $"Deleting branch {branch.Branch.Branch}...",
            RemoveWorktree remove => $"Removing {remove.Row.FolderName}...",
            PruneRepository => "Pruning...",
            OpenShellTab shell => $"Opening a shell in {shell.Row.FolderName}...",
            StartSession start => $"Starting {start.Plan.Name.Text}...",
            _ => "Working...",
        };
    }
}