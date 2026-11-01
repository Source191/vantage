# Spec Delta

## ADDED Requirements

### Requirement: A tab's shell is found only through the session's real ancestors
When adopt or close-empty looks for the Windows Terminal tab that holds a session's process, it SHALL walk up only through parents that started before their child; a parent that started after its child holds a reused process id, and the walk SHALL stop there and treat the session as not in a tab.

#### Scenario: The launching shell has exited and its id is reused
- **WHEN** the user confirms close-empty on an interactive session whose launching shell has exited and whose recorded parent id now belongs to a PowerShell that started later as the root of another Windows Terminal tab
- **THEN** the session's own process is ended and the other tab and its shell are left open

#### Scenario: A session in its own tab
- **WHEN** the user adopts an interactive session whose process runs under a PowerShell that started before it, directly under Windows Terminal
- **THEN** that PowerShell is ended cleanly and its tab closes, as before

## MODIFIED Requirements

### Requirement: Delete clears a worktree whose .git has gone
When the session's Vantage-created worktree no longer has its `.git`, delete SHALL treat its folder as a leftover: a folder that holds no files SHALL be deleted and its stale registration pruned from the repository, and a folder that still holds files SHALL be kept, with the status line saying how many untracked files would be lost and that force-remove on the Worktrees tab deletes it. A worktree that git does not list SHALL be judged by its folder on disc, never taken to be gone.

#### Scenario: The worktree is no longer listed
- **WHEN** the user deletes a session whose Vantage-created worktree has lost its `.git` file and whose folder holds no files
- **THEN** the folder is gone and the repository no longer lists the worktree

#### Scenario: The folder still holds work
- **WHEN** the user deletes a session whose Vantage-created worktree has lost its `.git` file and whose folder still holds twelve files
- **THEN** the session is deleted, the folder and its files are kept, and the status line says twelve untracked files would be lost and that force-remove on the Worktrees tab deletes it

#### Scenario: Git spells the worktree's path differently
- **WHEN** the user deletes a session whose Vantage-created worktree is listed by git under a different spelling of its path, such as the real folder behind a junction, and the folder holds uncommitted work
- **THEN** the session is deleted and the folder and its work are kept

### Requirement: Delete offers to remove the session's worktree
Deleting a session SHALL first end its Vantage-opened tab and wait for the tab's shell to exit. When the session's working directory is a worktree Vantage created, delete SHALL then offer to remove that worktree under the worktree-management safety rules.

#### Scenario: Clean worktree
- **WHEN** the user deletes a session in a Vantage-created worktree that has no changes and no unpushed commits
- **THEN** Vantage offers removal and, if accepted, removes the worktree and its branch

#### Scenario: Worktree with changes
- **WHEN** the worktree has uncommitted changes
- **THEN** the session is deleted but the worktree is kept and the row explains why

#### Scenario: Tab closed before the worktree goes
- **WHEN** the user deletes a session that was started and opened in its own tab
- **THEN** the tab closes before the worktree is removed, the worktree is removed, and the status line says the session was deleted and its tab closed

#### Scenario: Worktree without its .git
- **WHEN** the user deletes a session whose Vantage-created worktree no longer has a `.git` file and whose folder holds no files
- **THEN** the session is deleted, the folder is deleted, the stale registration is pruned from the repository, and the status line says so

#### Scenario: Folder still in use
- **WHEN** the user deletes a session while a PowerShell tab opened from the worktrees view sits in its worktree
- **THEN** the session is deleted, the status line says the worktree folder is still in use by a tab or program and names it, and pressing remove on it in the worktrees view after closing that tab deletes what is left
