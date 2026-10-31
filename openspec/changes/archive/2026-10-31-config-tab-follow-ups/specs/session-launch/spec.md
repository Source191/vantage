# Spec Delta

## MODIFIED Requirements

### Requirement: A new worktree is the default launch mode
The wizard opened from the overview SHALL start on the default place setting, a new worktree unless the user has chosen the current branch, and SHALL offer the other as the alternative: a new worktree, or the repository's main checkout on whatever branch is checked out. The wizard opened on a fixed folder SHALL offer no choice and start on that folder's current branch, whatever the setting.

#### Scenario: Accepting the defaults
- **WHEN** the default place is New worktree and the user opens the wizard with `n`, chooses Northwind.Orders, names the session "Fix 123" and accepts the other defaults
- **THEN** the session starts in a new worktree Northwind.Orders-fix-123 under the worktree root, on branch fix-123

#### Scenario: Choosing the current branch
- **WHEN** the user opens the wizard with `n`, chooses Northwind.Orders and switches Where to "Current branch"
- **THEN** the session starts in the repository's main checkout and no worktree is created

#### Scenario: Current branch by default
- **WHEN** the default place is Current branch and the user opens the wizard with `n`, chooses Northwind.Orders and names the session
- **THEN** Where reads Current branch and the session starts in the main checkout unless the user switches it

#### Scenario: Default left in place for a folder that is not a repository
- **WHEN** the user opens the wizard with `n`, chooses "(no repository)", names the session and starts without changing Where while it reads New worktree
- **THEN** the wizard starts nothing and explains that a worktree needs a repository

#### Scenario: Wizard on a fixed folder
- **WHEN** the default place is New worktree and the user starts a session with `s` on a worktree row
- **THEN** the session starts in that worktree on its current branch and no new worktree is created

#### Scenario: Fixed folder without a tab
- **WHEN** the user starts a session with `s` on a worktree row, types a name and presses F5
- **THEN** the session starts in that worktree and no tab is opened

#### Scenario: Fixed folder dialog is compact
- **WHEN** the wizard opens from a worktree row
- **THEN** the dialog shows the directory, the name, the preview and the prompt from its left edge, with no empty repository column, and its hint line says Enter starts and opens in a tab
