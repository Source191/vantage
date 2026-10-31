## MODIFIED Requirements

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

### Requirement: Scanning is visible
The worktrees view SHALL show a spinner with the words "Scanning worktrees" in the middle of the table until the first scan arrives, and SHALL show a spinner beside the status line's working text while an action started from the view runs; neither spinner SHALL appear for the timed rescans.

#### Scenario: First scan pending
- **WHEN** the user opens the worktrees view and the scan of the repository roots has not yet returned
- **THEN** the table area shows a spinner and "Scanning worktrees", which disappear when the first rows arrive

#### Scenario: Action running
- **WHEN** the user removes a worktree
- **THEN** the status line shows a spinner before "Removing name" until the removal reports its outcome

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

### Requirement: An elsewhere worktree is never deleted as a leftover
A row with health elsewhere is not a leftover and SHALL NOT be deleted as a plain folder.

#### Scenario: Remove on an elsewhere row
- **WHEN** the user presses remove on a row with health elsewhere
- **THEN** nothing is deleted and the status line says its repository is not under the roots
