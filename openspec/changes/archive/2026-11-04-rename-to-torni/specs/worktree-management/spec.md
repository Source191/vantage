## MODIFIED Requirements

### Requirement: Branch deletion follows ownership and merge state
After removing a worktree, Torni SHALL delete its branch automatically only when Torni created that branch and it is fully merged into the remote default branch, and SHALL otherwise ask before deleting. A worktree whose head is detached, or whose head git did not name, has no branch of its own: it SHALL be removed with no branch deleted and no question asked, and its folder's name SHALL never be taken for a branch.

#### Scenario: Vantage-created merged branch
- **WHEN** the removed worktree's branch was created by Torni and is merged
- **THEN** the branch is deleted without asking

#### Scenario: Hand-made branch
- **WHEN** the removed worktree's branch was not created by Torni
- **THEN** Torni asks whether to delete the branch and keeps it unless the user agrees

#### Scenario: Detached head
- **WHEN** the user removes a clean worktree at C:\Users\me\source\repos\Northwind.Orders-fix-123 that git lists on a detached head, and a branch named Northwind.Orders-fix-123 exists in the repository
- **THEN** the worktree is removed, no branch is deleted, no question about a branch is asked, and the status line says the worktree was removed

#### Scenario: Head git could not name
- **WHEN** the user removes a clean worktree whose head git did not report
- **THEN** the worktree is removed and no branch is asked about or deleted

### Requirement: Enter on a worktree row goes to its session by identity
Enter on a worktree row with a running session SHALL do what Enter does on that session's row on the Sessions tab, found by its session id so two sessions sharing a name cannot be confused: open the session in its own Windows Terminal tab, or switch to that tab when one is open, or say in the status line why it cannot. The Worktrees tab SHALL stay shown. With no session in the worktree, or one the sessions listing does not hold, the status line SHALL say so.

#### Scenario: Two sessions share a name
- **WHEN** two background sessions are both named fix-tests and one runs in the selected worktree
- **THEN** Enter opens or switches to the tab of the one whose working directory is that worktree

#### Scenario: The session's tab is open
- **WHEN** a Torni-opened tab for the worktree's background session is alive and the user presses Enter on the worktree row
- **THEN** Windows Terminal switches to that tab, no new tab opens, and the Worktrees tab stays shown in Torni

#### Scenario: The session has no tab
- **WHEN** the worktree's background session has no live tab and the user presses Enter on the worktree row
- **THEN** a new tab opens attached to that session, as Enter on the Sessions tab opens one

#### Scenario: A terminal session
- **WHEN** the session in the worktree is a plain claude running in its own terminal
- **THEN** nothing opens and the status line says, as on the Sessions tab, that it runs in its own terminal

#### Scenario: No session
- **WHEN** no session runs in the selected worktree and the user presses Enter
- **THEN** nothing opens and the status line says no session runs in that worktree

#### Scenario: Session gone from the listing
- **WHEN** the worktree's row names a session that the latest sessions listing no longer holds
- **THEN** nothing opens and the status line says the session is not listed any more

### Requirement: Deleting a leftover folder never follows links
When Torni deletes a leftover folder from disc, every junction or symbolic link inside it SHALL be removed as a link, without descending into it or changing anything it points at, and the rest of the folder SHALL then be deleted completely, including read-only files.

#### Scenario: Leftover folder holding a junction
- **WHEN** the user force-removes an orphan folder whose node_modules holds a junction to a folder outside it, and that outside folder holds a read-only file
- **THEN** the orphan folder is gone, and the outside folder and its file still exist and the file is still read-only

#### Scenario: Leftover folder with read-only git files
- **WHEN** the user removes a broken folder whose object files git left read-only
- **THEN** the folder is deleted completely

### Requirement: Deleting a leftover folder never deletes a root
When Torni deletes a leftover folder from disc, it SHALL refuse a path that is not fully qualified, or that is a drive root or a network share root, before changing anything. The status line SHALL report the refusal as a failure naming the path.

#### Scenario: A drive root reaches the deletion
- **WHEN** a leftover folder's path is C:\
- **THEN** nothing on the drive is deleted and the status line reports a failure naming C:\

#### Scenario: A share root or a relative path reaches the deletion
- **WHEN** a leftover folder's path is \\server\share\ or Northwind.Orders-fix
- **THEN** nothing is deleted and the status line reports a failure naming that path

#### Scenario: An ordinary leftover folder with a trailing separator
- **WHEN** the user removes an orphan folder whose path is written C:\Users\me\source\worktrees\Northwind.Orders-fix\
- **THEN** the folder is deleted as before

