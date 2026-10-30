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
The view SHALL also show folders that git no longer recognises: a folder under a repository's .claude\worktrees that the listing omits is an orphan; a folder directly under a root or under the worktree root whose .git file points at a worktree registration that no longer exists is broken; an empty folder named with a .worktrees suffix directly under a root is empty. A folder directly under a root or under the worktree root whose .git file points at a registration that still exists, in a repository that still has its .git folder but is not under the roots, is elsewhere, and its row SHALL name that repository.

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
- **THEN** the view shows it with health elsewhere, its repository as Northwind.Ledger, and the footer says its repository D:\other\Northwind.Ledger is not under the roots

### Requirement: Locked worktrees are marked
A worktree that git reports as locked SHALL show health locked, its row in amber, and the detail pane's Health line SHALL show the lock reason when git provides one.

#### Scenario: Locked by another tool
- **WHEN** git reports a worktree as locked and it is selected
- **THEN** its row is amber and the pane reads locked and the lock reason

### Requirement: Each row shows work at risk
For every registered worktree the view SHALL show the age of the last commit and the name of any session whose working directory is that worktree in its row, and the count of changed or untracked files and the count of commits not on any remote branch in the detail pane's Changes line when the row is selected.

#### Scenario: Dirty worktree
- **WHEN** a worktree has five changed files and two unpushed commits and is selected
- **THEN** the pane's Changes line reads 5 changed files · 2 unpushed commits, in amber

### Requirement: Removal refuses to destroy work
Removal SHALL be refused when the worktree has changed or untracked files, unpushed commits, a lock, or a live session inside it, and the refusal SHALL name the reason. When git cannot delete the folder because a tab or program still has it as its working directory, the removal SHALL report the folder as in use, naming it, and SHALL leave the registration for a later remove or prune.

#### Scenario: Unpushed commits
- **WHEN** the user presses remove on a worktree with unpushed commits
- **THEN** nothing is removed and the footer names the branch and the number of unpushed commits

#### Scenario: Clean worktree
- **WHEN** the user presses remove on a worktree with no changes, no unpushed commits, no lock and no session
- **THEN** the worktree is removed from git and from disc

#### Scenario: Folder in use by a shell tab
- **WHEN** the user presses remove on a clean worktree while a PowerShell tab opened from the worktrees view sits in it
- **THEN** the footer says the folder is still in use by a tab or program and names it, and after that tab is closed a second remove deletes what is left and prunes the registration

### Requirement: Forced removal needs a typed confirmation
The force-remove key SHALL remove a worktree despite changes or unpushed commits only after the user enters the worktree's folder name to confirm (shown selected in the dialog so it can be copied and pasted; a wrong or empty answer keeps the dialog open and says what was typed), and SHALL never force-remove a locked worktree or one with a live session. A refusal that cannot be forced SHALL NOT suggest force-remove in the footer.

#### Scenario: Confirmed force
- **WHEN** the user presses force-remove and enters the exact folder name
- **THEN** the worktree is removed along with its changes

#### Scenario: Wrong answer
- **WHEN** the user enters anything else
- **THEN** nothing is removed, the dialog stays open and says what was typed, and Esc keeps the worktree

#### Scenario: Live session inside
- **WHEN** the user presses force-remove on a worktree that a live session is using
- **THEN** removal is refused and the footer names the session

#### Scenario: Refusal that cannot be forced
- **WHEN** plain remove is refused because the worktree is locked or a live session is using it
- **THEN** the footer names the reason and does not mention force-remove, whatever words the reason uses

### Requirement: Branch deletion follows ownership and merge state
After removing a worktree, Vantage SHALL delete its branch automatically only when Vantage created that branch and it is fully merged into the remote default branch, and SHALL otherwise ask before deleting.

#### Scenario: Vantage-created merged branch
- **WHEN** the removed worktree's branch was created by Vantage and is merged
- **THEN** the branch is deleted without asking

#### Scenario: Hand-made branch
- **WHEN** the removed worktree's branch was not created by Vantage
- **THEN** Vantage asks whether to delete the branch and keeps it unless the user agrees

### Requirement: Leftover folders can be deleted as plain folders
For rows with health orphan, broken or empty, the remove key SHALL delete the folder from disc without git, and for broken rows SHALL also prune the stale registration from the owning repository when that repository still exists. An orphan or broken folder that still contains files SHALL be refused unless forced, because git no longer tracks that work, and a leftover folder with a live session inside SHALL be refused even when forced. A row with health elsewhere is not a leftover and SHALL NOT be deleted this way.

#### Scenario: Delete empty leftover
- **WHEN** the user presses remove on an empty row
- **THEN** the folder is deleted and the row disappears

#### Scenario: Leftover folder with files
- **WHEN** the user presses remove on an orphan folder that still holds twelve files
- **THEN** removal is refused and the footer says twelve untracked files would be lost, and force-remove with the typed confirmation deletes it

### Requirement: Stale registrations can be pruned
The prune key SHALL run git's worktree prune for the selected row's repository and report how many registrations were removed.

#### Scenario: Prune
- **WHEN** a repository has a registration whose folder is gone and the user presses prune
- **THEN** the registration is removed and the footer reports one pruned

