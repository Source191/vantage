# Spec Delta

## ADDED Requirements

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

## MODIFIED Requirements

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
