# Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: A new worktree is a sibling of the repository`
- TO: `### Requirement: A new worktree goes under the worktree root`

## MODIFIED Requirements

### Requirement: Repository is chosen from configured roots
The wizard SHALL offer every git repository's main checkout found directly under the configured repository roots, the most recently used repositories first, and a "no repository" option that starts in the roots folder. A linked worktree directly under a root SHALL NOT be offered as a repository.

#### Scenario: Filtering the picker
- **WHEN** the user types part of a repository name
- **THEN** the picker narrows to repositories whose names contain that text, case-insensitively

#### Scenario: Recently used first
- **WHEN** the picker opens
- **THEN** the ten most recently launched repositories appear before the rest

#### Scenario: Old sibling worktree under a root
- **WHEN** the root holds Northwind.Orders and its linked worktree Northwind.Orders-fix-123
- **THEN** the picker offers Northwind.Orders and does not offer Northwind.Orders-fix-123

### Requirement: A new worktree goes under the worktree root
When the user chooses a worktree, Vantage SHALL create it at `<worktree root>\<repository>-<slug>` on a new branch named `<slug>`, creating the worktree root when it does not exist, where the slug is the name lower-cased with every run of characters other than a-z and 0-9 replaced by one hyphen and leading and trailing hyphens removed.

#### Scenario: Slug derivation
- **WHEN** the session name is "Fix #123: flaky tests!" and the worktree root is C:\Users\me\source\worktrees
- **THEN** the worktree is created at C:\Users\me\source\worktrees\Northwind.Orders-fix-123-flaky-tests on branch fix-123-flaky-tests

#### Scenario: Worktree root does not exist yet
- **WHEN** the worktree root C:\Users\me\source\worktrees does not exist and the user starts a session in a new worktree
- **THEN** the folder is created and the worktree is made inside it

#### Scenario: Name yields an empty slug
- **WHEN** the session name contains no letters or digits
- **THEN** the wizard refuses the worktree option and explains why

#### Scenario: Worktree already exists
- **WHEN** a folder already exists at the worktree path under the worktree root
- **THEN** the wizard offers to start the session in the existing worktree instead of creating one

#### Scenario: Preview names the place
- **WHEN** the user types the name fix-123 with the worktree option chosen
- **THEN** the wizard's preview names the folder Northwind.Orders-fix-123 under the worktree root and the branch fix-123
