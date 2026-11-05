using Source191.Torni.Domain;

namespace Source191.Torni.Application;

/// <summary>
/// The text of a FOLDER cell. The roots are the repository roots and the worktree root, read from the settings on every
/// call rather than copied, so a save shows on the next redraw without touching the rows.
/// </summary>
public sealed class FolderLabels(CurrentSettings settings, FolderPath home)
{
    public string For(FolderPath folder)
    {
        Settings current = settings.Value;

        return FolderLabel.For(folder, [.. current.RepositoryRoots, current.WorktreeRoot], home);
    }
}