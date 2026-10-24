# session-listing Specification

## Purpose
Shows every Claude Code session on the machine in one table with its state, so the user can see at a glance which sessions need attention and where each one is working.

## Requirements

### Requirement: Sessions come from the supported CLI listing
Vantage SHALL obtain the session list by running the Claude Code CLI's JSON listing of active and completed sessions, and SHALL NOT read transcript files or the sessions folder of the Claude configuration directory.

#### Scenario: Listing succeeds
- **WHEN** the CLI listing returns three sessions
- **THEN** the sessions view shows three rows, each with the session's name, state and working directory

#### Scenario: Listing fails
- **WHEN** the CLI cannot be run or returns invalid JSON
- **THEN** the view keeps the rows it last showed, displays an error banner naming the failure, and does not exit

### Requirement: Session state is shown in six display states
Vantage SHALL map each session's process presence, status, waitingFor and state fields to exactly one of: working, needs input, idle, completed, failed, stopped. A row without a process identifier is stopped or failed, never working.

#### Scenario: Busy session
- **WHEN** a session reports status busy and state working
- **THEN** its row shows working

#### Scenario: Waiting on a permission prompt
- **WHEN** a session reports status waiting, waitingFor "permission prompt" and state blocked
- **THEN** its row shows needs input and the peek text names the permission prompt

#### Scenario: Idle session that has not started
- **WHEN** a session reports status idle and state blocked
- **THEN** its row shows needs input

#### Scenario: Finished session with a live process
- **WHEN** a session reports status idle and state done
- **THEN** its row shows completed

#### Scenario: Deliberately stopped session
- **WHEN** a session row has no process identifier and no status, and its state is done or blocked
- **THEN** its row shows stopped

#### Scenario: Unknown combination
- **WHEN** a session reports a status or state Vantage does not recognise
- **THEN** its row shows the raw value rather than guessing a display state

### Requirement: Rows show repository, branch and worktree
For each session Vantage SHALL derive the repository, the checked-out branch and whether the working directory is a linked git worktree.

#### Scenario: Session in a sibling worktree
- **WHEN** a session's working directory is a linked worktree of a repository
- **THEN** the row shows the parent repository's name, the worktree's branch, and marks the row as a worktree

#### Scenario: Session outside any repository
- **WHEN** a session's working directory is not inside a git repository
- **THEN** the row shows the folder name as the repository and leaves the branch blank

### Requirement: Descriptions are shown with their session
When Vantage holds a description for a session's ID, the listing SHALL show it in the row, truncated to the available width, and in full in the peek text; when it holds none the row shows nothing there.

#### Scenario: Description present
- **WHEN** a session was launched with a description
- **THEN** its row shows the description and the peek text shows it in full

### Requirement: Interactive sessions are shown with limited actions
Sessions whose kind is interactive SHALL appear in the list with their kind marked, and SHALL NOT accept attach, open-in-tab, stop or rename actions; the adopt key moves them under the supervisor and, for one never sent a message, the delete key closes its tab (session-attach spec).

#### Scenario: Action on an interactive row
- **WHEN** the user presses attach on an interactive session's row
- **THEN** the footer explains that the session runs in its own terminal and names the adopt key, and nothing else happens

### Requirement: Rows are ordered by attention needed
The view SHALL order rows by display state: needs input first, then working, idle, completed, failed, stopped, and within a state by most recently started first.

#### Scenario: Mixed states
- **WHEN** the list contains one working, one needs-input and one stopped session
- **THEN** the needs-input row is first and the stopped row is last

### Requirement: The list refreshes without losing the selection
Vantage SHALL refresh the list automatically at least every five seconds and on demand, and SHALL keep the selected session selected across a refresh when it still exists.

#### Scenario: Selected session survives refresh
- **WHEN** the user has selected a session and a refresh occurs
- **THEN** the same session remains selected even if its row position changed

#### Scenario: Selected session disappears
- **WHEN** the selected session is no longer in the listing after a refresh
- **THEN** the selection moves to the nearest remaining row

### Requirement: Rows can be filtered
The view SHALL filter rows by a text typed after the filter key, matching case-insensitively against name, repository and branch.

#### Scenario: Filter by repository
- **WHEN** the user types a filter of "templates"
- **THEN** only rows whose name, repository or branch contains "templates" remain visible

### Requirement: The screen is a header, a table and a footer
The sessions view SHALL show a header with context on the left, key hints in the middle with each key coloured differently from its description, and the Vantage name on the right; a bordered table whose title names the view, the filter and the row count; a highlighted bar on the selected row; and a footer crumb naming the view with a status line beneath it.

#### Scenario: Table title
- **WHEN** seven sessions are listed with no filter
- **THEN** the table title reads sessions(all)[7]

#### Scenario: Table title with a filter
- **WHEN** a filter of "templates" matches two rows
- **THEN** the table title reads sessions(templates)[2]

### Requirement: States are colour coded
Each state word SHALL be shown in its own colour: needs input amber, working cyan, idle white, completed green, failed red, stopped and unknown grey; and rows that are completed, failed or stopped SHALL be dimmed.

#### Scenario: State colours
- **WHEN** the table shows a working row, a needs-input row and a stopped row
- **THEN** the working state reads in cyan, the needs-input state in amber, and the stopped row is dimmed grey

### Requirement: The header counts sessions needing input
The header SHALL show how many sessions are in the needs-input state.

#### Scenario: Two sessions need input
- **WHEN** two sessions are in the needs-input state
- **THEN** the header shows a count of two
