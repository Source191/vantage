## MODIFIED Requirements

### Requirement: Rows show repository, branch and worktree
For each session Vantage SHALL derive the repository, the checked-out branch and the folder it runs in, and SHALL show the folder relative to its root as the folder-actions spec describes; a session in a linked git worktree is recognisable because its folder differs from its repository's, so the row carries no separate worktree marker.

#### Scenario: Session in a sibling worktree
- **WHEN** a session's working directory is the linked worktree C:\Users\me\source\worktrees\Northwind.Orders-fix-123 of Northwind.Orders
- **THEN** the row shows Northwind.Orders as the repository, the worktree's branch, and Northwind.Orders-fix-123 as the folder

#### Scenario: Session outside any repository
- **WHEN** a session's working directory is not inside a git repository
- **THEN** the row shows the folder name as the repository, leaves the branch blank, and shows the folder relative to its root

#### Scenario: Session whose folder no longer exists
- **WHEN** a session's working directory has been deleted
- **THEN** the listing still succeeds, and the row shows the folder name as the repository and leaves the branch blank
