# Spec Delta

## ADDED Requirements

### Requirement: The TAB column is a swatch
The sessions table's TAB column SHALL be one cell wide and show a solid block in the colour of the Vantage tab showing the session while that tab is alive, a hollow grey block for a session running as plain claude in its own terminal, and nothing otherwise. The help and the column's tooltip SHALL say what each block means.

#### Scenario: A session open in a Vantage tab
- **WHEN** fix-123 is shown in a Vantage tab coloured yellow
- **THEN** its TAB cell is a solid block drawn in yellow

#### Scenario: A terminal session
- **WHEN** study was started as plain claude in a tab of its own
- **THEN** its TAB cell is a hollow block drawn in grey

#### Scenario: No tab
- **WHEN** a background session has no live Vantage tab
- **THEN** its TAB cell is blank

#### Scenario: The help names the blocks
- **WHEN** the help is shown on the sessions view
- **THEN** TAB's line says a solid block is an open Vantage tab in that tab's colour and a hollow block a plain claude in its own terminal

## MODIFIED Requirements

### Requirement: Rows show repository, branch and worktree
For each session Vantage SHALL derive the repository, the checked-out branch and the folder it runs in, and SHALL show them in one WHERE column: the repository, then › and the folder's part after the repository (the slug of a worktree named `<repository>-<slug>`, or the path inside the repository) when the folder is not the main checkout, then the branch in brackets when it differs from that part. A folder outside any repository SHALL show as the folder-actions label alone. The filter SHALL still match the name, the repository and the branch.

#### Scenario: Session in a sibling worktree
- **WHEN** a session's working directory is the linked worktree C:\Users\me\source\worktrees\Northwind.Orders-fix-123 of Northwind.Orders, on branch fix-123
- **THEN** its WHERE cell reads Northwind.Orders › fix-123

#### Scenario: Session outside any repository
- **WHEN** a session's working directory is not inside a git repository
- **THEN** its WHERE cell reads the folder's label alone, such as ~\scratch\notes, with no branch

#### Scenario: Session whose folder no longer exists
- **WHEN** a session's working directory has been deleted
- **THEN** the listing still succeeds, and its WHERE cell reads the folder's label alone

#### Scenario: Branch differs from the slug
- **WHEN** a session runs in the worktree Northwind.Orders-fix-123 on branch hotfix/orders
- **THEN** its WHERE cell reads Northwind.Orders › fix-123 (hotfix/orders)

#### Scenario: Main checkout
- **WHEN** a session runs in C:\Users\me\source\repos\Northwind.Orders on branch main
- **THEN** its WHERE cell reads Northwind.Orders (main)

#### Scenario: Folder inside the repository
- **WHEN** a session runs in C:\Users\me\source\repos\Northwind.Orders\src\Api on branch main
- **THEN** its WHERE cell reads Northwind.Orders › src\Api (main)

### Requirement: Long names never hide columns
The sessions table SHALL give WHERE the width its other columns leave and SHALL fit every column on a 120-column terminal; a WHERE value too long for its width SHALL lose the middle of its repository name, marked with an ellipsis, and keep its slug and branch whole. NAME SHALL keep its cap and ellipsis, the row SHALL keep the full values for every key that uses them, and Left and Right SHALL still scroll the table on a narrower terminal. The key hints in the header SHALL show every key within 120 columns and every key's words within 126 columns.

#### Scenario: Long repository name in the sessions view
- **WHEN** a session runs in the worktree Northwind.Integration.CosmosDb.ChangeFeedMonitoring-fix-123 and the terminal is 120 columns wide
- **THEN** every column of the sessions table is on screen without scrolling, and the WHERE cell reads the repository name with its middle replaced by an ellipsis, then › fix-123 whole

#### Scenario: Long folder in the worktrees view
- **WHEN** a worktree's folder label is 60 characters long and the terminal is 120 columns wide
- **THEN** every column of the worktrees table is on screen without scrolling, and the WORKTREE cell keeps its slug whole with the repository name elided in the middle

#### Scenario: Copy a capped folder
- **WHEN** the user presses the copy key on a row whose WHERE cell holds an ellipsis
- **THEN** the clipboard holds the full folder path

#### Scenario: Third column of key hints
- **WHEN** the terminal is 126 columns wide
- **THEN** the words beside the c, e, v and p keys are visible in the header of both views

#### Scenario: Third column at 120 columns
- **WHEN** the terminal is 120 columns wide
- **THEN** the c, e, v and p keys are visible in the header, under the logo, and their tooltips still explain them

#### Scenario: Wide terminal
- **WHEN** the terminal is 127 columns or wider
- **THEN** the WHERE cell for Northwind.Integration.CosmosDb.ChangeFeedMonitoring › fix-123 is shown whole

### Requirement: Interactive sessions are shown with limited actions
Sessions whose kind is interactive SHALL appear in the list with their kind marked by the hollow block in the TAB column, and SHALL NOT accept attach, open-in-tab, stop or rename actions; the adopt key moves them under the supervisor and, for one never sent a message, the delete key closes its tab (session-attach spec).

#### Scenario: Action on an interactive row
- **WHEN** the user presses attach on an interactive session's row
- **THEN** the footer explains that the session runs in its own terminal and names the adopt key, and nothing else happens

### Requirement: States are colour coded
Each state SHALL be shown as a glyph and its word in its own colour: ◐ needs input amber, ● working cyan, ○ idle white, ✓ completed green, ✗ failed red, ■ stopped and unknown grey; and rows that are completed, failed or stopped SHALL be dimmed.

#### Scenario: State colours
- **WHEN** the table shows a working row, a needs-input row and a stopped row
- **THEN** the working state reads ● working in cyan, the needs-input state ◐ needs input in amber, and the stopped row is dimmed grey
