using Source191.Vantage.Application;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WordingOfKeysAndColumnsShould
{
    private static readonly string[] TabKeys = ["1 ", "2 ", "3 ", "← ", "→ ", "< ", "> "];

    [Fact]
    public void ListEveryKeyOnceWithItsHintAndLine()
    {
        Wording.SessionsKeys.Select(key => $"{key.Key} {key.Hint}").ShouldBe(
        [
            "enter Attach", "o Open tab", "n New session", "r Rename", "x Stop", "d Delete",
            "R Resume", "C Close empty", "b Adopt", "/ Filter", "? Help", "q Quit",
            "c Copy path", "e Open folder", "v Solution", "p Open PR", "m Memory", "U Update", "! Report", .. TabKeys,
        ]);
        Wording.WorktreesKeys.Select(key => $"{key.Key} {key.Hint}").ShouldBe(
        [
            "d Remove", "D Force remove", "P Prune", "s Start session", "o Open shell", "enter Go to session",
            "esc Sessions", "/ Filter", "? Help", "q Quit",
            "c Copy path", "e Open folder", "v Solution", "p Open PR", "U Update", "! Report", .. TabKeys,
        ]);
        Wording.ConfigKeys.Select(key => $"{key.Key} {key.Hint}").ShouldBe(
        [
            "enter Edit", "esc Sessions", "/ Filter", "? Help", "q Quit",
            "c Copy", "e Open folder", "U Update", "! Report", .. TabKeys,
        ]);
    }

    [Fact]
    public void GiveEveryKeyALineAndNoKeyTwice()
    {
        IReadOnlyList<KeyHelp>[] views = [Wording.SessionsKeys, Wording.WorktreesKeys, Wording.ConfigKeys];
        foreach (IReadOnlyList<KeyHelp> keys in views)
        {
            keys.ShouldAllBe(key => key.Line.Length > 0);
            keys.Select(key => key.Key).Distinct().Count().ShouldBe(keys.Count);
        }
    }

    [Fact]
    public void ShowTheKeyAsTheHelpAndTheKeyBarDo()
    {
        Wording.SessionsKeys[0].HelpKey.ShouldBe("Enter");
        Wording.SessionsKeys[6].HelpKey.ShouldBe("R");
        Wording.SessionsKeys.Single(key => key.Key == "<").HelpKey.ShouldBe("<");
        Wording.SessionsKeys.Single(key => key.Key == "→").HelpKey.ShouldBe("→");
    }

    [Fact]
    public void ExplainEveryColumnOfEachView()
    {
        Wording.SessionsColumns.Select(column => column.Name).ShouldBe(["NAME", "TAB", "STATE", "WHERE", "PR", "AGE", "MEM"]);
        Wording.WorktreesColumns.Select(column => column.Name).ShouldBe(["WORKTREE", "BRANCH", "PR", "DIRTY", "UNPUSHED", "LAST COMMIT", "SESSION", "HEALTH"]);
        Wording.ConfigColumns.Select(column => column.Name).ShouldBe(["SETTING", "VALUE"]);
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
            b      move a terminal session under Vantage   /      filter
            ?      help                                    q      quit
            c      copy the folder path                    e      open the folder in your editor
            v      open its solution                       p      open its pull request in the browser
            m      what its memory is made of              U      update Vantage in a new tab
            !      report a problem on GitHub              1      the Sessions tab
            2      the Worktrees tab                       3      the Config tab
            ←      the tab to the left                     →      the tab to the right
            <      scroll the columns left                 >      scroll the columns right

            Columns
            NAME   the session name given at launch, or the folder name for an unnamed one
            TAB    a block in the tab's colour while a Vantage tab shows the session,
                   ▯ grey for a plain claude in its own terminal
            STATE  ◐ needs input (amber), ● working (cyan), ○ idle (white),
                   ✓ completed (green), ✗ failed (red), ■ stopped (grey)
            WHERE  repository › slug for a worktree, with the branch in brackets when it differs;
                   repository (branch) for a main checkout; the folder alone outside a repository
            PR     its pull request: #123 on GitHub (open, draft, merged, closed),
                   !123 on Azure DevOps (active, draft, completed, abandoned); blank if none or not yet known
            AGE    time since the session started
            MEM    private working set of the session's process and everything it started; - when it has no process

            Claude Code: C:\Users\me\.local\bin\claude.exe
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void BuildTheWorktreesHelpFromKeysAndColumns()
    {
        Wording.WorktreesHelp.ShouldBe("""
            Keys
            d      remove (refused when work would be lost)      D      force remove after typing its name
            P      prune stale registrations in its repository   s      start a session in this worktree
            o      open a PowerShell tab there                   Enter  go to the session running in it
            Esc    back to sessions (clears a filter first)      /      filter
            ?      help                                          q      quit
            c      copy the folder path                          e      open the folder in your editor
            v      open its solution                             p      open its pull request in the browser
            U      update Vantage in a new tab                   !      report a problem on GitHub
            1      the Sessions tab                              2      the Worktrees tab
            3      the Config tab                                ←      the tab to the left
            →      the tab to the right                          <      scroll the columns left
            >      scroll the columns right

            Columns
            WORKTREE     the owning repository › the folder's slug, or its label when not named after it
            BRANCH       the branch checked out there
            PR           its pull request: #123 on GitHub (open, draft, merged, closed),
                         !123 on Azure DevOps (active, draft, completed, abandoned); blank if none or not yet known
            DIRTY        changed or untracked files
            UNPUSHED     commits on its branch that no remote has
            LAST COMMIT  time since the last commit
            SESSION      the session running in it, if any
            HEALTH       ok; locked (by another tool); orphan (a folder git has forgotten);
                         broken (registration or owner gone); empty (a leftover folder);
                         elsewhere (a worktree of a repository outside the roots)
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void BuildTheConfigHelpFromKeysAndColumns()
    {
        Wording.ConfigHelp.ShouldBe("""
            Keys
            Enter  edit the setting, or say how to set up         Esc    back to sessions (clears a filter first)
            /      filter                                         ?      help
            q      quit                                           c      copy the path
            e      open the folder, or the one holding the file   U      update Vantage in a new tab
            !      report a problem on GitHub                     1      the Sessions tab
            2      the Worktrees tab                              3      the Config tab
            ←      the tab to the left                            →      the tab to the right
            <      scroll the columns left                        >      scroll the columns right

            Columns
            SETTING  a setting Vantage keeps in config.json, or, in grey, setup it reads but does not change
            VALUE    its value; a setup row that needs attention says what to run
            """.ReplaceLineEndings("\n"));
    }
}