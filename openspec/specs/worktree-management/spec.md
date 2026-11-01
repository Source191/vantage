# worktree-management Specification

## Purpose
Shows every git worktree across the repository roots with its health and lets the user remove stale ones without losing uncommitted or unpushed work.

## Requirements

### Requirement: Worktrees are listed across all roots
The worktrees view SHALL list every linked worktree of every git repository found directly under the configured roots, wherever the worktree's folder is (under the worktree root, beside the repository or elsewhere), using git's machine-readable worktree listing, each as a row whose WORKTREE cell names the repository and the folder and whose BRANCH cell names the branch.

#### Scenario: Two repositories with worktrees
- **WHEN** two repositories under the roots each have one linked worktree
- **THEN** the view shows two rows, each with a WORKTREE cell reading its repository › its folder's slug, and its branch in BRANCH

#### Scenario: Old and new placements together
- **WHEN** Northwind.Orders has one worktree beside it from an earlier version and one under C:\Users\me\source\worktrees
- **THEN** the view shows both rows, each reading Northwind.Orders › and its slug

### Requirement: Leftover folders are detected
The view SHALL also show folders that git no longer recognises: a folder under a repository's .claude\worktrees that the listing omits is an orphan; a folder directly under a root or under the worktree root whose .git file points at a worktree registration that no longer exists is broken; an empty folder named with a .worktrees suffix directly under a root is empty.

#### Scenario: Orphan folder
- **WHEN** a folder exists under .claude\worktrees but is absent from git's listing
- **THEN** the view shows it with health orphan

#### Scenario: Broken registration
- **WHEN** a sibling folder's .git file names a worktree registration whose repository no longer exists
- **THEN** the view shows it with health broken

#### Scenario: Broken registration under the worktree root
- **WHEN** a folder under C:\Users\me\source\worktrees has a .git file naming a worktree registration that no repository under the roots lists
- **THEN** the view shows it with health broken

#### Scenario: Empty leftover
- **WHEN** an empty folder named Northwind.Orders.worktrees sits directly under a root
- **THEN** the view shows it with health empty

#### Scenario: Worktree of a repository outside the roots
- **WHEN** a folder under C:\Users\me\source\worktrees has a .git file naming the registration D:\other\Northwind.Ledger\.git\worktrees\fix, that registration exists, D:\other\Northwind.Ledger\.git exists, and D:\other is not a root
- **THEN** the view shows it with health elsewhere, its repository as Northwind.Ledger, and the status line says its repository D:\other\Northwind.Ledger is not under the roots

### Requirement: Locked worktrees are marked
A worktree that git reports as locked SHALL show health locked, its HEALTH cell reading ◆ locked and its row in amber, and the detail pane's Health line SHALL show the lock reason when git provides one.

#### Scenario: Locked by another tool
- **WHEN** git reports a worktree as locked and it is selected
- **THEN** its row is amber, its HEALTH cell reads ◆ locked, and the pane reads locked and the lock reason

### Requirement: Each row shows work at risk
For every registered worktree the view SHALL show the age of the last commit and the name of any session whose working directory is that worktree in its row, and the count of changed or untracked files and the count of commits not on any remote branch in the detail pane's Changes line when the row is selected.

#### Scenario: Dirty worktree
- **WHEN** a worktree has five changed files and two unpushed commits and is selected
- **THEN** the pane's Changes line reads 5 changed files · 2 unpushed commits, in amber

### Requirement: Removal refuses to destroy work
Removal SHALL be refused when the worktree has changed or untracked files, unpushed commits, a lock, or a live session inside it, and the refusal SHALL name the reason. When git cannot delete the folder because a tab or program still has it as its working directory, the removal SHALL report the folder as in use, naming it, and SHALL leave the registration for a later remove or prune.

#### Scenario: Unpushed commits
- **WHEN** the user presses remove on a worktree with unpushed commits
- **THEN** nothing is removed and the status line names the branch and the number of unpushed commits

#### Scenario: Clean worktree
- **WHEN** the user presses remove on a worktree with no changes, no unpushed commits, no lock and no session
- **THEN** the worktree is removed from git and from disc

