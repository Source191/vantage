using Source191.Vantage.Application;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WordingOfKeysAndColumnsShould
{
    [Fact]
    public void ListEveryKeyOnceWithItsHintAndLine()
    {
        Wording.SessionsKeys.Select(key => $"{key.Column} {key.Key} {key.Hint}").ShouldBe(
        [
            "0 enter Attach", "0 o Open in tab", "0 n New session", "0 r Rename", "0 x Stop", "0 d Delete",
            "1 R Resume stopped", "1 C Close empty tabs", "1 b ", "1 a Agent view", "1 w Worktrees", "1 / Filter", "1 ? Help", "1 q Quit", "0 , Settings",
        ]);
        Wording.WorktreesKeys.Select(key => $"{key.Column} {key.Key} {key.Hint}").ShouldBe(
        [
            "0 d Remove", "0 D Force remove", "0 p Prune", "0 s Start session", "0 o Open shell", "0 enter Go to session",
            "1 w Sessions", "1 esc Sessions", "1 / Filter", "1 ? Help", "1 q Quit", "1 , Settings",
        ]);
    }

    [Fact]
    public void GiveEveryKeyALineAndNoKeyTwice()
    {
        foreach (IReadOnlyList<KeyHelp> keys in new[] { Wording.SessionsKeys, Wording.WorktreesKeys })
        {
            keys.ShouldAllBe(key => key.Line.Length > 0);
            keys.Select(key => key.Key).Distinct().Count().ShouldBe(keys.Count);
        }
    }

    [Fact]
    public void ShowTheKeyAsTheHeaderAndTheHelpDo()
    {
        KeyHelp enter = Wording.SessionsKeys[0];

        enter.HeaderKey.ShouldBe("<enter>");
        enter.HelpKey.ShouldBe("Enter");
        Wording.SessionsKeys[6].HeaderKey.ShouldBe("<R>");
        Wording.SessionsKeys[6].HelpKey.ShouldBe("R");
    }

    [Fact]
    public void ExplainEveryColumnOfEachView()
    {
        Wording.SessionsColumns.Select(column => column.Name).ShouldBe(["NAME", "STATE", "REPOSITORY", "BRANCH", "WT", "AGE", "TAB"]);
        Wording.WorktreesColumns.Select(column => column.Name).ShouldBe(["REPOSITORY", "FOLDER", "BRANCH", "DIRTY", "UNPUSHED", "LAST COMMIT", "SESSION", "HEALTH"]);
        Wording.SessionsColumns.Concat(Wording.WorktreesColumns).ShouldAllBe(column => column.Meaning.Length > 0);
    }

    [Fact]
    public void BuildTheSessionsHelpFromKeysColumnsAndTheClaudePath()
    {
        Wording.SessionsHelp(@"C:\Users\me\.local\bin\claude.exe").ShouldBe("""
            Keys
            Enter  attach (Ctrl+Z or Esc comes back)       o      open in its own tab, or switch to it
            n      new session                             r      rename (restarts the session)
            x      stop                                    d      delete, and its worktree if nothing is lost
            R      resume after a reboot                   C      close tabs of unused terminal sessions
            b      move a terminal session under Vantage   a      native agent view
            w      worktrees view                          /      filter
            ?      help                                    q      quit
            ,      edit roots, worktree root and editor

            Columns
            NAME        the session name given at launch, or the folder name for an unnamed one
            STATE       needs input (amber), working (cyan), idle (white),
                        completed (green), failed (red), stopped (grey)
            REPOSITORY  the repository the session runs in
            BRANCH      the branch checked out there
            WT          yes when the session runs in a linked worktree
            AGE         time since the session started
            TAB         terminal for a plain claude started in its own tab, open while a Vantage tab shows it

            Claude Code: C:\Users\me\.local\bin\claude.exe
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void BuildTheWorktreesHelpFromKeysAndColumns()
    {
        Wording.WorktreesHelp.ShouldBe("""
            Keys
            d      remove (refused when work would be lost)      D      force remove after typing its name
            p      prune stale registrations in its repository   s      start a session in this worktree
            o      open a PowerShell tab there                   Enter  go to the session running in it
            w      back to sessions                              Esc    back to sessions (clears a filter first)
            /      filter                                        ?      help
            q      quit                                          ,      edit roots, worktree root and editor

            Columns
            REPOSITORY   the repository that owns the worktree
            FOLDER       the worktree's folder name
            BRANCH       the branch checked out there
            DIRTY        changed or untracked files
            UNPUSHED     commits on its branch that no remote has
            LAST COMMIT  time since the last commit
            SESSION      the session running in it, if any
            HEALTH       ok; locked (by another tool); orphan (a folder git has forgotten);
                         broken (registration or owner gone); empty (a leftover folder)
            """.ReplaceLineEndings("\n"));
    }
}