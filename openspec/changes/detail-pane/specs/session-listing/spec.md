# Spec Delta

## MODIFIED Requirements

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
- **THEN** it lists no MEM column, and the m key's line says the pane shows the figure and m what it is made of

### Requirement: Long names never hide columns
The sessions table SHALL give WHERE the width its other columns leave, 64 cells on a 120-column terminal, and SHALL fit every column there; a WHERE value too long for its width SHALL lose the middle of its repository name, marked with an ellipsis, and keep its slug and branch whole. NAME SHALL keep its cap and ellipsis, the row SHALL keep the full values for every key that uses them, and Left and Right SHALL still scroll the table on a narrower terminal. The key bar SHALL show every key that applies within 120 columns, with its words.

#### Scenario: Long repository name in the sessions view
- **WHEN** a session runs in the worktree Northwind.Integration.CosmosDb.ChangeFeedMonitoring-fix-123 and the terminal is 120 columns wide
- **THEN** every column of the sessions table is on screen without scrolling, and the WHERE cell reads Northwind.Integration.CosmosDb.ChangeFeedMonitoring › fix-123 whole

#### Scenario: Long folder in the worktrees view
- **WHEN** a worktree's folder label is 60 characters long and the terminal is 120 columns wide
- **THEN** every column of the worktrees table is on screen without scrolling, and the WORKTREE cell keeps its slug whole with the repository name elided in the middle

#### Scenario: Copy a capped folder
- **WHEN** the user presses the copy key on a row whose WHERE cell holds an ellipsis
- **THEN** the clipboard holds the full folder path

#### Scenario: Third column of key hints
- **WHEN** the terminal is 126 columns wide
- **THEN** the c, e, v and p keys and their words are in the Folder group of the key bar on both tabs

#### Scenario: Third column at 120 columns
- **WHEN** the terminal is 120 columns wide
- **THEN** the c, e, v and p keys and their words are in the Folder group of the key bar, and their tooltips still explain them

#### Scenario: Wide terminal
- **WHEN** the terminal is 127 columns or wider
- **THEN** the WHERE cell for Northwind.Integration.CosmosDb.ChangeFeedMonitoring › fix-123 is shown whole, as it is at 120

#### Scenario: A longer name at 120 columns
- **WHEN** a session runs in a worktree of a repository whose name is 70 characters long and the terminal is 120 columns wide
- **THEN** the WHERE cell keeps › and the slug whole and the repository name's middle is replaced by an ellipsis