#### Scenario: Folder in use by a shell tab
- **WHEN** the user presses remove on a clean worktree while a PowerShell tab opened from the worktrees view sits in it
- **THEN** the status line says the folder is still in use by a tab or program and names it, and after that tab is closed a second remove deletes what is left and prunes the registration

### Requirement: Forced removal needs a typed confirmation
The force-remove key SHALL remove a worktree despite changes or unpushed commits only after the user enters the worktree's folder name to confirm (shown selected in the dialog so it can be copied and pasted; a wrong or empty answer keeps the dialog open and says what was typed), and SHALL never force-remove a locked worktree or one with a live session. A refusal that cannot be forced SHALL NOT suggest force-remove in the status line.

#### Scenario: Confirmed force
- **WHEN** the user presses force-remove and enters the exact folder name
- **THEN** the worktree is removed along with its changes

#### Scenario: Wrong answer
- **WHEN** the user enters anything else
- **THEN** nothing is removed, the dialog stays open and says what was typed, and Esc keeps the worktree

#### Scenario: Live session inside
- **WHEN** the user presses force-remove on a worktree that a live session is using
- **THEN** removal is refused and the status line names the session

#### Scenario: Refusal that cannot be forced
- **WHEN** plain remove is refused because the worktree is locked or a live session is using it
- **THEN** the status line names the reason and does not mention force-remove, whatever words the reason uses

### Requirement: Branch deletion follows ownership and merge state
After removing a worktree, Vantage SHALL delete its branch automatically only when Vantage created that branch and it is fully merged into the remote default branch, and SHALL otherwise ask before deleting.

#### Scenario: Vantage-created merged branch
- **WHEN** the removed worktree's branch was created by Vantage and is merged
- **THEN** the branch is deleted without asking

#### Scenario: Hand-made branch
- **WHEN** the removed worktree's branch was not created by Vantage
- **THEN** Vantage asks whether to delete the branch and keeps it unless the user agrees

### Requirement: Leftover folders can be deleted as plain folders
For rows with health orphan, broken or empty, the remove key SHALL delete the folder from disc without git, and for broken rows SHALL also prune the stale registration from the owning repository when that repository still exists.

#### Scenario: Delete empty leftover
- **WHEN** the user presses remove on an empty row
- **THEN** the folder is deleted and the row disappears

#### Scenario: Leftover folder with files
- **WHEN** the user presses remove on an orphan folder that still holds twelve files
- **THEN** removal is refused and the status line says twelve untracked files would be lost, and force-remove with the typed confirmation deletes it

### Requirement: Stale registrations can be pruned
The prune key SHALL run git's worktree prune for the selected row's repository and report how many registrations were removed.

#### Scenario: Prune
- **WHEN** a repository has a registration whose folder is gone and the user presses prune
- **THEN** the registration is removed and the status line reports one pruned

### Requirement: A session or shell can be started in a worktree
From a worktree row whose folder exists and whose health is ok, locked or elsewhere, the user SHALL be able to start a new session in that worktree through the launch wizard with the directory preselected, with n, or open a plain PowerShell tab there, with o.

#### Scenario: Start session here
- **WHEN** the user presses n on a worktree row
- **THEN** the launch wizard opens with that worktree as the chosen directory and no new worktree option

#### Scenario: Shell in an elsewhere worktree
- **WHEN** the user presses the open-shell key on an elsewhere row
- **THEN** a PowerShell tab opens in that folder

### Requirement: Scanning is visible
The worktrees view SHALL show a spinner with the words "Scanning worktrees" in the middle of the table while it is shown and no scan has yet returned, and SHALL show a spinner beside the status line's working text while an action started from the view runs; neither spinner SHALL appear for the rescan when the tab is shown again or for the timed rescans.

#### Scenario: First scan pending
- **WHEN** the user opens the worktrees view and the scan of the repository roots has not yet returned
- **THEN** the table area shows a spinner and "Scanning worktrees", which disappear when the first rows arrive

#### Scenario: Rescan on show
- **WHEN** the user opens the worktrees view after its first scan has returned
- **THEN** the earlier rows show at once with no spinner while the rescan runs

