# worktree-management Specification

## Purpose
Shows every git worktree across the repository roots with its health and lets the user remove stale ones without losing uncommitted or unpushed work.

## Requirements

### Requirement: Worktrees are listed across all roots
The worktrees view SHALL list every linked worktree of every git repository found directly under the configured roots, wherever the worktree's folder is (under the worktree root, beside the repository or elsewhere), using git's machine-readable worktree listing.

#### Scenario: Two repositories with worktrees
- **WHEN** two repositories under the roots each have one linked worktree
- **THEN** the view shows two rows, each naming its repository, path and branch

#### Scenario: Old and new placements together
- **WHEN** Northwind.Orders has one worktree beside it from an earlier version and one under C:\Users\me\source\worktrees
- **THEN** the view shows both rows

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

### Requirement: Locked worktrees are marked
A worktree that git reports as locked SHALL show health locked and SHALL show the lock reason when git provides one.

#### Scenario: Locked by another tool
- **WHEN** git reports a worktree as locked
- **THEN** the row shows locked and the lock reason

### Requirement: Each row shows work at risk
For every registered worktree the view SHALL show the count of changed or untracked files, the count of commits not on any remote branch, the age of the last commit, and the name of any session whose working directory is that worktree.

#### Scenario: Dirty worktree
- **WHEN** a worktree has five changed files and two unpushed commits
- **THEN** its row shows dirty 5 and unpushed 2

### Requirement: Removal refuses to destroy work
Removal SHALL be refused when the worktree has changed or untracked files, unpushed commits, a lock, or a live session inside it, and the refusal SHALL name the reason.

#### Scenario: Unpushed commits
- **WHEN** the user presses remove on a worktree with unpushed commits
- **THEN** nothing is removed and the footer names the branch and the number of unpushed commits

#### Scenario: Clean worktree
- **WHEN** the user presses remove on a worktree with no changes, no unpushed commits, no lock and no session
- **THEN** the worktree is removed from git and from disc

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
For rows with health orphan, broken or empty, the remove key SHALL delete the folder from disc without git, and for broken rows SHALL also prune the stale registration from the owning repository when that repository still exists. An orphan or broken folder that still contains files SHALL be refused unless forced, because git no longer tracks that work, and a leftover folder with a live session inside SHALL be refused even when forced.

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
From a worktree row the user SHALL be able to start a new session in that worktree through the launch wizard with the directory preselected, or open a plain PowerShell tab there.

#### Scenario: Start session here
- **WHEN** the user presses the start-session key on a worktree row
- **THEN** the launch wizard opens with that worktree as the chosen directory and no new worktree option

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
The worktrees view's header SHALL show the Vantage version, the Claude Code CLI version and the repository roots as the sessions view's does; its help SHALL explain each of its columns; each of its key hints SHALL show the key's help line as a tooltip while the mouse pointer rests on it, and each of its column headers SHALL show what the column shows.

#### Scenario: Worktrees help
- **WHEN** the user presses the help key in the worktrees view
- **THEN** the help names every column of the worktrees table with its meaning, including what each health word means

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on the hint for p in the worktrees view
- **THEN** a tooltip shows the help line for p, saying it prunes stale registrations in the row's repository

#### Scenario: Hover over a column header
- **WHEN** the mouse pointer rests on the HEALTH header
- **THEN** a tooltip names the column and explains what each health word means
