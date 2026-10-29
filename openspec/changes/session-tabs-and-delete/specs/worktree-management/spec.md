# Spec Delta

## MODIFIED Requirements

### Requirement: Removal refuses to destroy work
Removal SHALL be refused when the worktree has changed or untracked files, unpushed commits, a lock, or a live session inside it, and the refusal SHALL name the reason. When git cannot delete the folder because a tab or program still has it as its working directory, the removal SHALL report the folder as in use, naming it, and SHALL leave the registration for a later remove or prune.

#### Scenario: Unpushed commits
- **WHEN** the user presses remove on a worktree with unpushed commits
- **THEN** nothing is removed and the footer names the branch and the number of unpushed commits

#### Scenario: Clean worktree
- **WHEN** the user presses remove on a worktree with no changes, no unpushed commits, no lock and no session
- **THEN** the worktree is removed from git and from disc

#### Scenario: Folder in use by a shell tab
- **WHEN** the user presses remove on a clean worktree while a PowerShell tab opened from the worktrees view sits in it
- **THEN** the footer says the folder is still in use by a tab or program and names it, and after that tab is closed a second remove deletes what is left and prunes the registration
