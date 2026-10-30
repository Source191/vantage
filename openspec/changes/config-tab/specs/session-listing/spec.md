# Spec Delta

## MODIFIED Requirements

### Requirement: Long names never hide columns
The sessions table SHALL give WHERE the width its other columns leave and SHALL fit every column on a 120-column terminal; a WHERE value too long for its width SHALL lose the middle of its repository name, marked with an ellipsis, and keep its slug and branch whole. NAME SHALL keep its cap and ellipsis, the row SHALL keep the full values for every key that uses them, and Shift+Left and Shift+Right SHALL scroll the table on a narrower terminal, Left and Right being the tab keys. The key bar SHALL show every key that applies within 120 columns, with its words.

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
- **THEN** the c, e, v and p keys and their words are in the Folder group of the key bar on both tabs

#### Scenario: Third column at 120 columns
- **WHEN** the terminal is 120 columns wide
- **THEN** the c, e, v and p keys and their words are in the Folder group of the key bar, and their tooltips still explain them

#### Scenario: Wide terminal
- **WHEN** the terminal is 127 columns or wider
- **THEN** the WHERE cell for Northwind.Integration.CosmosDb.ChangeFeedMonitoring › fix-123 is shown whole


#### Scenario: Scroll on a narrow terminal
- **WHEN** the terminal is 100 columns wide and the user presses Shift+Right on the Sessions tab
- **THEN** the table scrolls one column to the right and the Sessions tab stays shown
