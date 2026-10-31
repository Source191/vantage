# Spec Delta

## MODIFIED Requirements

### Requirement: Leftover folders are detected
The view SHALL also show folders that git no longer recognises: a folder under a repository's .claude\worktrees that the listing omits is an orphan; a folder directly under a root or under the worktree root whose .git file points at a worktree registration that no longer exists is broken; an empty folder named with a .worktrees suffix directly under a root is empty.

#### Scenario: Orphan folder
- **WHEN** a folder exists under .claude\worktrees but is absent from git's listing
- **THEN** the view shows it with health orphan

#### Scenario: Broken registration
- **WHEN** a sibling folder's .git file names a worktree registration whose repository no longer exists
- **THEN** the view shows it with health broken

#### Scenario: Broken registration under the worktree root
- **WHEN** a folder under C:\Users\me\source\worktrees has a .git file naming a worktree registration that no repository under the roots lists
- **THEN** the view shows it with health broken

#### Scenario: Empty leftover
- **WHEN** an empty folder named Northwind.Orders.worktrees sits directly under a root
- **THEN** the view shows it with health empty

#### Scenario: Worktree of a repository outside the roots
- **WHEN** a folder under C:\Users\me\source\worktrees has a .git file naming the registration D:\other\Northwind.Ledger\.git\worktrees\fix, that registration exists, D:\other\Northwind.Ledger\.git exists, and D:\other is not a root
- **THEN** the view shows it with health elsewhere, its repository as Northwind.Ledger, and the footer says its repository D:\other\Northwind.Ledger is not under the roots

### Requirement: Leftover folders can be deleted as plain folders
For rows with health orphan, broken or empty, the remove key SHALL delete the folder from disc without git, and for broken rows SHALL also prune the stale registration from the owning repository when that repository still exists.

#### Scenario: Delete empty leftover
- **WHEN** the user presses remove on an empty row
- **THEN** the folder is deleted and the row disappears

#### Scenario: Leftover folder with files
- **WHEN** the user presses remove on an orphan folder that still holds twelve files
- **THEN** removal is refused and the footer says twelve untracked files would be lost, and force-remove with the typed confirmation deletes it

### Requirement: The worktrees header and help match the sessions view
The worktrees view SHALL be the Worktrees tab of the console, under the same settings line as the Sessions tab, with its own counts beside the tabs and its own key bar grouped Worktree, Folder and view; its status line SHALL show only messages, never a standing hint or the selected row's health, which the detail pane shows.

#### Scenario: Worktrees help
- **WHEN** the user presses the help key on the Worktrees tab
- **THEN** the help names every column of the worktrees table with its meaning, and its Pane section names every line of the pane, including what each health word means

#### Scenario: Worktrees help on a short terminal
- **WHEN** the user presses the help key in the worktrees view on a 120 x 36 terminal
- **THEN** every line of the help is reachable with the arrow keys and PageDown

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on P Prune in the key bar of the Worktrees tab
- **THEN** a tooltip says it prunes stale registrations in the row's repository

#### Scenario: An unhealthy row selected
- **WHEN** an elsewhere worktree is selected and no key has reported anything
- **THEN** the pane's Health line gives the reason and there is no status line

#### Scenario: Hover over a column header
- **WHEN** the mouse pointer rests on the LAST COMMIT header
- **THEN** a tooltip names the column and explains it

#### Scenario: The worktree bar
- **WHEN** an ok worktree with a session inside it is selected
- **THEN** the first bar row reads d Remove, D Force remove, P Prune, s Start session, o Open shell and Enter Go to session, and the second c Copy path, e Open folder, v Solution, p Open PR, / Filter, ? Help, ! Report and q Quit

### Requirement: One WORKTREE column names the worktree
The worktrees table SHALL show the repository and the folder in one WORKTREE column, as the repository, › and the folder's part after the repository (the slug of a folder named `<repository>-<slug>`, else the folder's label), elastic in width and elided in the middle of the repository name when too long, followed by LAST COMMIT and SESSION.

#### Scenario: A worktree Vantage made
- **WHEN** Northwind.Billing has the worktree C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf on branch invoice-pdf
- **THEN** its WORKTREE cell reads Northwind.Billing › invoice-pdf, and the pane reads Branch invoice-pdf

#### Scenario: A leftover folder
- **WHEN** a broken leftover C:\Users\me\source\worktrees\Northwind.Sales-republish once belonged to Northwind.Sales
- **THEN** its WORKTREE cell reads Northwind.Sales › republish and its row is red

#### Scenario: A folder that does not follow the naming
- **WHEN** Northwind.Orders has a hand-made worktree at C:\Users\me\source\worktrees\spike on branch spike
- **THEN** its WORKTREE cell reads Northwind.Orders › spike

#### Scenario: Filter by health
- **WHEN** the user filters by broken
- **THEN** the broken rows stay in the table though no cell shows the health

#### Scenario: The help names the column
- **WHEN** the help is shown on the worktrees view
- **THEN** it lists WORKTREE with its meaning, and the header shows the same words on hover

## ADDED Requirements

### Requirement: A worktree of a repository outside the roots is elsewhere
A folder directly under a root or under the worktree root whose .git file points at a registration that still exists, in a repository that still has its .git folder but is not under the roots, SHALL be shown with health elsewhere, and its row SHALL name that repository.

#### Scenario: Directly under a repository root
- **WHEN** a folder directly under C:\Users\me\source\repos has a .git file naming the existing registration D:\other\Northwind.Ledger\.git\worktrees\spike, and D:\other is not a root
- **THEN** the view shows it with health elsewhere and its repository as Northwind.Ledger

### Requirement: Leftover work is protected from removal
An orphan or broken folder that still contains files SHALL be refused unless forced, because git no longer tracks that work, and a leftover folder with a live session inside SHALL be refused even when forced.

#### Scenario: A live session in a broken folder
- **WHEN** the user force-removes a broken folder that a live session is running in
- **THEN** removal is refused and the folder is kept

### Requirement: An elsewhere worktree is never deleted as a leftover
A row with health elsewhere is not a leftover and SHALL NOT be deleted as a plain folder.

#### Scenario: Remove on an elsewhere row
- **WHEN** the user presses remove on a row with health elsewhere
- **THEN** nothing is deleted and the footer says its repository is not under the roots

### Requirement: The worktrees view explains itself
The worktrees view's help SHALL explain each of its columns and each line of its detail pane; each item of its key bar SHALL show the key's tooltip while the mouse pointer rests on it, and each of its column headers SHALL show what the column shows.

#### Scenario: Hover over the SESSION header
- **WHEN** the mouse pointer rests on the SESSION header of the worktrees table
- **THEN** a tooltip names the column and explains it

### Requirement: A worktree's details are in the pane
The branch, the pull request, the changed files, the unpushed commits and the health SHALL be shown in the detail pane, and each row SHALL keep its health's colour. The filter SHALL still match the repository, the folder, the branch and the health.

#### Scenario: Filter by a branch no cell shows
- **WHEN** Northwind.Orders has the worktree C:\Users\me\source\worktrees\spike on branch feature/refund-retry and the user filters by refund
- **THEN** the row stays in the table though no cell shows the branch, and the pane reads Branch feature/refund-retry