### Requirement: A session or shell can be started in a worktree
From a worktree row whose folder exists and whose health is ok, locked or elsewhere, the user SHALL be able to start a new session in that worktree through the launch wizard with the directory preselected, or open a plain PowerShell tab there.

#### Scenario: Start session here
- **WHEN** the user presses the start-session key on a worktree row
- **THEN** the launch wizard opens with that worktree as the chosen directory and no new worktree option

#### Scenario: Shell in an elsewhere worktree
- **WHEN** the user presses the open-shell key on an elsewhere row
- **THEN** a PowerShell tab opens in that folder

### Requirement: Scanning is visible
The worktrees view SHALL show a spinner with the words "Scanning worktrees" in the middle of the table until the first scan arrives, and SHALL show a spinner beside the footer's working text while an action started from the view runs; neither spinner SHALL appear for the timed rescans.

#### Scenario: First scan pending
- **WHEN** the user opens the worktrees view and the scan of the repository roots has not yet returned
- **THEN** the table area shows a spinner and "Scanning worktrees", which disappear when the first rows arrive

#### Scenario: Action running
- **WHEN** the user removes a worktree
- **THEN** the footer shows a spinner before "Removing name" until the removal reports its outcome

### Requirement: Enter on a worktree row goes to its session by identity
Enter on a worktree row with a running session SHALL switch to the sessions view with that session selected by its session id, so two sessions sharing a name cannot be confused.

#### Scenario: Two sessions share a name
- **WHEN** two sessions are both named fix-tests and one runs in the selected worktree
- **THEN** the sessions view opens with the one whose working directory is that worktree selected

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
The worktrees view SHALL be the Worktrees tab of the console, under the same settings line as the Sessions tab, with its own counts beside the tabs and its own key bar grouped Worktree, Folder and view; its status line SHALL show only messages, never a standing hint or the selected row's health, which the detail pane shows; its help SHALL explain each of its columns and each line of its detail pane; each item of its key bar SHALL show the key's tooltip while the mouse pointer rests on it, and each of its column headers SHALL show what the column shows.

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
- **THEN** the first bar row reads d Remove, D Force remove, P Prune, s Start session, o Open shell and Enter Go to session, and the second c Copy path, e Open folder, v Solution, p Open PR, / Filter, ? Help, ! Report and q Quit

### Requirement: Deleting a leftover folder never deletes a root
When Vantage deletes a leftover folder from disc, it SHALL refuse a path that is not fully qualified, or that is a drive root or a network share root, before changing anything. The footer SHALL report the refusal as a failure naming the path.

#### Scenario: A drive root reaches the deletion
- **WHEN** a leftover folder's path is C:\
- **THEN** nothing on the drive is deleted and the footer reports a failure naming C:\

#### Scenario: A share root or a relative path reaches the deletion
- **WHEN** a leftover folder's path is \\server\share\ or Northwind.Orders-fix
- **THEN** nothing is deleted and the footer reports a failure naming that path

#### Scenario: An ordinary leftover folder with a trailing separator
- **WHEN** the user removes an orphan folder whose path is written C:\Users\me\source\worktrees\Northwind.Orders-fix\
- **THEN** the folder is deleted as before

### Requirement: A worktree of a repository outside the roots is never deleted as a leftover
For a row with health elsewhere, the remove and force-remove keys SHALL refuse, even after a typed confirmation, with a reason saying the worktree's repository is not under the roots and to add that root in settings or remove the worktree with git there.

#### Scenario: Remove an elsewhere row
- **WHEN** the user presses remove or force-remove on an elsewhere row
- **THEN** nothing is deleted and the footer says the repository is not under the roots, without offering force-remove

### Requirement: One WORKTREE column names the worktree
The worktrees table SHALL show the repository and the folder in one WORKTREE column, as the repository, › and the folder's part after the repository (the slug of a folder named `<repository>-<slug>`, else the folder's label), elastic in width and elided in the middle of the repository name when too long, followed by LAST COMMIT and SESSION; the branch, the pull request, the changed files, the unpushed commits and the health SHALL be shown in the detail pane, and each row SHALL keep its health's colour. The filter SHALL still match the repository, the folder, the branch and the health.

#### Scenario: A worktree Vantage made
- **WHEN** Northwind.Billing has the worktree C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf on branch invoice-pdf
- **THEN** its WORKTREE cell reads Northwind.Billing › invoice-pdf, and the pane reads Branch invoice-pdf

#### Scenario: A leftover folder
- **WHEN** a broken leftover C:\Users\me\source\worktrees\Northwind.Sales-republish once belonged to Northwind.Sales
- **THEN** its WORKTREE cell reads Northwind.Sales › republish and its row is red

#### Scenario: A folder that does not follow the naming
- **WHEN** Northwind.Orders has a hand-made worktree at C:\Users\me\source\worktrees\spike on branch spike
- **THEN** its WORKTREE cell reads Northwind.Orders › spike

#### Scenario: Filter by health
- **WHEN** the user filters by broken
- **THEN** the broken rows stay in the table though no cell shows the health

#### Scenario: The help names the column
- **WHEN** the help is shown on the worktrees view
- **THEN** it lists WORKTREE with its meaning, and the header shows the same words on hover
