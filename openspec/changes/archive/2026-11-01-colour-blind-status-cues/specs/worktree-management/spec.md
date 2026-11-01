# Spec Delta

## ADDED Requirements

### Requirement: A worktree's health reads without colour
The worktrees table SHALL show each row's health in a HEALTH column as a glyph and the health's word, in the row's colour: ✓ ok, ◆ locked, △ orphan, ✗ broken, □ empty, ↗ elsewhere; no two healths SHALL share a glyph. The help and the HEALTH header's tooltip SHALL list every glyph with its word.

#### Scenario: Every health in the table
- **WHEN** the table lists one worktree of each health
- **THEN** their HEALTH cells read ✓ ok, ◆ locked, △ orphan, ✗ broken, □ empty and ↗ elsewhere

#### Scenario: Two amber healths
- **WHEN** a locked worktree and an elsewhere worktree are listed
- **THEN** both rows are amber, one HEALTH cell reads ◆ locked and the other ↗ elsewhere

#### Scenario: The help lists the glyphs
- **WHEN** the help is shown on the worktrees view
- **THEN** its HEALTH line lists ✓ ok, ◆ locked, △ orphan, ✗ broken, □ empty and ↗ elsewhere, and the header's tooltip shows the same words

## MODIFIED Requirements

### Requirement: Locked worktrees are marked
A worktree that git reports as locked SHALL show health locked, its HEALTH cell reading ◆ locked and its row in amber, and the detail pane's Health line SHALL show the lock reason when git provides one.

#### Scenario: Locked by another tool
- **WHEN** git reports a worktree as locked and it is selected
- **THEN** its row is amber, its HEALTH cell reads ◆ locked, and the pane reads locked and the lock reason

### Requirement: One WORKTREE column names the worktree
The worktrees table SHALL show the repository and the folder in one WORKTREE column, as the repository, › and the folder's part after the repository (the slug of a folder named `<repository>-<slug>`, else the folder's label), elastic in width and elided in the middle of the repository name when too long, followed by HEALTH, LAST COMMIT and SESSION.

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

### Requirement: A worktree's details are in the pane
The branch, the pull request, the changed files and the unpushed commits SHALL be shown in the detail pane, and the health's reason in full on its Health line; each row SHALL keep its health's colour beside its HEALTH cell. The filter SHALL still match the repository, the folder, the branch and the health.

#### Scenario: Filter by a branch no cell shows
- **WHEN** Northwind.Orders has the worktree C:\Users\me\source\worktrees\spike on branch feature/refund-retry and the user filters by refund
- **THEN** the row stays in the table though no cell shows the branch, and the pane reads Branch feature/refund-retry
