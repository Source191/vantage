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
- **WHEN** the default place is New worktree and the user starts a session with `n` on a worktree row
- **THEN** the session starts in that worktree on its current branch and no new worktree is created

#### Scenario: Fixed folder without a tab
- **WHEN** the user starts a session with `n` on a worktree row, types a name and presses F5
- **THEN** the session starts in that worktree and opens in its own tab, as on Enter, because the wizard has no way to start without one

#### Scenario: Fixed folder dialog is compact
- **WHEN** the wizard opens from a worktree row
- **THEN** the dialog shows the directory, the name, the preview and the prompt from its left edge, with no empty repository column, and its hint line says Enter or F5 starts and opens in a tab

### Requirement: The wizard reads as one form
The wizard opened from the overview SHALL lay its controls out in one column in reading order: the repository filter with its list beneath, the session name, Where, the worktree base, the preview, the opening prompt, the problem line, the hint, and the buttons Start (F5) and Cancel. The dialog SHALL fit a 120 by 36 terminal with at least four repository rows visible, and Enter SHALL never start a session in it.

#### Scenario: Reading order
- **WHEN** the user opens the wizard with n
- **THEN** the filter is above the repository list, which is above the name, Where, the base, the preview and the prompt, in one column

#### Scenario: Fits the laptop screen
- **WHEN** the wizard opens on a 120 by 36 terminal with twelve repositories under the roots
- **THEN** every control and the buttons are on screen and at least four repositories are listed

#### Scenario: One start button
- **WHEN** the user opens the wizard with n
- **THEN** the buttons are Start (F5) and Cancel, and no button starts a session without opening its tab

#### Scenario: Enter in the prompt
- **WHEN** the user presses Enter in the opening prompt
- **THEN** the prompt gains a new line and nothing starts

#### Scenario: The compact form is unchanged
- **WHEN** the user presses n on a worktree row
- **THEN** the compact fixed-folder dialog opens as before, and Enter in its name starts the session and opens its tab
