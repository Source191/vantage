using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// The smoke run's steps, built from Wording so a renamed title or a rebound key needs no change here: open and cancel
/// the Keys help, the new-session wizard and the problem report on the sessions view, the repository roots dialog and its folder picker on the Config tab, then on the worktrees view its help,
/// the force-remove confirmation, the fixed-folder wizard and the solution chooser for the named worktree, and quit. Dialogs are recognised by the tab title each one sets,
/// views by a column only that view has; a key is looked up by its hint, and a lost hint fails with its name.
/// </summary>
public static class SmokeScript
{
    private static readonly TimeSpan Slow = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan Usual = TimeSpan.FromSeconds(20);

    /// <summary>F3, which opens the folder picker from the repository roots dialog (its button reads <see cref="Wording.BrowseButton"/>).</summary>
    private const string Browse = "\u001bOR";

    /// <param name="worktreeFolder">The folder name of an idle linked worktree under a repository root, holding two solutions: force remove is refused with no dialog while a session uses it, and s asks only when there is more than one.</param>
    public static IReadOnlyList<SmokeStep> Build(string worktreeFolder)
    {
        IReadOnlyList<KeyHelp> sessions = Wording.SessionsKeys;
        IReadOnlyList<KeyHelp> worktrees = Wording.WorktreesKeys;
        IReadOnlyList<KeyHelp> config = Wording.ConfigKeys;
        CaptureMarker sessionsView = new CaptureMarker.ScreenText(ColumnOnlyIn(Wording.SessionsColumns, Wording.WorktreesColumns));
        CaptureMarker worktreesView = new CaptureMarker.ScreenText(ColumnOnlyIn(Wording.WorktreesColumns, Wording.SessionsColumns));
        CaptureMarker configView = new CaptureMarker.ScreenText(ColumnOnlyIn(Wording.ConfigColumns, [.. Wording.SessionsColumns, .. Wording.WorktreesColumns]));
        CaptureMarker closed = new CaptureMarker.Title(Wording.TabTitle);
        string escape = Sequence("esc");

        return
        [
            new SmokeStep(sessionsView, KeyFor(sessions, "Help"), Slow),
            new SmokeStep(Opened(DialogName.Keys), escape, Usual),
            new SmokeStep(closed, KeyFor(sessions, "New session"), Usual),
            new SmokeStep(Opened(DialogName.NewSession), escape, Usual),
            new SmokeStep(closed, KeyFor(sessions, "Report"), Usual),
            new SmokeStep(Opened(DialogName.ProblemReport), escape, Usual),
            new SmokeStep(closed, TabKey(sessions, ConsoleTab.Config), Usual),
            // The Config tab opens on the first repository root, and Enter edits the roots in a setting's dialog, where F3 opens the folder picker.
            new SmokeStep(configView, KeyFor(config, "Edit"), Usual),
            new SmokeStep(Opened(DialogName.Settings), Browse, Usual),
            new SmokeStep(Opened(DialogName.FolderPicker), escape, Usual),
            new SmokeStep(Opened(DialogName.Settings), escape, Usual),
            new SmokeStep(closed, TabKey(config, ConsoleTab.Worktrees), Usual),
            new SmokeStep(worktreesView, string.Empty, Usual),
            // The scan has finished once the table's title names a selection; keys typed into the filter before then are lost.
            // The WORKTREE cell reads "repository › slug", so the folder name itself is not on screen until the title or the filter shows it.
            new SmokeStep(new CaptureMarker.ScreenText(SelectionMarker(worktreeFolder)), KeyFor(worktrees, "Help"), Slow),
            new SmokeStep(Opened(DialogName.Keys), escape, Usual),
            new SmokeStep(closed, KeyFor(worktrees, "Filter") + worktreeFolder, Usual),
            new SmokeStep(CaptureMarker.Nothing, Sequence("enter"), Usual),
            new SmokeStep(CaptureMarker.Nothing, KeyFor(worktrees, "Force remove"), Usual),
            new SmokeStep(Opened(DialogName.ForceRemove), escape, Usual),
            new SmokeStep(closed, KeyFor(worktrees, "New session"), Usual),
            // The fixed-folder wizard shares the new-session tab title, so its own dialog title tells it apart.
            new SmokeStep(new CaptureMarker.ScreenText($"New session in {worktreeFolder}"), escape, Usual),
            // The worktree holds two solutions, neither named after its repository, so s asks which one.
            new SmokeStep(closed, KeyFor(worktrees, "Open solution"), Usual),
            new SmokeStep(Opened(DialogName.OpenSolution), escape, Usual),
            // Esc would only clear the filter typed above, so the tab key goes back.
            new SmokeStep(closed, TabKey(worktrees, ConsoleTab.Sessions), Usual),
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

    /// <summary>The key that chooses a tab, which has no bar hint: the one the help says shows that tab.</summary>
    public static string TabKey(IReadOnlyList<KeyHelp> keys, ConsoleTab tab)
    {
        string line = $"the {Wording.TabTitleFor(tab)} tab";

        return Sequence(keys.First(key => key.Line == line).Key);
    }

    /// <summary>What a terminal sends for a key as the help names it.</summary>
    private static string Sequence(string key) => key switch
    {
        "esc" => "\u001b",
        "enter" => "\r",
        _ => key,
    };

    private static CaptureMarker.Title Opened(DialogName dialog) => new(Wording.TabTitleDuring(dialog));

    /// <summary>The words a table title puts before the selected row's name (", selected: "), taken from Wording so a reworded title moves the marker with it.</summary>
    private static string SelectionMarker(string selected)
    {
        string title = Wording.Describe(new TableTitle("worktree", 1, 1, string.Empty, selected));

        return title[title.IndexOf(',', StringComparison.Ordinal)..^selected.Length];
    }

    private static string ColumnOnlyIn(IReadOnlyList<ColumnHelp> view, IReadOnlyList<ColumnHelp> other)
        => view.First(column => other.All(otherColumn => otherColumn.Name != column.Name)).Name;
}