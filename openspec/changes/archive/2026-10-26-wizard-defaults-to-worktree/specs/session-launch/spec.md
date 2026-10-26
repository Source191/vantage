# Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: The current branch is the default launch mode`
- TO: `### Requirement: A new worktree is the default launch mode`

## MODIFIED Requirements

### Requirement: A new worktree is the default launch mode
The wizard opened from the overview SHALL default to starting the session in a new worktree, and SHALL offer the repository's main checkout on whatever branch is checked out as the alternative. The wizard opened on a fixed folder SHALL offer no choice and start on that folder's current branch.

#### Scenario: Accepting the defaults
- **WHEN** the user opens the wizard with `n`, chooses Northwind.Orders, names the session "Fix 123" and accepts the other defaults
- **THEN** the session starts in a new worktree Northwind.Orders-fix-123 under the worktree root, on branch fix-123

#### Scenario: Choosing the current branch
- **WHEN** the user opens the wizard with `n`, chooses Northwind.Orders and switches Where to "Current branch"
- **THEN** the session starts in the repository's main checkout and no worktree is created

#### Scenario: Default left in place for a folder that is not a repository
- **WHEN** the user opens the wizard with `n`, chooses "(no repository)", names the session and starts without changing Where
- **THEN** the wizard starts nothing and explains that a worktree needs a repository

#### Scenario: Wizard on a fixed folder
- **WHEN** the user starts a session with `s` on a worktree row
- **THEN** the session starts in that worktree on its current branch and no new worktree is created
