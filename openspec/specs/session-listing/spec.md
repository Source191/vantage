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
For each session Vantage SHALL derive the repository, the checked-out branch and the folder it runs in, and SHALL show the folder relative to its root as the folder-actions spec describes; a session in a linked git worktree is recognisable because its folder differs from its repository's, so the row carries no separate worktree marker.

#### Scenario: Session in a sibling worktree
- **WHEN** a session's working directory is the linked worktree C:\Users\me\source\worktrees\Northwind.Orders-fix-123 of Northwind.Orders
- **THEN** the row shows Northwind.Orders as the repository, the worktree's branch, and Northwind.Orders-fix-123 as the folder

#### Scenario: Session outside any repository
- **WHEN** a session's working directory is not inside a git repository
- **THEN** the row shows the folder name as the repository, leaves the branch blank, and shows the folder relative to its root

#### Scenario: Session whose folder no longer exists
- **WHEN** a session's working directory has been deleted
- **THEN** the listing still succeeds, and the row shows the folder name as the repository and leaves the branch blank

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
Vantage SHALL refresh the list automatically at least every five seconds and on demand, SHALL keep the selected session selected across a refresh when it still exists, and SHALL stay consistent when one refresh is still running as the next begins.

#### Scenario: Selected session survives refresh
- **WHEN** the user has selected a session and a refresh occurs
- **THEN** the same session remains selected even if its row position changed

#### Scenario: Selected session disappears
- **WHEN** the selected session is no longer in the listing after a refresh
- **THEN** the selection moves to the nearest remaining row

#### Scenario: Overlapping refreshes
- **WHEN** a refresh takes longer than the refresh interval and a second refresh starts before it finishes
- **THEN** the view shows a complete listing from one of the two refreshes and neither refresh fails

### Requirement: Rows can be filtered
The view SHALL filter rows by a text typed after the filter key, matching case-insensitively against name, repository and branch.

#### Scenario: Filter by repository
- **WHEN** the user types a filter of "templates"
- **THEN** only rows whose name, repository or branch contains "templates" remain visible

### Requirement: The screen is a header, a table and a footer
The sessions view SHALL show a header with context on the left, key hints in the middle with each key coloured differently from its description, and the Vantage name on the right; a bordered table whose title counts the rows shown, names the filter when one is set and names the selected session; a highlighted bar on the selected row; and a footer crumb naming the view with a status line beneath it.

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

### Requirement: Waiting is visible
The sessions view SHALL show a spinner with the words "Listing sessions" in the middle of the table until the first listing arrives, and SHALL show a spinner beside the footer's working text while an action started from the view runs; neither spinner SHALL appear for the timed refreshes.

#### Scenario: First listing pending
- **WHEN** Vantage starts and the CLI listing has not yet returned
- **THEN** the table area shows a spinner and "Listing sessions", which disappear when the first rows arrive

#### Scenario: Action running
- **WHEN** the user starts a session from the wizard
- **THEN** the footer shows a spinner before "Starting name" until the start reports its outcome

#### Scenario: First listing fails
- **WHEN** the first CLI listing fails
- **THEN** the spinner is replaced by the failure banner

### Requirement: The header shows the versions and the roots
The header's context SHALL show the Vantage version followed by the memory Vantage's own process uses in brackets, the Claude Code CLI version, the repository roots and the worktree root, and SHALL NOT show the machine name or the Claude executable's path. A local build SHALL show its prerelease label without the build timestamp, and a Claude version that cannot be read SHALL show as unknown. A Claude version older than the minimum Vantage supports SHALL show that minimum and ask for Claude Code to be updated; the view SHALL work as usual. The roots and the worktree root SHALL be the settings saved now, so a save from the settings dialog shows at once.

#### Scenario: Released versions
- **WHEN** Vantage 0.5.0 runs using 29 MB with Claude Code 2.1.310 and the root C:\Users\me\source\repos
- **THEN** the header reads Vantage: 0.5.0 (29 MB), Claude: 2.1.310, Roots: C:\Users\me\source\repos and Worktree root: C:\Users\me\source\worktrees above the session counts

#### Scenario: Local build
- **WHEN** the installed tool's version is 0.5.0-local.20261025T101500 and Vantage uses 29 MB
- **THEN** the header reads Vantage: 0.5.0-local (29 MB)

#### Scenario: An update is available
- **WHEN** Vantage 0.5.0 uses 29 MB and 0.6.0 is the newest release
- **THEN** the header reads Vantage: 0.5.0 (29 MB; 0.6.0 available, U to update)

#### Scenario: Claude version unreadable
- **WHEN** the Claude Code CLI's version command fails or prints something without a version
- **THEN** the header reads Claude: unknown and the view works as usual

#### Scenario: Claude version below the minimum
- **WHEN** the minimum is 2.1.309 and the Claude Code CLI reports 2.1.302
- **THEN** the header reads Claude: 2.1.302 (below 2.1.309, update Claude Code) and the view works as usual

#### Scenario: Claude version at the minimum
- **WHEN** the minimum is 2.1.309 and the Claude Code CLI reports 2.1.309
- **THEN** the header reads Claude: 2.1.309 with no warning

