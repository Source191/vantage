## MODIFIED Requirements

### Requirement: Gitignored local files are copied into a new worktree
When the repository contains a `.worktreeinclude` file, Vantage SHALL copy the gitignored files it matches into the new worktree before starting the session. When the copy fails because a file cannot be read or written, the session SHALL NOT start, the worktree SHALL be kept and remain recorded as Vantage's own, and the status line SHALL say the session could not start and give the reason.

#### Scenario: Include file present
- **WHEN** `.worktreeinclude` lists appsettings.Development.json and that file is gitignored in the main checkout
- **THEN** the file exists in the new worktree before the session starts

#### Scenario: Names with non-ASCII characters, spaces and apostrophes
- **WHEN** `.worktreeinclude` matches the gitignored files Café.local.json, my app.local.json and it's.local.json
- **THEN** all three exist in the new worktree under the same names, and the copy reports all three

#### Scenario: A file cannot be copied
- **WHEN** `.worktreeinclude` matches a file that another program holds locked, so the copy fails
- **THEN** no session starts, the worktree stays on disc and in Vantage's records, and the status line says the session could not start and why

### Requirement: Vantage records what it created
Vantage SHALL record each worktree and branch it created and each launched repository in its own state folder, and SHALL keep every record when two Vantage processes write at the same time. A new worktree SHALL be recorded as soon as git has created it, before its include files are copied, so a worktree that exists is never one Vantage does not know it made.

#### Scenario: Worktree recorded
- **WHEN** the wizard creates a worktree
- **THEN** the state folder records the worktree path, branch, repository and creation time

#### Scenario: Recorded before the includes are copied
- **WHEN** the wizard creates a worktree in a repository with a `.worktreeinclude` file
- **THEN** the worktree is recorded before any include file is copied, and a failed copy leaves it recorded

#### Scenario: Two processes record at once
- **WHEN** `vantage here` records a recent repository while the console records another recent repository and a created worktree
- **THEN** all three records are present on the next read

#### Scenario: Existing state is kept
- **WHEN** Vantage starts with recent repositories and created worktrees saved by an earlier version
- **THEN** the recent repositories are still listed first in the wizard and the created worktrees still decide the branch-deletion rules

#### Scenario: Old description files are ignored
- **WHEN** the state folder holds a `descriptions` folder or a `sessions.json` written by an earlier version
- **THEN** Vantage neither reads nor deletes them
