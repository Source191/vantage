## MODIFIED Requirements

### Requirement: Delete offers to remove the session's worktree
Deleting a session SHALL first end its Vantage-opened tab and wait for the tab's shell to exit. When the session's working directory is a worktree Vantage created, delete SHALL then offer to remove that worktree under the worktree-management safety rules. When that worktree's repository no longer exists, or git cannot inspect the worktree, the session SHALL still be deleted, the worktree SHALL be kept untouched and the status line SHALL say why, in Vantage's words.

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

#### Scenario: The worktree's repository has gone
- **WHEN** the user deletes a session in the Vantage-created worktree C:\Users\me\source\worktrees\Northwind.Orders-fix-123 and its repository C:\Users\me\source\repos\Northwind.Orders has been deleted since the worktree was created
- **THEN** the session is deleted, git is not asked anything, the folder and its files are kept, and the status line says the worktree was kept because its repository C:\Users\me\source\repos\Northwind.Orders no longer exists and that the Worktrees tab removes it

#### Scenario: Git fails while the worktree is inspected
- **WHEN** the user deletes a session in a Vantage-created worktree and git fails while listing the repository's worktrees
- **THEN** the session is deleted, nothing is removed or deleted, and the status line says the worktree was kept because git could not inspect it, giving git's reason, with no "Failed:" line

### Requirement: Empty terminal sessions can be closed
For an interactive session that has never been sent a message, the delete key SHALL end its claude process and then end the Windows Terminal tab's shell with exit code 0 so the tab closes; the close-empty key SHALL do the same for every such session after a confirmation naming them; a session not inside a Windows Terminal tab is only ended. When claude is itself the tab's root process, it SHALL be ended with exit code 0 so the tab closes, and SHALL be counted among the tabs closed.

#### Scenario: Close every empty tab
- **WHEN** two interactive sessions have never been sent a message and the user presses the close-empty key and confirms
- **THEN** both claude processes end, both tabs close, and the status line names them

#### Scenario: Nothing to close
- **WHEN** no interactive session is without messages
- **THEN** the status line says so and nothing is ended

#### Scenario: A tab whose process id was reused
- **WHEN** the user confirms close-empty while one of the named sessions' process ids now belongs to a process that started after that session did
- **THEN** that process and its tab are left alone, the other sessions are closed, and the status line names the one left alone

#### Scenario: Claude is the tab's root process
- **WHEN** the user confirms close-empty on an interactive session with no messages whose claude process was started directly by Windows Terminal, as a profile's command line
- **THEN** that claude is ended with exit code 0 and nothing else is ended, its tab closes, and the status line counts it among the tabs closed