#### Scenario: Default worktree root
- **WHEN** Vantage starts with no settings file
- **THEN** the header shows C:\Users\me\source\worktrees as the worktree root on both views

### Requirement: Help explains the columns
The help key SHALL show, beneath the keys, one line for each column of the current view saying what it shows, and the path of the Claude Code executable Vantage drives.

#### Scenario: Sessions help
- **WHEN** the user presses the help key in the sessions view
- **THEN** the help names every column of the sessions table with its meaning, and the Claude executable's path

### Requirement: Key hints explain themselves on hover
Each key hint in the header SHALL show the key's help line as a tooltip while the mouse pointer rests on it, using the same words as the help key.

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on the hint for x in the sessions view
- **THEN** a tooltip shows the help line for x, saying it stops the session

### Requirement: Column headers explain themselves on hover
A table column header SHALL show what the column shows as a tooltip while the mouse pointer rests on it, using the same words as the help key, and SHALL show nothing while the pointer rests on a row.

#### Scenario: Hover over a column header
- **WHEN** the mouse pointer rests on the STATE header in the sessions view
- **THEN** a tooltip names the column and lists the state words with their colours

#### Scenario: Hover over a row
- **WHEN** the mouse pointer rests on a row of the table
- **THEN** no column tooltip is shown

### Requirement: Rows show the session's memory
The sessions table SHALL have a MEM column showing the memory used by a running session's process tree, the session's process and every process it started, in whole megabytes below 1,000 MB and in gigabytes to one decimal place from there, refreshed with the rows. A session without a process SHALL show a dash. The help and the column's tooltip SHALL say what the column shows.

#### Scenario: A running session
- **WHEN** the session fix-123 runs with its process using 296 MB and the MCP servers it started using 607 MB between them
- **THEN** its MEM cell reads 903 MB

#### Scenario: A large session
- **WHEN** a session's tree uses 1,234 MB
- **THEN** its MEM cell reads 1.2 GB

#### Scenario: A stopped session
- **WHEN** a background session is listed as stopped, so it has no process
- **THEN** its MEM cell reads -

#### Scenario: The process has gone between the listing and the measurement
- **WHEN** a listed session's process ends before its tree is measured
- **THEN** its MEM cell reads - and the row is otherwise unchanged

#### Scenario: The help names the column
- **WHEN** the help is shown on the sessions view
- **THEN** it lists MEM beneath TAB with its meaning, and the MEM header shows the same words on hover

### Requirement: A key shows what the memory is made of
The `m` key SHALL open a dialog titled with the session's name that lists Claude Code itself and then one line per program the session's process started directly, each with the memory of that program's own tree, largest first, and a total with the number of processes in the tree. The dialog is a snapshot taken when it opens, closes with Close or Esc, and SHALL show a program's label only, never its command line. On a session with no process the footer SHALL say there is nothing to measure.

#### Scenario: A session with MCP servers
- **WHEN** `m` is pressed on fix-123, whose process uses 296 MB and which started cmd /c npx -y @azure-devops/mcp m-kopa (100 MB in 5 processes), uvx mcp-grafana@latest (30 MB in 7 processes) and a pwsh shell running a build (78 MB in 3 processes)
- **THEN** the dialog is titled Memory: fix-123 and lists Claude Code 296 MB, @azure-devops/mcp 100 MB, pwsh 78 MB and mcp-grafana 30 MB, then Total 504 MB in 16 processes

#### Scenario: Two servers from the same package
- **WHEN** a session started @playwright/mcp twice, once with --extension
- **THEN** the dialog shows two @playwright/mcp lines, each with its own memory

#### Scenario: A label never reveals a secret
- **WHEN** a server was started with a command line that carries an API key
- **THEN** the dialog shows the package name alone

#### Scenario: A stopped session
- **WHEN** `m` is pressed on a stopped session
- **THEN** no dialog opens and the footer says there is nothing to measure for a session with no process

#### Scenario: The key is listed
- **WHEN** the sessions view's help is shown
- **THEN** `m` is listed as the memory breakdown key; it has no hint in the header, which is full until the key bar replaces it

### Requirement: Memory is measured honestly
A process's memory SHALL be its private working set, the figure Task Manager's Memory column shows, so pages shared between processes are counted once. A process SHALL count towards a tree only when it started after its parent, so a reused process identifier never attaches an unrelated program to a session. A process that refuses to be measured SHALL count as zero and the rest of the tree SHALL still be measured.

#### Scenario: Shared pages are not multiplied
- **WHEN** thirty node processes in a session's tree map the same runtime
- **THEN** the session's figure is the sum of their private working sets, not of their working sets

#### Scenario: A reused identifier
- **WHEN** a long-running program's recorded parent identifier now belongs to a session's process that started after the program did
- **THEN** the program is not counted in that session's tree or its breakdown

#### Scenario: A protected process in the tree
- **WHEN** one process in a session's tree refuses the memory query
- **THEN** the session's figure is the sum of the others
