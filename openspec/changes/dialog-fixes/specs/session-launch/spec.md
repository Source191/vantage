# Spec Delta

## ADDED Requirements

### Requirement: The wizard's keys move through the form in order
Tab and Enter SHALL move focus through the wizard in its reading order: from the repository list to the name, from the name to Where when it is shown and otherwise to the prompt, from Where to the base, and from the base to the prompt. Enter SHALL never close the wizard or start a session, except in a fixed folder's name field, and only F5, F6 and the buttons SHALL start one.

#### Scenario: Tab from the list
- **WHEN** the user presses Tab on the repository list
- **THEN** focus moves to the name field, as it does on Enter

#### Scenario: Enter on Where
- **WHEN** the user presses Enter on the Where selector
- **THEN** focus moves to the worktree base and the wizard stays open

#### Scenario: Enter on the base
- **WHEN** the user presses Enter on the worktree base selector
- **THEN** focus moves to the opening prompt and the wizard stays open

#### Scenario: Enter in the prompt
- **WHEN** the user presses Enter in the opening prompt
- **THEN** the prompt gains a new line and the wizard stays open

## MODIFIED Requirements

### Requirement: Repository is chosen from configured roots
The wizard SHALL offer every git repository's main checkout found directly under the configured repository roots, the most recently used repositories first, and SHALL NOT offer a root folder itself or a linked worktree directly under a root.

#### Scenario: Filtering the picker
- **WHEN** the user types part of a repository name
- **THEN** the picker narrows to repositories whose names contain that text, case-insensitively

#### Scenario: Recently used first
- **WHEN** the picker opens
- **THEN** the ten most recently launched repositories appear before the rest

#### Scenario: Old sibling worktree under a root
- **WHEN** the root holds Northwind.Orders and its linked worktree Northwind.Orders-fix-123
- **THEN** the picker offers Northwind.Orders and does not offer Northwind.Orders-fix-123

#### Scenario: Roots are not choices
- **WHEN** the picker opens with the root C:\Users\me\source\repos
- **THEN** no "(no repository)" entry is listed; a session in the root folder itself is started with vantage here from that folder

### Requirement: A new worktree is the default launch mode
The wizard opened from the overview SHALL default to starting the session in a new worktree, and SHALL offer the repository's main checkout on whatever branch is checked out as the alternative. The wizard opened on a fixed folder SHALL offer no choice, start on that folder's current branch, show only the directory, the name, the preview and the prompt in a dialog sized to them, and start the session and open its tab on Enter in the name field.

#### Scenario: Accepting the defaults
- **WHEN** the user opens the wizard with `n`, chooses Northwind.Orders, names the session "Fix 123" and accepts the other defaults
- **THEN** the session starts in a new worktree Northwind.Orders-fix-123 under the worktree root, on branch fix-123

#### Scenario: Choosing the current branch
- **WHEN** the user opens the wizard with `n`, chooses Northwind.Orders and switches Where to "Current branch"
- **THEN** the session starts in the repository's main checkout and no worktree is created

#### Scenario: Default left in place for a folder that is not a repository
- **WHEN** a session is wanted in a folder that is not a repository, such as a root folder itself
- **THEN** the wizard offers no such folder, so the worktree default never meets one, and vantage here run from that folder starts the session there with no worktree

#### Scenario: Wizard on a fixed folder
- **WHEN** the user starts a session with `s` on a worktree row, types a name and presses Enter
- **THEN** the session starts in that worktree on its current branch, no new worktree is created, and it opens in its own tab

#### Scenario: Fixed folder without a tab
- **WHEN** the user starts a session with `s` on a worktree row, types a name and presses F5
- **THEN** the session starts in that worktree and no tab is opened

#### Scenario: Fixed folder dialog is compact
- **WHEN** the wizard opens from a worktree row
- **THEN** the dialog shows the directory, the name, the preview and the prompt from its left edge, with no empty repository column, and its hint line says Enter starts and opens in a tab