#### Scenario: Action running
- **WHEN** the user removes a worktree
- **THEN** the status line shows a spinner before "Removing name" until the removal reports its outcome

### Requirement: Enter on a worktree row goes to its session by identity
Enter on a worktree row with a running session SHALL do what Enter does on that session's row on the Sessions tab, found by its session id so two sessions sharing a name cannot be confused: open the session in its own Windows Terminal tab, or switch to that tab when one is open, or say in the status line why it cannot. The Worktrees tab SHALL stay shown. With no session in the worktree, or one the sessions listing does not hold, the status line SHALL say so.

#### Scenario: Two sessions share a name
- **WHEN** two background sessions are both named fix-tests and one runs in the selected worktree
- **THEN** Enter opens or switches to the tab of the one whose working directory is that worktree

#### Scenario: The session's tab is open
- **WHEN** a Vantage-opened tab for the worktree's background session is alive and the user presses Enter on the worktree row
- **THEN** Windows Terminal switches to that tab, no new tab opens, and the Worktrees tab stays shown in Vantage

#### Scenario: The session has no tab
- **WHEN** the worktree's background session has no live tab and the user presses Enter on the worktree row
- **THEN** a new tab opens attached to that session, as Enter on the Sessions tab opens one

#### Scenario: A terminal session
- **WHEN** the session in the worktree is a plain claude running in its own terminal
- **THEN** nothing opens and the status line says, as on the Sessions tab, that it runs in its own terminal

#### Scenario: No session
- **WHEN** no session runs in the selected worktree and the user presses Enter
- **THEN** nothing opens and the status line says no session runs in that worktree

#### Scenario: Session gone from the listing
- **WHEN** the worktree's row names a session that the latest sessions listing no longer holds
- **THEN** nothing opens and the status line says the session is not listed any more

### Requirement: Counting a leftover folder's files is bounded
The worktree scan SHALL stop counting a leftover folder's files at a fixed limit, so a refresh never walks a whole dependencies tree, and a refusal for such a folder SHALL say that at least that many files would be lost.

#### Scenario: Large leftover
- **WHEN** an orphan folder holds many thousands of files under a dependencies folder
- **THEN** the refresh stops counting at the limit, and pressing remove says at least that many files would be lost

#### Scenario: Small leftover
- **WHEN** an orphan folder holds twelve files
- **THEN** pressing remove says twelve files would be lost

### Requirement: Deleting a leftover folder never follows links
When Vantage deletes a leftover folder from disc, every junction or symbolic link inside it SHALL be removed as a link, without descending into it or changing anything it points at, and the rest of the folder SHALL then be deleted completely, including read-only files.

#### Scenario: Leftover folder holding a junction
- **WHEN** the user force-removes an orphan folder whose node_modules holds a junction to a folder outside it, and that outside folder holds a read-only file
- **THEN** the orphan folder is gone, and the outside folder and its file still exist and the file is still read-only

#### Scenario: Leftover folder with read-only git files
- **WHEN** the user removes a broken folder whose object files git left read-only
- **THEN** the folder is deleted completely

### Requirement: The worktrees table title counts rows and names the selection
The worktrees view's table title SHALL count the rows shown, name the filter when one is set, and name the selected row's folder.

#### Scenario: No filter
- **WHEN** twelve worktrees are listed with no filter and Northwind.Orders-fix-123 is selected
- **THEN** the table title reads 12 worktrees, selected: Northwind.Orders-fix-123

#### Scenario: With a filter
- **WHEN** a filter of "orp" matches one of twelve rows, the folder Northwind.Orders-orphaned
- **THEN** the table title reads 1 of 12 worktrees match "orp", selected: Northwind.Orders-orphaned

### Requirement: The worktrees header and help match the sessions view
The worktrees view SHALL be the Worktrees tab of the console, under the same settings line as the Sessions tab, with its own counts beside the tabs and its own key bar grouped Worktree, Folder and view; its status line SHALL show only messages, never a standing hint or the selected row's health, which the detail pane shows.

#### Scenario: Worktrees help
- **WHEN** the user presses the help key on the Worktrees tab
- **THEN** the help names every column of the worktrees table with its meaning, and its Pane section names every line of the pane, including what each health word means

