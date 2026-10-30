# Spec Delta

## ADDED Requirements

### Requirement: The header summarises sessions by state
The header's Sessions line SHALL show the number of sessions listed, followed in brackets by a count for each state other than needs input, SHALL omit the brackets when every session needs input, and SHALL read none when no session is listed.

#### Scenario: Mixed states
- **WHEN** nine sessions are listed, four of them stopped and the rest needing input
- **THEN** the Sessions line reads 9 (4 stopped)

#### Scenario: Every session needs input
- **WHEN** six sessions are listed and every one needs input
- **THEN** the Sessions line reads 6, with no brackets, and the Need input line reads 6

#### Scenario: No sessions
- **WHEN** no session is listed
- **THEN** the Sessions line reads none

### Requirement: Long names never hide columns
Both table views SHALL cap the NAME, REPOSITORY, FOLDER and BRANCH columns, ending a cut cell with an ellipsis while the row keeps the full value for every key that uses it, and SHALL scroll the table sideways with the Left and Right keys, so every column can be reached on a 120-column terminal. The key hints in the header SHALL show every key within 120 columns and every key's words within 126 columns.

#### Scenario: Long repository name in the sessions view
- **WHEN** a session runs in a repository whose name is 50 characters long and the terminal is 120 columns wide
- **THEN** the REPOSITORY cell ends with an ellipsis, and pressing Right scrolls the table until the PR, AGE and TAB columns are visible

#### Scenario: Long folder in the worktrees view
- **WHEN** a worktree's folder label is 60 characters long and the terminal is 120 columns wide
- **THEN** the FOLDER cell ends with an ellipsis, and pressing Right scrolls the table until the SESSION and HEALTH columns are visible

#### Scenario: Copy a capped folder
- **WHEN** the user presses the copy key on a row whose FOLDER cell ends with an ellipsis
- **THEN** the clipboard holds the full folder path

#### Scenario: Third column of key hints
- **WHEN** the terminal is 126 columns wide
- **THEN** the words beside the c, e, v and p keys are visible in the header of both views

#### Scenario: Third column at 120 columns
- **WHEN** the terminal is 120 columns wide
- **THEN** the c, e, v and p keys are visible in the header, under the logo, and their tooltips still explain them

## MODIFIED Requirements

### Requirement: Help explains the columns
The help key SHALL show, beneath the keys, one line for each column of the current view saying what it shows, and the path of the Claude Code executable Vantage drives. Every line of the help SHALL be visible or reachable by scrolling on a terminal of 36 rows or more, and the whole text SHALL be visible without scrolling on a terminal of 40 rows.

#### Scenario: Sessions help
- **WHEN** the user presses the help key in the sessions view
- **THEN** the help names every column of the sessions table with its meaning, and the Claude executable's path

#### Scenario: Help on a short terminal
- **WHEN** the user presses the help key on a 120 x 36 terminal
- **THEN** the help shows its first lines, the arrow keys and PageDown scroll it, and its last line, the Claude Code executable's path, is reached

#### Scenario: Help on a 40-row terminal
- **WHEN** the user presses the help key on a 120 x 40 terminal
- **THEN** the TAB column's line and the Claude Code executable's path are visible without scrolling

### Requirement: Key hints explain themselves on hover
Each key hint in the header SHALL show a tooltip while the mouse pointer rests on the key or on its words, saying in one sentence what the key does and when to use it, drawn in its own colours apart from the header's text.

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on the x key, or on the word Stop beside it, in the sessions view
- **THEN** a tooltip in colours distinct from the header says that x stops the session, which stays listed as stopped and can be resumed

#### Scenario: Every key has a tip
- **WHEN** the help lists a key of either view
- **THEN** that key has a tooltip sentence of at most 80 characters

### Requirement: Column headers explain themselves on hover
A table column header SHALL show what the column shows as a tooltip while the mouse pointer rests on it, using the same words as the help key and keeping the help's line breaks so the tooltip never runs off the screen, and SHALL show nothing while the pointer rests on a row.

#### Scenario: Hover over a column header
- **WHEN** the mouse pointer rests on the STATE header in the sessions view
- **THEN** a tooltip names the column and lists the state words with their colours

#### Scenario: Hover over the PR header
- **WHEN** the mouse pointer rests on the PR header on a 120-column terminal
- **THEN** the tooltip shows the column's meaning on two lines, each within the terminal's width

#### Scenario: Hover over a row
- **WHEN** the mouse pointer rests on a row of the table
- **THEN** no column tooltip is shown
