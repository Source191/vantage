# Spec Delta

## MODIFIED Requirements

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
