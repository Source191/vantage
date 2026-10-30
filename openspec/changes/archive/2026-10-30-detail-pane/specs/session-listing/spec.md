# Spec Delta

## MODIFIED Requirements

### Requirement: The screen is a header, a table and a footer
The sessions view SHALL be the Sessions tab of the console: under the settings rows and the tabs' row, a table framed by the tab, whose title, at the right end of the tabs' row, counts the rows shown, names the filter when one is set and names the selected session; a highlighted bar on the selected row; the detail pane beneath the table; and beneath that, above the key bar, a status line shown only while there is a message or an action is running, the table taking its row otherwise. A message SHALL clear when the selection moves or the filter changes. The status line SHALL NOT name the view or repeat the counts beside the tabs.

#### Scenario: Table title
- **WHEN** seven sessions are listed with no filter and fix-123 is selected
- **THEN** the table title reads 7 sessions, selected: fix-123

#### Scenario: Table title with a filter
- **WHEN** a filter of "templates" matches two of seven rows and templates-sync is selected
- **THEN** the table title reads 2 of 7 sessions match "templates", selected: templates-sync

#### Scenario: One session
- **WHEN** one session is listed with no filter and it is selected
- **THEN** the table title reads 1 session, selected: and the session's name

#### Scenario: Nothing selected
- **WHEN** no sessions are listed
- **THEN** the table title reads 0 sessions and names no selection

#### Scenario: Nothing to say
- **WHEN** two sessions need input and no key has reported anything
- **THEN** the counts beside the tabs read ◐ 2 need input, there is no status line, and the table has its row

#### Scenario: A message
- **WHEN** the user presses c on fix-123
- **THEN** a status line under the pane says the folder was copied, and it goes when the user presses Down

### Requirement: Column headers explain themselves on hover
A table column header SHALL show what the column shows as a tooltip while the mouse pointer rests on it, using the same words as the help key and keeping the help's line breaks so the tooltip never runs off the screen, drawn just under the header over the table's rows and never over the detail pane, and SHALL show nothing while the pointer rests on a row.

#### Scenario: Hover over a column header
- **WHEN** the mouse pointer rests on the STATE header in the sessions view
- **THEN** a tooltip names the column and lists the state words with their colours

#### Scenario: Hover over the PR header
- **WHEN** the mouse pointer rests on the PR header on a 120-column terminal
- **THEN** the tooltip shows the column's meaning on two lines, each within the terminal's width, on the rows just under the header, and the pane is not covered

#### Scenario: Hover over a row
- **WHEN** the mouse pointer rests on a row of the table
- **THEN** no column tooltip is shown

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

#### Scenario: Branch differs from the slug
- **WHEN** a session runs in the worktree Northwind.Orders-fix-123 on branch hotfix/orders
- **THEN** its REPOSITORY cell reads Northwind.Orders, and the pane reads Worktree fix-123 and Branch hotfix/orders

#### Scenario: Main checkout
- **WHEN** a session runs in C:\Users\me\source\repos\Northwind.Orders on branch main
- **THEN** its REPOSITORY cell reads Northwind.Orders, and the pane reads Worktree main checkout and Branch main

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

#### Scenario: Third column of key hints
- **WHEN** the terminal is 126 columns wide
- **THEN** the c, e, v and p keys and their words are in the Folder group of the key bar on both tabs

#### Scenario: Wide terminal
- **WHEN** the terminal is 127 columns or wider
- **THEN** the REPOSITORY cell for Northwind.Integration.CosmosDb.ChangeFeedMonitoring is shown whole

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
