# Spec Delta

## ADDED Requirements

### Requirement: The worktrees table title counts rows and names the selection
The worktrees view's table title SHALL count the rows shown, name the filter when one is set, and name the selected row's folder.

#### Scenario: No filter
- **WHEN** twelve worktrees are listed with no filter and Northwind.Orders-fix-123 is selected
- **THEN** the table title reads 12 worktrees, selected: Northwind.Orders-fix-123

#### Scenario: With a filter
- **WHEN** a filter of "orp" matches one of twelve rows, the folder Northwind.Orders-orphaned
- **THEN** the table title reads 1 of 12 worktrees match "orp", selected: Northwind.Orders-orphaned

### Requirement: The worktrees header and help match the sessions view
The worktrees view's header SHALL show the Vantage version, the Claude Code CLI version and the repository roots as the sessions view's does; its help SHALL explain each of its columns; each of its key hints SHALL show the key's help line as a tooltip while the mouse pointer rests on it, and each of its column headers SHALL show what the column shows.

#### Scenario: Worktrees help
- **WHEN** the user presses the help key in the worktrees view
- **THEN** the help names every column of the worktrees table with its meaning, including what each health word means

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on the hint for p in the worktrees view
- **THEN** a tooltip shows the help line for p, saying it prunes stale registrations in the row's repository

#### Scenario: Hover over a column header
- **WHEN** the mouse pointer rests on the HEALTH header
- **THEN** a tooltip names the column and explains what each health word means
