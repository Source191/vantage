using Source191.Vantage.Application;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// The smoke run's steps, built from Wording so a renamed title or a rebound key needs no change here: open and cancel
/// the Keys help, Settings and the new-session wizard on the sessions view, then on the worktrees view its help, the
/// force-remove confirmation and the fixed-folder wizard for the named worktree, and quit. Dialogs are recognised by the tab title each one sets,
/// views by a column only that view has; a key is looked up by its hint, and a lost hint fails with its name.
/// </summary>
public static class SmokeScript
{
    private static readonly TimeSpan Slow = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan Usual = TimeSpan.FromSeconds(20);

    /// <param name="worktreeFolder">The folder name of an idle linked worktree under a repository root: force remove is refused with no dialog while a session uses it.</param>
    public static IReadOnlyList<SmokeStep> Build(string worktreeFolder)
    {
        IReadOnlyList<KeyHelp> sessions = Wording.SessionsKeys;
        IReadOnlyList<KeyHelp> worktrees = Wording.WorktreesKeys;
        CaptureMarker sessionsView = new CaptureMarker.ScreenText(ColumnOnlyIn(Wording.SessionsColumns, Wording.WorktreesColumns));
        CaptureMarker worktreesView = new CaptureMarker.ScreenText(ColumnOnlyIn(Wording.WorktreesColumns, Wording.SessionsColumns));
        CaptureMarker closed = new CaptureMarker.Title(Wording.TabTitle);
        string escape = Sequence("esc");

        return
        [
            new SmokeStep(sessionsView, KeyFor(sessions, "Help"), Slow),
            new SmokeStep(Opened(DialogName.Keys), escape, Usual),
            new SmokeStep(closed, KeyFor(sessions, "Settings"), Usual),
            new SmokeStep(Opened(DialogName.Settings), escape, Usual),
            new SmokeStep(closed, KeyFor(sessions, "New session"), Usual),
            new SmokeStep(Opened(DialogName.NewSession), escape, Usual),
            new SmokeStep(closed, KeyFor(sessions, "Worktrees"), Usual),
            new SmokeStep(worktreesView, string.Empty, Usual),
            // The scan has finished once the worktree is in the table; keys typed into the filter before then are lost.
            new SmokeStep(new CaptureMarker.ScreenText(worktreeFolder), KeyFor(worktrees, "Help"), Slow),
            new SmokeStep(Opened(DialogName.Keys), escape, Usual),
            new SmokeStep(closed, KeyFor(worktrees, "Filter") + worktreeFolder, Usual),
            new SmokeStep(CaptureMarker.Nothing, Sequence("enter"), Usual),
            new SmokeStep(CaptureMarker.Nothing, KeyFor(worktrees, "Force remove"), Usual),
            new SmokeStep(Opened(DialogName.ForceRemove), escape, Usual),
            new SmokeStep(closed, KeyFor(worktrees, "Start session"), Usual),
            // The fixed-folder wizard shares the new-session tab title, so its own dialog title tells it apart.
            new SmokeStep(new CaptureMarker.ScreenText($"New session in {worktreeFolder}"), escape, Usual),
            new SmokeStep(closed, KeyFor(worktrees, "Sessions"), Usual),
            new SmokeStep(sessionsView, KeyFor(sessions, "Quit"), Usual),
        ];
    }

    /// <summary>What a terminal sends for the first key with this hint; the help's names esc and enter become their sequences.</summary>
    public static string KeyFor(IReadOnlyList<KeyHelp> keys, string hint)
    {
        KeyHelp help = keys.FirstOrDefault(key => key.Hint == hint)
            ?? throw new InvalidOperationException($"No key has the hint \"{hint}\"; the smoke script looks keys up by their hints in Wording.");

        return Sequence(help.Key);
    }

    /// <summary>What a terminal sends for a key as the help names it.</summary>
    private static string Sequence(string key) => key switch
    {
        "esc" => "\u001b",
        "enter" => "\r",
        _ => key,
    };

    private static CaptureMarker.Title Opened(DialogName dialog) => new(Wording.TabTitleDuring(dialog));

    private static string ColumnOnlyIn(IReadOnlyList<ColumnHelp> view, IReadOnlyList<ColumnHelp> other)
        => view.First(column => other.All(otherColumn => otherColumn.Name != column.Name)).Name;
}