#### Scenario: Worktrees help on a short terminal
- **WHEN** the user presses the help key in the worktrees view on a 120 x 36 terminal
- **THEN** every line of the help is reachable with the arrow keys and PageDown

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on P Prune in the key bar of the Worktrees tab
- **THEN** a tooltip says it prunes stale registrations in the row's repository

#### Scenario: An unhealthy row selected
- **WHEN** an elsewhere worktree is selected and no key has reported anything
- **THEN** the pane's Health line gives the reason and there is no status line

#### Scenario: Hover over a column header
- **WHEN** the mouse pointer rests on the LAST COMMIT header
- **THEN** a tooltip names the column and explains it

#### Scenario: The worktree bar
- **WHEN** an ok worktree with a session inside it is selected
- **THEN** the first bar row reads d Remove, D Force remove, P Prune, n New session, o Open shell and Enter Open session, and the second c Copy path, f Open folder, s Open solution, p Open PR, / Filter, ? Help, ! Report and q Quit

### Requirement: Deleting a leftover folder never deletes a root
When Vantage deletes a leftover folder from disc, it SHALL refuse a path that is not fully qualified, or that is a drive root or a network share root, before changing anything. The status line SHALL report the refusal as a failure naming the path.

#### Scenario: A drive root reaches the deletion
- **WHEN** a leftover folder's path is C:\
- **THEN** nothing on the drive is deleted and the status line reports a failure naming C:\

#### Scenario: A share root or a relative path reaches the deletion
- **WHEN** a leftover folder's path is \\server\share\ or Northwind.Orders-fix
- **THEN** nothing is deleted and the status line reports a failure naming that path

#### Scenario: An ordinary leftover folder with a trailing separator
- **WHEN** the user removes an orphan folder whose path is written C:\Users\me\source\worktrees\Northwind.Orders-fix\
- **THEN** the folder is deleted as before

### Requirement: A worktree of a repository outside the roots is never deleted as a leftover
For a row with health elsewhere, the remove and force-remove keys SHALL refuse, even after a typed confirmation, with a reason saying the worktree's repository is not under the roots and to add that root in settings or remove the worktree with git there.

#### Scenario: Remove an elsewhere row
- **WHEN** the user presses remove or force-remove on an elsewhere row
- **THEN** nothing is deleted and the status line says the repository is not under the roots, without offering force-remove

### Requirement: One WORKTREE column names the worktree
The worktrees table SHALL show the repository and the folder in one WORKTREE column, as the repository, › and the folder's part after the repository (the slug of a folder named `<repository>-<slug>`, else the folder's label), elastic in width and elided in the middle of the repository name when too long, followed by HEALTH, LAST COMMIT and SESSION.

#### Scenario: A worktree Vantage made
- **WHEN** Northwind.Billing has the worktree C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf on branch invoice-pdf
- **THEN** its WORKTREE cell reads Northwind.Billing › invoice-pdf, and the pane reads Branch invoice-pdf

#### Scenario: A leftover folder
- **WHEN** a broken leftover C:\Users\me\source\worktrees\Northwind.Sales-republish once belonged to Northwind.Sales
- **THEN** its WORKTREE cell reads Northwind.Sales › republish, its HEALTH cell reads ✗ broken and its row is red

#### Scenario: A folder that does not follow the naming
- **WHEN** Northwind.Orders has a hand-made worktree at C:\Users\me\source\worktrees\spike on branch spike
- **THEN** its WORKTREE cell reads Northwind.Orders › spike

#### Scenario: Filter by health
- **WHEN** the user filters by broken
- **THEN** the broken rows stay in the table, each with its HEALTH cell reading ✗ broken

#### Scenario: The help names the column
- **WHEN** the help is shown on the worktrees view
- **THEN** it lists WORKTREE with its meaning, and the header shows the same words on hover

### Requirement: A worktree of a repository outside the roots is elsewhere
A folder directly under a root or under the worktree root whose .git file points at a folder that exists, and that no repository under the roots lists, SHALL be shown with health elsewhere, and its row SHALL name the repository the pointer leads to: the one holding the registration, a bare repository included, or the git directory itself when the pointer names no registration. A relative pointer SHALL be read from the .git file's own folder.

