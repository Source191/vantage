# Spec Delta

## MODIFIED Requirements

### Requirement: Rows show repository, branch and worktree
For each session Vantage SHALL derive the repository, the checked-out branch and the folder it runs in. The table SHALL show the repository's name in a REPOSITORY column, or the folder-actions label of a folder outside any repository; the worktree's place (its slug, or the path inside the repository, or the main checkout), the branch and the full folder SHALL be shown in the detail pane. The filter SHALL still match the name, the repository and the branch.

#### Scenario: Session in a sibling worktree
- **WHEN** a session's working directory is the linked worktree C:\Users\me\source\worktrees\Northwind.Orders-fix-123 of Northwind.Orders, on branch fix-123
- **THEN** its REPOSITORY cell reads Northwind.Orders, and the pane reads Worktree fix-123 and Branch fix-123

#### Scenario: Session outside any repository
- **WHEN** a session's working directory is not inside a git repository
- **THEN** its REPOSITORY cell reads the folder's label, such as ~\scratch\notes, and the pane reads Worktree not in a repository

#### Scenario: Session whose folder no longer exists
- **WHEN** a session's working directory has been deleted
- **THEN** the listing still succeeds, and its REPOSITORY cell reads the folder's label

#### Scenario: Folder inside the repository
- **WHEN** a session runs in C:\Users\me\source\repos\Northwind.Orders\src\Api on branch main
- **THEN** its REPOSITORY cell reads Northwind.Orders, and the pane reads Worktree src\Api and Branch main

#### Scenario: Filter by branch
- **WHEN** the user filters by hotfix and a session runs on the branch hotfix/orders
- **THEN** that session's row stays in the table though no cell shows its branch

### Requirement: Long names never hide columns
The sessions table SHALL give REPOSITORY the width its other columns leave and SHALL fit every column on a 120-column terminal; a REPOSITORY value too long for its width SHALL lose its middle, marked with an ellipsis. NAME SHALL keep its cap and ellipsis, the row SHALL keep the full values for every key that uses them, and < and > SHALL scroll the table on a narrower terminal, Left and Right being the tab keys. The key bar SHALL show every key that applies within 120 columns, with its words.

#### Scenario: Long repository name in the sessions view
- **WHEN** a session runs in a worktree of Northwind.Integration.CosmosDb.ChangeFeedMonitoring and the terminal is 120 columns wide
- **THEN** every column of the sessions table is on screen without scrolling, and the REPOSITORY cell reads Northwind.Integration.CosmosDb.ChangeFeedMonitoring whole

#### Scenario: Long folder in the worktrees view
- **WHEN** a worktree's folder label is 60 characters long and the terminal is 120 columns wide
- **THEN** every column of the worktrees table is on screen without scrolling, and the WORKTREE cell shows it whole

#### Scenario: Copy a capped folder
- **WHEN** the user presses the copy key on a row whose REPOSITORY cell holds an ellipsis
- **THEN** the clipboard holds the full folder path

#### Scenario: Third column at 120 columns
- **WHEN** the terminal is 120 columns wide
- **THEN** the c, e, v and p keys and their words are in the Folder group of the key bar, and their tooltips still explain them

#### Scenario: Scroll on a narrow terminal
- **WHEN** the terminal is 100 columns wide and the user presses > on the Sessions tab
- **THEN** the table scrolls one column to the right and the Sessions tab stays shown

### Requirement: Rows show the session's memory
The memory used by a running session's process tree, the session's process and every process it started, SHALL be shown in the detail pane's Memory line for the selected row, in whole megabytes below 1,000 MB and in gigabytes to one decimal place from there, refreshed with the rows. A session without a process SHALL read nothing to measure. The sessions table SHALL have no MEM column, and the help SHALL say the pane and the m key show memory.

#### Scenario: A running session
- **WHEN** the session fix-123 is selected with its process using 296 MB and the MCP servers it started using 607 MB between them
- **THEN** the pane's Memory line reads 903 MB

#### Scenario: A large session
- **WHEN** a selected session's tree uses 1,234 MB
- **THEN** the pane's Memory line reads 1.2 GB

#### Scenario: A stopped session
- **WHEN** a background session listed as stopped, so with no process, is selected
- **THEN** the pane's Memory line reads nothing to measure

#### Scenario: The process has gone between the listing and the measurement
- **WHEN** a selected session's process ends before its tree is measured
- **THEN** the pane's Memory line reads nothing to measure and the row is otherwise unchanged

#### Scenario: The help names the column
- **WHEN** the help is shown on the sessions view
- **THEN** it lists no MEM column, its Pane section explains the Memory line, and the m key's line says what m shows
