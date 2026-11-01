## MODIFIED Requirements

### Requirement: Branch deletion follows ownership and merge state
After removing a worktree, Vantage SHALL delete its branch automatically only when Vantage created that branch and it is fully merged into the remote default branch, and SHALL otherwise ask before deleting. A worktree whose head is detached, or whose head git did not name, has no branch of its own: it SHALL be removed with no branch deleted and no question asked, and its folder's name SHALL never be taken for a branch.

#### Scenario: Vantage-created merged branch
- **WHEN** the removed worktree's branch was created by Vantage and is merged
- **THEN** the branch is deleted without asking

#### Scenario: Hand-made branch
- **WHEN** the removed worktree's branch was not created by Vantage
- **THEN** Vantage asks whether to delete the branch and keeps it unless the user agrees

#### Scenario: Detached head
- **WHEN** the user removes a clean worktree at C:\Users\me\source\repos\Northwind.Orders-fix-123 that git lists on a detached head, and a branch named Northwind.Orders-fix-123 exists in the repository
- **THEN** the worktree is removed, no branch is deleted, no question about a branch is asked, and the status line says the worktree was removed

#### Scenario: Head git could not name
- **WHEN** the user removes a clean worktree whose head git did not report
- **THEN** the worktree is removed and no branch is asked about or deleted

### Requirement: Leftover folders can be deleted as plain folders
For rows with health orphan, broken or empty, the remove key SHALL delete the folder from disc without git, and for broken rows SHALL also prune the stale registration from the owning repository when that repository still exists. When the folder cannot be deleted because a tab or program still has it open, the removal SHALL report the folder as in use, naming it, in the same words as a worktree git could not delete, and SHALL prune nothing.

#### Scenario: Delete empty leftover
- **WHEN** the user presses remove on an empty row
- **THEN** the folder is deleted and the row disappears

#### Scenario: Leftover folder with files
- **WHEN** the user presses remove on an orphan folder that still holds twelve files
- **THEN** removal is refused and the status line says twelve untracked files would be lost, and force-remove with the typed confirmation deletes it

#### Scenario: Leftover folder held open
- **WHEN** the user presses remove on an empty broken folder C:\Users\me\source\worktrees\Northwind.Orders-stale while a PowerShell tab sits in it, so the folder cannot be deleted
- **THEN** the status line says the folder Northwind.Orders-stale is still in use by a tab or program and to close it and remove again, no registration is pruned, and no exception message is shown

## ADDED Requirements

### Requirement: Counting a leftover folder's files never follows links or fails
When the scan or a removal key counts a leftover folder's files, it SHALL NOT enter a junction or symbolic link inside the folder, and SHALL skip a subfolder it cannot read rather than fail, so one such folder never blanks the worktrees view or stops a removal.

#### Scenario: A junction inside a leftover
- **WHEN** an orphan folder holds three files and a node_modules junction pointing at a folder with thousands of files
- **THEN** the row counts three files, and removing it says three untracked files would be lost

#### Scenario: An unreadable subfolder inside a leftover
- **WHEN** a broken folder holds two files the user can read and one subfolder the user cannot
- **THEN** the worktrees view still lists every row, that row counts two files, and no failure is shown
