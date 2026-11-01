using Source191.Vantage.Application;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WordingOfKeysAndColumnsShould
{
    private static readonly string[] TabKeys = ["1 ", "2 ", "3 ", "← ", "→ ", "< ", "> "];

    [Fact]
    public void ListEveryKeyOnceWithItsHintAndLine()
    {
        Wording.SessionsKeys.Select(key => $"{key.Key} {key.Hint}").ShouldBe(
        [
            "enter Open", "n New session", "r Rename", "x Stop", "d Delete",
            "R Resume", "C Close empty", "b Adopt", "/ Filter", "? Help", "q Quit",
            "c Copy path", "f Open folder", "s Open solution", "p Open PR", "m Memory", "U Update", "! Report", .. TabKeys,
        ]);
        Wording.WorktreesKeys.Select(key => $"{key.Key} {key.Hint}").ShouldBe(
        [
            "d Remove", "D Force remove", "P Prune", "n New session", "o Open shell", "enter Open session",
            "esc Sessions", "/ Filter", "? Help", "q Quit",
            "c Copy path", "f Open folder", "s Open solution", "p Open PR", "U Update", "! Report", .. TabKeys,
        ]);
        Wording.ConfigKeys.Select(key => $"{key.Key} {key.Hint}").ShouldBe(
        [
            "enter Edit", "esc Sessions", "/ Filter", "? Help", "q Quit",
            "c Copy", "f Open folder", "U Update", "! Report", .. TabKeys,
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
        Wording.SessionsKeys[5].HelpKey.ShouldBe("R");
        Wording.SessionsKeys.Single(key => key.Key == "<").HelpKey.ShouldBe("<");
        Wording.SessionsKeys.Single(key => key.Key == "→").HelpKey.ShouldBe("→");
    }

    [Fact]
    public void SayEnterOpensTheSessionInItsOwnTab()
    {
        KeyHelp enter = Wording.SessionsKeys.Single(key => key.Key == "enter");

        enter.Line.ShouldBe("open in its own tab, or switch to it");
        enter.Tip.ShouldBe("Open this session in its own tab, or switch to that tab if it is open.");
        Wording.SessionsKeys.ShouldNotContain(key => key.Key == "o");
    }

    [Fact]
    public void ExplainEveryColumnOfEachView()
    {
        Wording.SessionsColumns.Select(column => column.Name).ShouldBe(["NAME", "TAB", "STATE", "REPOSITORY", "PR", "AGE"]);
        Wording.WorktreesColumns.Select(column => column.Name).ShouldBe(["WORKTREE", "HEALTH", "LAST COMMIT", "SESSION"]);
        Wording.ConfigColumns.Select(column => column.Name).ShouldBe(["SETTING", "VALUE"]);
        Wording.SessionsColumns.Concat(Wording.WorktreesColumns).ShouldAllBe(column => column.Meaning.Length > 0);
    }

    [Fact]
    public void ListEveryHealthGlyphInTheHelp()
    {
        string health = Wording.WorktreesColumns.Single(column => column.Name == "HEALTH").Meaning;
        string worktree = Wording.WorktreesColumns.Single(column => column.Name == "WORKTREE").Meaning;

        Enum.GetValues<WorktreeHealthKind>().ShouldAllBe(kind => health.Contains(Wording.HealthCell(kind), StringComparison.Ordinal));
        worktree.ShouldNotContain("white", Case.Insensitive);
        worktree.ShouldNotContain("amber", Case.Insensitive);
        worktree.ShouldNotContain("grey", Case.Insensitive);
    }

    [Fact]
    public void SayTheTabBlockMatchesTheTabAsItLooksFromVantage()
    {
        Wording.SessionsColumns.Single(column => column.Name == "TAB").Meaning
            .ShouldStartWith("a block in the tab's colour, faded as the tab looks from Vantage's own tab,\nwhile a Vantage tab shows the session;");
    }

    [Fact]
    public void ExplainEveryLineOfEachPane()
    {
        Wording.SessionsPane.Select(line => line.Name).ShouldBe(["Waiting", "Started", "Prompted", "Folder", "Session", "Worktree", "Branch", "Tab", "Changes", "Memory"]);
        Wording.WorktreesPane.Select(line => line.Name).ShouldBe(["Health", "Branch", "PR", "Folder", "Created", "Changes", "Unpushed"]);
        Wording.SessionsPane.Concat(Wording.WorktreesPane).ShouldAllBe(line => line.Meaning.Length > 0);
    }

    [Fact]
    public void BuildTheSessionsHelpFromKeysColumnsAndTheClaudePath()
    {
        Wording.SessionsHelp(@"C:\Users\me\.local\bin\claude.exe").ShouldBe("""
            Keys
            Enter  open in its own tab, or switch to it          n      new session
            r      rename (restarts the session)                 x      stop
            d      delete, and its worktree if nothing is lost   R      resume every stopped or failed session shown, each in its own tab
            C      close tabs of unused terminal sessions        b      move a terminal session under Vantage
            /      filter                                        ?      help
            q      quit                                          c      copy the folder path
            f      open the folder in your editor                s      open its solution
            p      open its pull request in the browser          m      what its memory is made of
            U      update Vantage in a new tab                   !      report a problem on GitHub
            1      the Sessions tab                              2      the Worktrees tab
            3      the Config tab                                ←      the tab to the left
            →      the tab to the right                          <      scroll the columns left
            >      scroll the columns right

            Columns
            NAME        the session name given at launch, or the folder name for an unnamed one
            TAB         a block in the tab's colour, faded as the tab looks from Vantage's own tab,
                        while a Vantage tab shows the session; ▯ grey for a plain claude in its own terminal
            STATE       ◐ needs input (amber), ● working (cyan), ○ idle (white),
                        ✓ completed (green), ✗ failed (red), ■ stopped (grey)
            REPOSITORY  the repository it runs in, or the folder's label outside a repository
            PR          its pull request: #123 on GitHub (open, draft, merged, closed),
                        !123 on Azure DevOps (active, draft, completed, abandoned); blank if none or not yet known
            AGE         time since the session started

            Pane
            Waiting   what it waits for: Claude Code's reason, a first prompt, or nothing
            Started   when the session started
            Prompted  when Vantage last saw it go from waiting or idle to working
            Folder    the folder it runs in
            Session   the session id
            Worktree  the worktree's slug, the path inside the repository, or the main checkout
            Branch    the branch checked out there
            Tab       its Vantage tab and the tab's colour, or its own terminal
            Changes   changed files, unpushed commits and the last commit there
            Memory    what the session and everything it started use; m for the breakdown

            Claude Code: C:\Users\me\.local\bin\claude.exe
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void BuildTheWorktreesHelpFromKeysAndColumns()
    {
        Wording.WorktreesHelp.ShouldBe("""
            Keys
            d      remove (refused when work would be lost)      D      force remove after typing its name
            P      prune stale registrations in its repository   n      start a session in this worktree
            o      open a PowerShell tab there                   Enter  open its session's tab, or switch to it
            Esc    back to sessions (clears a filter first)      /      filter
            ?      help                                          q      quit
            c      copy the folder path                          f      open the folder in your editor
            s      open its solution                             p      open its pull request in the browser
            U      update Vantage in a new tab                   !      report a problem on GitHub
            1      the Sessions tab                              2      the Worktrees tab
            3      the Config tab                                ←      the tab to the left
            →      the tab to the right                          <      scroll the columns left
            >      scroll the columns right

            Columns
            WORKTREE     the owning repository › the folder's slug, or its label when not named after it
            HEALTH       ✓ ok, ◆ locked (by another tool), △ orphan (a folder git has forgotten),
                         ✗ broken (registration or owner gone), □ empty (a leftover folder),
                         ↗ elsewhere (a worktree of a repository outside the roots); the pane gives the reason
            LAST COMMIT  time since the last commit
            SESSION      the session running in it, if any

            Pane
            Health    ok; locked (by another tool); orphan (a folder git has forgotten);
                      broken (registration or owner gone); empty (a leftover folder);
                      elsewhere (a worktree of a repository outside the roots); then the reason in full
            Branch    the branch checked out there, and whether it is merged into origin's default branch
            PR        its pull request, as the sessions table shows it, or none; p opens it
            Folder    the worktree's folder
            Created   when Vantage created it, or not by Vantage
            Changes   changed or untracked files, and commits on its branch that no remote has
            Unpushed  the newest five unpushed commits, or the changed files when none, and how many more
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
            f      open the folder, or the one holding the file   U      update Vantage in a new tab
            !      report a problem on GitHub                     1      the Sessions tab
            2      the Worktrees tab                              3      the Config tab
            ←      the tab to the left                            →      the tab to the right
            <      scroll the columns left                        >      scroll the columns right

            Columns
            SETTING  a setting Vantage keeps in config.json, or, in grey, setup it reads but does not change
            VALUE    its value; a setup row that needs attention says what to run
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void SayRResumesEveryShownStoppedOrFailedSessionInItsOwnTab()
    {
        Wording.SessionsKeys.Single(key => key.Key == "R").Tip
            .ShouldBe("Resume each stopped or failed session shown, in its own tab; / filters which.");
    }
}