#### Scenario: Directly under a repository root
- **WHEN** a folder directly under C:\Users\me\source\repos has a .git file naming the existing registration D:\other\Northwind.Ledger\.git\worktrees\spike, and D:\other is not a root
- **THEN** the view shows it with health elsewhere and its repository as Northwind.Ledger

#### Scenario: A worktree of a bare repository
- **WHEN** a folder under C:\Users\me\source\worktrees has a .git file naming the existing registration D:\bare\Northwind.Ledger.git\worktrees\spike
- **THEN** the view shows it with health elsewhere and its repository as Northwind.Ledger.git, and force-remove is refused

#### Scenario: A checkout with a separate git directory
- **WHEN** a folder directly under C:\Users\me\source\repos has a .git file naming the existing git directory D:\gitdirs\Northwind.Ledger, which holds no worktrees segment
- **THEN** the view shows it with health elsewhere, not orphan, and force-remove is refused

#### Scenario: A worktree made with relative paths
- **WHEN** a folder under C:\Users\me\source\worktrees has a .git file whose gitdir line is ../../other/Northwind.Ledger/.git/worktrees/spike, and that registration exists relative to the folder
- **THEN** the view shows it with health elsewhere and its repository as Northwind.Ledger, whatever folder Vantage was started from

### Requirement: Leftover work is protected from removal
An orphan or broken folder that still contains files SHALL be refused unless forced, because git no longer tracks that work, and a leftover folder with a live session inside SHALL be refused even when forced.

#### Scenario: A live session in a broken folder
- **WHEN** the user force-removes a broken folder that a live session is running in
- **THEN** removal is refused and the folder is kept

### Requirement: An elsewhere worktree is never deleted as a leftover
A row with health elsewhere is not a leftover and SHALL NOT be deleted as a plain folder.

#### Scenario: Remove on an elsewhere row
- **WHEN** the user presses remove on a row with health elsewhere
- **THEN** nothing is deleted and the status line says its repository is not under the roots

### Requirement: The worktrees view explains itself
The worktrees view's help SHALL explain each of its columns and each line of its detail pane; each item of its key bar SHALL show the key's tooltip while the mouse pointer rests on it, and each of its column headers SHALL show what the column shows.

#### Scenario: Hover over the SESSION header
- **WHEN** the mouse pointer rests on the SESSION header of the worktrees table
- **THEN** a tooltip names the column and explains it

### Requirement: A worktree's details are in the pane
The branch, the pull request, the changed files and the unpushed commits SHALL be shown in the detail pane, and the health's reason in full on its Health line; each row SHALL keep its health's colour beside its HEALTH cell. The filter SHALL still match the repository, the folder, the branch and the health.

#### Scenario: Filter by a branch no cell shows
- **WHEN** Northwind.Orders has the worktree C:\Users\me\source\worktrees\spike on branch feature/refund-retry and the user filters by refund
- **THEN** the row stays in the table though no cell shows the branch, and the pane reads Branch feature/refund-retry

### Requirement: The worktrees are scanned as the console opens
The console SHALL start one worktrees scan as it opens, whichever tab is shown, and that scan SHALL wait until the first sessions listing has returned, whether it succeeded or failed. After it, the worktrees SHALL be rescanned each time their tab is shown and every 15 seconds while it is shown, and never while it is hidden. Neither showing the tab nor the 15-second timer SHALL start a scan while another is running.

#### Scenario: Never opened
- **WHEN** the console has been open for ten minutes on the Sessions tab
- **THEN** the worktrees have been scanned exactly once

#### Scenario: Opened during the first scan
- **WHEN** the user shows the Worktrees tab before the start-up scan has returned
- **THEN** the Worktrees tab waits for that scan, and no second scan starts

#### Scenario: A slow scan
- **WHEN** the Worktrees tab is shown and a scan of fifty repositories takes forty seconds
- **THEN** the 15-second timer starts no other scan until it returns

#### Scenario: Shown again
- **WHEN** the user leaves the Worktrees tab, runs `git worktree remove` on one of its worktrees in a shell and shows the Worktrees tab again
- **THEN** the tab shows its earlier rows at once and drops the removed worktree when the rescan returns

