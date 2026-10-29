# Spec Delta

## ADDED Requirements

### Requirement: A worktree of a repository outside the roots is never deleted as a leftover
For a row with health elsewhere, the remove and force-remove keys SHALL refuse, even after a typed confirmation, with a reason saying the worktree's repository is not under the roots and to add that root in settings or remove the worktree with git there.

#### Scenario: Remove an elsewhere row
- **WHEN** the user presses remove or force-remove on an elsewhere row
- **THEN** nothing is deleted and the footer says the repository is not under the roots, without offering force-remove

## MODIFIED Requirements

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

### Requirement: Leftover folders can be deleted as plain folders
For rows with health orphan, broken or empty, the remove key SHALL delete the folder from disc without git, and for broken rows SHALL also prune the stale registration from the owning repository when that repository still exists. An orphan or broken folder that still contains files SHALL be refused unless forced, because git no longer tracks that work, and a leftover folder with a live session inside SHALL be refused even when forced. A row with health elsewhere is not a leftover and SHALL NOT be deleted this way.

#### Scenario: Delete empty leftover
- **WHEN** the user presses remove on an empty row
- **THEN** the folder is deleted and the row disappears

#### Scenario: Leftover folder with files
- **WHEN** the user presses remove on an orphan folder that still holds twelve files
- **THEN** removal is refused and the footer says twelve untracked files would be lost, and force-remove with the typed confirmation deletes it

### Requirement: A session or shell can be started in a worktree
From a worktree row whose folder exists and whose health is ok, locked or elsewhere, the user SHALL be able to start a new session in that worktree through the launch wizard with the directory preselected, or open a plain PowerShell tab there.

#### Scenario: Start session here
- **WHEN** the user presses the start-session key on a worktree row
- **THEN** the launch wizard opens with that worktree as the chosen directory and no new worktree option

#### Scenario: Shell in an elsewhere worktree
- **WHEN** the user presses the open-shell key on an elsewhere row
- **THEN** a PowerShell tab opens in that folder
