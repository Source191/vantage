## MODIFIED Requirements

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
