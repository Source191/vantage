# Spec Delta

## ADDED Requirements

### Requirement: One WORKTREE column names the worktree
The worktrees table SHALL show the repository and the folder in one WORKTREE column, as the repository, › and the folder's part after the repository (the slug of a folder named `<repository>-<slug>`, else the folder's label), elastic in width and elided in the middle of the repository name when too long. BRANCH SHALL stay its own column. The filter SHALL still match the repository, the folder, the branch and the health.

#### Scenario: A worktree Vantage made
- **WHEN** Northwind.Billing has the worktree C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf on branch invoice-pdf
- **THEN** its WORKTREE cell reads Northwind.Billing › invoice-pdf and its BRANCH cell reads invoice-pdf

#### Scenario: A leftover folder
- **WHEN** a broken leftover C:\Users\me\source\worktrees\Northwind.Sales-republish once belonged to Northwind.Sales
- **THEN** its WORKTREE cell reads Northwind.Sales › republish

#### Scenario: A folder that does not follow the naming
- **WHEN** Northwind.Orders has a hand-made worktree at C:\Users\me\source\worktrees\spike on branch spike
- **THEN** its WORKTREE cell reads Northwind.Orders › spike

#### Scenario: The help names the column
- **WHEN** the help is shown on the worktrees view
- **THEN** it lists WORKTREE with its meaning in place of REPOSITORY and FOLDER, and the header shows the same words on hover

## MODIFIED Requirements

### Requirement: Worktrees are listed across all roots
The worktrees view SHALL list every linked worktree of every git repository found directly under the configured roots, wherever the worktree's folder is (under the worktree root, beside the repository or elsewhere), using git's machine-readable worktree listing, each as a row whose WORKTREE cell names the repository and the folder and whose BRANCH cell names the branch.

#### Scenario: Two repositories with worktrees
- **WHEN** two repositories under the roots each have one linked worktree
- **THEN** the view shows two rows, each with a WORKTREE cell reading its repository › its folder's slug, and its branch in BRANCH

#### Scenario: Old and new placements together
- **WHEN** Northwind.Orders has one worktree beside it from an earlier version and one under C:\Users\me\source\worktrees
- **THEN** the view shows both rows, each reading Northwind.Orders › and its slug
