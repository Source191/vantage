# Spec Delta

## MODIFIED Requirements

### Requirement: Repository is chosen from configured roots
The wizard SHALL offer every git repository found directly under the configured repository roots, the most recently used repositories first, and a "no repository" option that starts in the roots folder.

#### Scenario: Filtering the picker
- **WHEN** the user types part of a repository name
- **THEN** the picker narrows to repositories whose names contain that text, case-insensitively

#### Scenario: Recently used first
- **WHEN** the picker opens
- **THEN** the ten most recently launched repositories appear before the rest

### Requirement: Vantage records what it created
Vantage SHALL record each worktree and branch it created, each launched repository and each session description in its own state folder, and SHALL keep every record when two Vantage processes write at the same time.

#### Scenario: Worktree recorded
- **WHEN** the wizard creates a worktree
- **THEN** the state folder records the worktree path, branch, repository and creation time

#### Scenario: Two processes record at once
- **WHEN** `vantage here` records a description while the console records a recent repository
- **THEN** both records are present on the next read

#### Scenario: Existing state is kept
- **WHEN** Vantage starts with recent repositories and descriptions saved by an earlier version
- **THEN** they are still listed and shown
