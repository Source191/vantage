# Spec Delta

## MODIFIED Requirements

### Requirement: One WORKTREE column names the worktree
The worktrees table SHALL show the repository and the folder in one WORKTREE column, as the repository, › and the folder's part after the repository (the slug of a folder named `<repository>-<slug>`, else the folder's label), elastic in width and elided in the middle of the repository name when too long, followed by LAST COMMIT and SESSION; the branch, the pull request, the changed files, the unpushed commits and the health SHALL be shown in the detail pane, and each row SHALL keep its health's colour. The filter SHALL still match the repository, the folder, the branch and the health.

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

### Requirement: Each row shows work at risk
For every registered worktree the view SHALL show the age of the last commit and the name of any session whose working directory is that worktree in its row, and the count of changed or untracked files and the count of commits not on any remote branch in the detail pane's Changes line when the row is selected.

#### Scenario: Dirty worktree
- **WHEN** a worktree has five changed files and two unpushed commits and is selected
- **THEN** the pane's Changes line reads 5 changed files · 2 unpushed commits, in amber

### Requirement: Locked worktrees are marked
A worktree that git reports as locked SHALL show health locked, its row in amber, and the detail pane's Health line SHALL show the lock reason when git provides one.

#### Scenario: Locked by another tool
- **WHEN** git reports a worktree as locked and it is selected
- **THEN** its row is amber and the pane reads locked and the lock reason

### Requirement: The worktrees header and help match the sessions view
The worktrees view SHALL be the Worktrees tab of the console, under the same settings line as the Sessions tab, with its own counts beside the tabs and its own key bar grouped Worktree, Folder and view; its status line SHALL show only messages, never a standing hint or the selected row's health, which the detail pane shows; its help SHALL explain each of its columns and each line of its detail pane; each item of its key bar SHALL show the key's tooltip while the mouse pointer rests on it, and each of its column headers SHALL show what the column shows.

#### Scenario: Worktrees help
- **WHEN** the user presses the help key on the Worktrees tab
- **THEN** the help names every column of the worktrees table with its meaning, and its Pane section names every line of the pane, including what each health word means

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on P Prune in the key bar of the Worktrees tab
- **THEN** a tooltip says it prunes stale registrations in the row's repository

#### Scenario: An unhealthy row selected
- **WHEN** an elsewhere worktree is selected and no key has reported anything
- **THEN** the pane's Health line gives the reason and there is no status line

#### Scenario: Hover over a column header
- **WHEN** the mouse pointer rests on the LAST COMMIT header
- **THEN** a tooltip names the column and explains it