### Requirement: One WORKTREE column names the worktree
The worktrees table SHALL show the repository and the folder in one WORKTREE column, as the repository, › and the folder's part after the repository (the slug of a folder named `<repository>-<slug>`, else the folder's label), elastic in width and elided in the middle of the repository name when too long, followed by HEALTH, LAST COMMIT and SESSION. LAST COMMIT SHALL be right-aligned, as AGE is on the Sessions tab, so its durations line up on their last character.

#### Scenario: A worktree Vantage made
- **WHEN** Northwind.Billing has the worktree C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf on branch invoice-pdf
- **THEN** its WORKTREE cell reads Northwind.Billing › invoice-pdf, and the pane reads Branch invoice-pdf

#### Scenario: A leftover folder
- **WHEN** a broken leftover C:\Users\me\source\worktrees\Northwind.Sales-republish once belonged to Northwind.Sales
- **THEN** its WORKTREE cell reads Northwind.Sales › republish, its HEALTH cell reads ✗ broken and its row is red

#### Scenario: A folder that does not follow the naming
- **WHEN** Northwind.Orders has a hand-made worktree at C:\Users\me\source\worktrees\spike on branch spike
- **THEN** its WORKTREE cell reads Northwind.Orders › spike

#### Scenario: Filter by health
- **WHEN** the user filters by broken
- **THEN** the broken rows stay in the table, each with its HEALTH cell reading ✗ broken

#### Scenario: The help names the column
- **WHEN** the help is shown on the worktrees view
- **THEN** it lists WORKTREE with its meaning, and the header shows the same words on hover

#### Scenario: Last commits line up at the right
- **WHEN** Northwind.Billing-invoice-pdf last committed five minutes ago, Northwind.Orders-fix-123 three days and four hours ago, and an empty leftover folder has no commit
- **THEN** the LAST COMMIT cells 5m, 3d4h and - each end at the column's right edge, and the WORKTREE cells start at its left edge

### Requirement: A worktree of a repository outside the roots is elsewhere
A folder directly under a root or under the worktree root whose .git file points at a folder that exists, and that no repository under the roots lists, SHALL be shown with health elsewhere, and its row SHALL name the repository the pointer leads to: the one holding the registration, a bare repository included, or the git directory itself when the pointer names no registration. A relative pointer SHALL be read from the .git file's own folder.

#### Scenario: Directly under a repository root
- **WHEN** a folder directly under C:\Users\me\source\repos has a .git file naming the existing registration D:\other\Northwind.Ledger\.git\worktrees\spike, and D:\other is not a root
- **THEN** the view shows it with health elsewhere and its repository as Northwind.Ledger

#### Scenario: A worktree of a bare repository
- **WHEN** a folder under C:\Users\me\source\worktrees has a .git file naming the existing registration D:\bare\Northwind.Ledger.git\worktrees\spike
- **THEN** the view shows it with health elsewhere and its repository as Northwind.Ledger.git, and force-remove is refused

#### Scenario: A checkout with a separate git directory
- **WHEN** a folder directly under C:\Users\me\source\repos has a .git file naming the existing git directory D:\gitdirs\Northwind.Ledger, which holds no worktrees segment
- **THEN** the view shows it with health elsewhere, not orphan, and force-remove is refused

#### Scenario: A worktree made with relative paths
- **WHEN** a folder under C:\Users\me\source\worktrees has a .git file whose gitdir line is ../../other/Northwind.Ledger/.git/worktrees/spike, and that registration exists relative to the folder
- **THEN** the view shows it with health elsewhere and its repository as Northwind.Ledger, whatever folder Torni was started from

### Requirement: A leftover folder is deleted only inside the roots
Before Torni deletes a leftover folder from disc, it SHALL refuse a path that is not under a repository root or the worktree root, changing nothing, and the status line SHALL say the folder is outside the roots and name it. Force-remove SHALL NOT be offered for this refusal.

#### Scenario: A registration that points outside the roots
- **WHEN** a repository under C:\Users\me\source\repos registers a worktree at D:\Northwind.Archive, git lists it as prunable, and the user presses remove or force-remove on its row
- **THEN** D:\Northwind.Archive is not deleted and the status line says the folder is outside the roots, naming D:\Northwind.Archive

#### Scenario: A leftover inside the roots
- **WHEN** the user removes an empty leftover folder C:\Users\me\source\repos\Northwind.Orders.worktrees
- **THEN** the folder is deleted as before