#### Scenario: The sessions listing failed
- **WHEN** the first sessions listing fails as the console opens
- **THEN** the start-up scan still runs and lists the worktrees with no sessions inside them

### Requirement: A worktrees scan reuses the sessions listing
A worktrees scan SHALL take the sessions inside each worktree from the latest sessions listing the console already holds, and SHALL NOT run a Claude Code listing of its own. When no listing has succeeded yet, the scan SHALL treat every worktree as having no session.

#### Scenario: One listing per refresh
- **WHEN** the Worktrees tab is shown and its 15-second rescan runs
- **THEN** no `claude agents` process starts for the rescan, and the SESSION cells match the Sessions tab's latest listing

### Requirement: A worktree's health reads without colour
The worktrees table SHALL show each row's health in a HEALTH column as a glyph and the health's word, in the row's colour: ✓ ok, ◆ locked, △ orphan, ✗ broken, □ empty, ↗ elsewhere; no two healths SHALL share a glyph. The help and the HEALTH header's tooltip SHALL list every glyph with its word.

#### Scenario: Every health in the table
- **WHEN** the table lists one worktree of each health
- **THEN** their HEALTH cells read ✓ ok, ◆ locked, △ orphan, ✗ broken, □ empty and ↗ elsewhere

#### Scenario: Two amber healths
- **WHEN** a locked worktree and an elsewhere worktree are listed
- **THEN** both rows are amber, one HEALTH cell reads ◆ locked and the other ↗ elsewhere

#### Scenario: The help lists the glyphs
- **WHEN** the help is shown on the worktrees view
- **THEN** its HEALTH line lists ✓ ok, ◆ locked, △ orphan, ✗ broken, □ empty and ↗ elsewhere, and the header's tooltip shows the same words

### Requirement: A prunable worktree whose folder exists is a leftover
When git lists a worktree as prunable but its folder still exists, the view SHALL show it with health broken and a reason saying its .git file is gone, and SHALL count its files as for any leftover folder, so the leftover protection applies: a folder holding files is refused unless forced, and a folder with none is deleted and its registration pruned.

#### Scenario: A worktree that lost its .git file
- **WHEN** C:\Users\me\source\worktrees\Northwind.Orders-fix still holds twelve files but its .git file has been deleted, so git lists it as prunable
- **THEN** the view shows it with health broken and the status line says its .git file is gone, not that the folder is gone

#### Scenario: Remove on it while it holds files
- **WHEN** the user presses remove on that row
- **THEN** nothing is deleted and the status line says twelve untracked files would be lost, and force-remove with the typed folder name deletes the folder and prunes the registration

#### Scenario: A half-finished remove left an empty folder
- **WHEN** git lists a worktree as prunable and its folder exists but holds no files
- **THEN** remove deletes the folder and prunes the registration, as before

### Requirement: A leftover deletion reads the disc when the key is pressed
Whether a leftover folder exists and how many files it holds SHALL be read when the remove key is pressed, not taken from the last scan, and the removal rules SHALL decide on those facts.

#### Scenario: Files written after the scan
- **WHEN** an empty leftover row was scanned and a program has since written three files into its folder
- **THEN** remove deletes nothing and the status line says three untracked files would be lost

### Requirement: A leftover folder is deleted only inside the roots
Before Vantage deletes a leftover folder from disc, it SHALL refuse a path that is not under a repository root or the worktree root, changing nothing, and the status line SHALL say the folder is outside the roots and name it. Force-remove SHALL NOT be offered for this refusal.

#### Scenario: A registration that points outside the roots
- **WHEN** a repository under C:\Users\me\source\repos registers a worktree at D:\Northwind.Archive, git lists it as prunable, and the user presses remove or force-remove on its row
- **THEN** D:\Northwind.Archive is not deleted and the status line says the folder is outside the roots, naming D:\Northwind.Archive

#### Scenario: A leftover inside the roots
- **WHEN** the user removes an empty leftover folder C:\Users\me\source\repos\Northwind.Orders.worktrees
- **THEN** the folder is deleted as before
