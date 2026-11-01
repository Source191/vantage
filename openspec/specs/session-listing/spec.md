# session-listing Specification

## Purpose
Shows every Claude Code session on the machine in one table with its state, so the user can see at a glance which sessions need attention and where each one is working.

## Requirements

### Requirement: Sessions come from the supported CLI listing
Vantage SHALL obtain the session list by running the Claude Code CLI's JSON listing of active and completed sessions, and SHALL NOT read transcript files or the sessions folder of the Claude configuration directory. A listing that has not returned within the child program time limit SHALL be ended and reported as a failed listing. When the listing command exits with an error, the failure banner SHALL show the CLI's own words and no JSON parser's message.

#### Scenario: Listing succeeds
- **WHEN** the CLI listing returns three sessions
- **THEN** the sessions view shows three rows, each with the session's name, state and working directory

#### Scenario: Listing fails
- **WHEN** the CLI cannot be run or returns invalid JSON
- **THEN** the view keeps the rows it last showed, displays an error banner naming the failure, and does not exit

#### Scenario: Listing hangs
- **WHEN** the CLI listing has not exited after the time limit
- **THEN** the listing's process tree is ended, the view keeps the rows it last showed, the banner says the listing failed and names claude and the limit, and the next timed refresh runs as usual

#### Scenario: The CLI refuses the listing
- **WHEN** the listing command exits with an error and prints "Couldn't connect to the background service" to its error output and nothing to its output
- **THEN** the banner reads listing failed: Couldn't connect to the background service, with no mention of JSON tokens, the view keeps the rows it last showed, and the next refresh runs as usual

### Requirement: Session state is shown in six display states
Vantage SHALL map each session's process presence, status, waitingFor and state fields to exactly one of: working, needs input, idle, completed, failed, stopped. A row without a process identifier is stopped or failed, never working. A background session with a process identifier and no status yet is starting up, and SHALL show the word starting in the working state.

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

#### Scenario: Background session starting up
- **WHEN** a background session reports a process identifier, no status and state blocked
- **THEN** its row shows starting in the working colour and glyph, never "/blocked"

#### Scenario: Unknown combination
- **WHEN** a session reports a status or state Vantage does not recognise
- **THEN** its row shows the raw value rather than guessing a display state

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

### Requirement: Interactive sessions are shown with limited actions
Sessions whose kind is interactive SHALL appear in the list with their kind marked by the hollow block in the TAB column, and SHALL NOT accept open-in-tab (Enter), stop or rename actions; the adopt key moves them under the supervisor and, for one never sent a message, the delete key closes its tab (session-attach spec).

#### Scenario: Action on an interactive row
- **WHEN** the user presses Enter on an interactive session's row
- **THEN** the status line explains that the session runs in its own terminal and names the adopt key, and nothing else happens

### Requirement: Rows are ordered by attention needed
The view SHALL order rows by display state: needs input first, then working, idle, completed, failed, stopped, and within a state by most recently started first.

#### Scenario: Mixed states
- **WHEN** the list contains one working, one needs-input and one stopped session
- **THEN** the needs-input row is first and the stopped row is last

### Requirement: The list refreshes without losing the selection
Vantage SHALL refresh the list automatically at least every five seconds and on demand, SHALL keep the selected session selected across a refresh when it still exists, and SHALL stay consistent when one refresh is still running as the next begins: a refresh SHALL never show rows older than the ones already shown, and rows that arrive while a dialog is open SHALL be kept when it closes. The timed refresh SHALL NOT start a listing while one is running; the refresh after an action SHALL always run.

#### Scenario: Selected session survives refresh
- **WHEN** the user has selected a session and a refresh occurs
- **THEN** the same session remains selected even if its row position changed

#### Scenario: Selected session disappears
- **WHEN** the selected session is no longer in the listing after a refresh
- **THEN** the selection moves to the nearest remaining row

#### Scenario: Overlapping refreshes
- **WHEN** a refresh takes longer than the refresh interval and the refresh after an action starts before it finishes
- **THEN** the view shows the complete listing from the refresh that started last, whichever finishes first, and neither refresh fails

#### Scenario: A listing started before a stop lands after it
- **WHEN** the timed listing is running, the user presses x on fix-123, the stop completes and its own listing returns without fix-123, and then the earlier listing returns with fix-123 still running
- **THEN** fix-123 stays gone, no bell rings and no last prompted time is recorded for the reordering

#### Scenario: Rows loaded while a dialog is open
- **WHEN** the user presses m on a session, a timed refresh returns new rows while the memory dialog is open, and the user closes it
- **THEN** the table shows the new rows, not the ones from before the dialog opened

#### Scenario: The timer waits for a running listing
- **WHEN** a listing takes eight seconds
- **THEN** the five-second timer starts no second `claude agents` process until it returns, and the refresh after a stop or delete still starts its own listing

### Requirement: Rows can be filtered
The view SHALL filter rows by a text typed after the filter key, matching case-insensitively against name, repository and branch.

#### Scenario: Filter by repository
- **WHEN** the user types a filter of "templates"
- **THEN** only rows whose name, repository or branch contains "templates" remain visible

### Requirement: The screen is a header, a table and a detail pane
The sessions view SHALL be the Sessions tab of the console: under the settings rows and the tabs' row, a table framed by the tab, whose title, at the right end of the tabs' row, counts the rows shown, names the filter when one is set and names the selected session; a highlighted bar on the selected row; and the detail pane beneath the table.

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
- **THEN** a status line under the settings row, above the tabs, says the folder was copied, and it goes when the user presses Down

### Requirement: States are colour coded
Each state SHALL be shown as a glyph and its word in its own colour, each state with a glyph of its own: ◐ needs input amber, ● working cyan, ○ idle white, ✓ completed green, ✗ failed red, ■ stopped grey and ? unknown grey, an unknown state's word being the status and state Claude Code listed; and each row SHALL be drawn in its state's colour, none dimmed further.

#### Scenario: State colours
- **WHEN** the table shows a working row, a needs-input row and a stopped row
- **THEN** the working state reads ● working in cyan, the needs-input state ◐ needs input in amber, and the stopped row is grey

#### Scenario: Stopped and unknown apart
- **WHEN** the table shows a stopped row and a row whose listed status and state Vantage does not know, busy/paused
- **THEN** both rows are grey, one STATE cell reads ■ stopped and the other ? busy/paused

### Requirement: The header counts sessions needing input
The counts beside the tabs SHALL show how many sessions are in the needs-input state, first and in amber, whenever any is.

#### Scenario: Two sessions need input
- **WHEN** two sessions are in the needs-input state
- **THEN** the counts row starts ◐ 2 need input in amber

### Requirement: Waiting is visible
The sessions view SHALL show a spinner with the words "Listing sessions" in the middle of the table until the first listing arrives, and SHALL show a spinner beside the status line's working text while an action started from the view runs; neither spinner SHALL appear for the timed refreshes. Typing a filter, Backspace or Escape before the first listing SHALL NOT end the waiting spinner.

#### Scenario: First listing pending
- **WHEN** Vantage starts and the CLI listing has not yet returned
- **THEN** the table area shows a spinner and "Listing sessions", which disappear when the first rows arrive

#### Scenario: A filter typed before the first listing
- **WHEN** the CLI listing has not yet returned and the user presses / and types t
- **THEN** the table area still shows the spinner and "Listing sessions", and the title shows the filter being typed

#### Scenario: Escape before the first listing
- **WHEN** the CLI listing has not yet returned and the user presses Esc
- **THEN** the table area still shows the spinner and "Listing sessions"

#### Scenario: Action running
- **WHEN** the user starts a session from the wizard
- **THEN** the status line shows a spinner before "Starting name" until the start reports its outcome

#### Scenario: First listing fails
- **WHEN** the first CLI listing fails
- **THEN** the spinner is replaced by the failure banner

### Requirement: The header shows the versions and the roots
The settings rows SHALL show the Vantage version followed by the memory Vantage's own process uses in brackets, the Claude Code CLI version, the repository roots and the worktree root, and SHALL NOT show the machine name or the Claude executable's path. The roots and the worktree root SHALL be the settings saved now, so a save from the settings dialog shows at once.

#### Scenario: Released versions
- **WHEN** Vantage 0.5.0 runs using 29 MB with Claude Code 2.1.310 and the root C:\Users\me\source\repos
- **THEN** the first settings row reads Vantage 0.5.0 (29 MB) · Claude 2.1.310 and the second Repository roots C:\Users\me\source\repos · Worktree root C:\Users\me\source\worktrees

#### Scenario: Local build
- **WHEN** the installed tool's version is 0.5.0-local.20261025T101500 and Vantage uses 29 MB
- **THEN** the first settings row starts Vantage 0.5.0-local (29 MB)

#### Scenario: An update is available
- **WHEN** Vantage 0.5.0 uses 29 MB and 0.6.0 is the newest release
- **THEN** the first settings row starts Vantage 0.5.0 (29 MB; 0.6.0 available, U to update)

#### Scenario: Claude version unreadable
- **WHEN** the Claude Code CLI's version command fails or prints something without a version
- **THEN** the first settings row reads Claude unknown and the view works as usual

#### Scenario: Claude version below the minimum
- **WHEN** the minimum is 2.1.309 and the Claude Code CLI reports 2.1.302
- **THEN** the first settings row reads Claude 2.1.302 (below 2.1.309, update Claude Code) and the view works as usual

#### Scenario: Claude version at the minimum
- **WHEN** the minimum is 2.1.309 and the Claude Code CLI reports 2.1.309
- **THEN** the first settings row reads Claude 2.1.309 with no warning

#### Scenario: Default worktree root
- **WHEN** Vantage starts with no settings file
- **THEN** the second settings row ends Worktree root C:\Users\me\source\worktrees on both tabs

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
Each item of the key bar SHALL show a tooltip while the mouse pointer rests on it, saying in one sentence what the key does and when to use it, drawn in its own colours apart from the bar's text.

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on x Stop in the key bar of the Sessions tab
- **THEN** a tooltip in colours distinct from the bar says that x stops the session, which stays listed as stopped and can be resumed

#### Scenario: Every key has a tip
- **WHEN** the help lists a key of either view
- **THEN** that key has a tooltip sentence of at most 80 characters

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

### Requirement: A key shows what the memory is made of
The `m` key SHALL open a dialog titled with the session's name that lists Claude Code itself and then one line per program the session's process started directly, each with the memory of that program's own tree, largest first, and a total with the number of processes in the tree. The dialog is a snapshot taken when it opens, closes with Close or Esc, and SHALL show a program's label only, never its command line. On a session with no process the status line SHALL say there is nothing to measure.

#### Scenario: A session with MCP servers
- **WHEN** `m` is pressed on fix-123, whose process uses 296 MB and which started cmd /c npx -y @azure-devops/mcp m-kopa (100 MB in 5 processes), uvx mcp-grafana@latest (30 MB in 7 processes) and a pwsh shell running a build (78 MB in 3 processes)
- **THEN** the dialog is titled Memory: fix-123 and lists Claude Code 296 MB, @azure-devops/mcp 100 MB, pwsh 78 MB and mcp-grafana 30 MB, then Total 504 MB in 16 processes

#### Scenario: Two servers from the same package
- **WHEN** a session started @playwright/mcp twice, once with --extension
- **THEN** the dialog shows two @playwright/mcp lines, each with its own memory

#### Scenario: A label never reveals a secret
- **WHEN** a server was started with a command line that carries an API key
- **THEN** the dialog shows the package name alone

#### Scenario: An option's value is never a label
- **WHEN** a session started uvx --from git+https://x-access-token:secret@github.com/northwind/mcp northwind-mcp, and another started npx --registry https://me:secret@registry.northwind.example -y @northwind/mcp
- **THEN** the dialog shows northwind-mcp and @northwind/mcp, and no line shows any part of either URL

#### Scenario: A stopped session
- **WHEN** `m` is pressed on a stopped session
- **THEN** no dialog opens and the status line says there is nothing to measure for a session with no process

#### Scenario: The key is listed
- **WHEN** a running session is selected on the Sessions tab
- **THEN** m Memory is in the key bar's first row, and the help lists `m` as the memory breakdown key

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

### Requirement: The header summarises sessions by state
The table's title beside the tabs SHALL count the sessions listed, and the counts beside the tabs SHALL show one glyph, count and state word per state present, needs input first; with no session listed the title SHALL read 0 sessions and no counts SHALL show. Sessions starting up SHALL be counted with the working ones under the word working.

#### Scenario: Mixed states
- **WHEN** nine sessions are listed, four of them stopped and the rest needing input
- **THEN** the title starts 9 sessions and the counts read ◐ 5 need input · ■ 4 stopped

#### Scenario: Every session needs input
- **WHEN** six sessions are listed and every one needs input
- **THEN** the title starts 6 sessions and the counts read ◐ 6 need input

#### Scenario: No sessions
- **WHEN** no session is listed
- **THEN** the title reads 0 sessions and no counts show

#### Scenario: Starting and working together
- **WHEN** one session is starting up and one is working, the starting one first in the table
- **THEN** the counts read ● 2 working

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

### Requirement: The TAB column is a swatch
The sessions table's TAB column SHALL show a two-cell block in the colour of the Vantage tab showing the session while that tab is alive, in the shade that tab shows from Vantage's own tab, with a cell of space before the next column, on the highlighted row as on every other; SHALL show a hollow grey block for a session running as plain claude in its own terminal; and nothing otherwise. The help and the column's tooltip SHALL say what each means.

#### Scenario: A session open in a Vantage tab
- **WHEN** fix-123 is shown in a Vantage tab coloured yellow
- **THEN** its TAB cell shows a two-cell yellow block, then a space before the state

#### Scenario: A terminal session
- **WHEN** study was started as plain claude in a tab of its own
- **THEN** its TAB cell is a hollow block drawn in grey

#### Scenario: No tab
- **WHEN** a background session has no live Vantage tab
- **THEN** its TAB cell is blank

#### Scenario: The help names the blocks
- **WHEN** the help is shown on the sessions view
- **THEN** TAB's line says a block in the tab's colour marks a session a Vantage tab shows and a hollow block a plain claude in its own terminal

#### Scenario: The highlighted row keeps the colour
- **WHEN** fix-123, shown in a Vantage tab coloured red, is the selected row
- **THEN** its TAB block is the same shade of red as on any other row, not the highlight's colour

### Requirement: The TAB block matches the tab as it looks from Vantage
The TAB column's block SHALL be drawn in the shade Windows Terminal shows for that session's tab while another tab is selected, because Vantage's own tab is the selected one whenever the table is read. The shade SHALL be the same on the selected row as on any other, and SHALL come from one rule applied to the tab's colour.

#### Scenario: A red tab
- **WHEN** a session's Vantage tab is red and the Vantage tab is selected
- **THEN** the session's TAB block is the faded red its tab shows in the tab strip, not the bright red the tab shows when selected

#### Scenario: The selected row
- **WHEN** the row of a session with a blue tab is selected
- **THEN** its TAB block is the same faded blue on the selection bar

#### Scenario: Every palette colour
- **WHEN** each colour of the tab palette is turned into its shade
- **THEN** each matches the colour measured from Windows Terminal's tab strip for that colour, within one step in each channel

### Requirement: Automated runs are not sessions
An interactive entry in the CLI listing that has no status SHALL be treated as an automated run (a `claude` started through the Agent SDK or `claude -p`), not a session: it SHALL NOT be a row, SHALL NOT be counted in the table's title and SHALL take no keys. The decision SHALL use only the listing's fields. Every other entry SHALL be listed as before.

#### Scenario: A plugin's background review
- **WHEN** the listing holds the background session fix-123 and an interactive entry with a process, fix-123's folder, no name and no status
- **THEN** the table shows one row, fix-123, and its title starts 1 session

#### Scenario: A named automated run
- **WHEN** the listing holds an interactive entry with a process, the name tmp-9d and no status, as `claude -p` gives
- **THEN** no row shows for it

#### Scenario: A real interactive session
- **WHEN** the listing holds an interactive entry with a process and the status idle
- **THEN** it shows as an interactive session row, as before

#### Scenario: A real interactive session that has just started
- **WHEN** a `claude` started in a terminal is listed in the moment before it has a status, and has the status idle at the next refresh
- **THEN** it shows no row for that one listing and shows as an interactive session row from the next

#### Scenario: A stopped background session
- **WHEN** the listing holds a background entry with no process and no status
- **THEN** it shows as a stopped row, as before

### Requirement: The counts show the automated runs
While any automated run is listed, the counts beside the tabs SHALL end with a grey ◇, how many runs, the word automated and the memory of their process trees together, measured and written as the detail pane's Memory line measures and writes a session's. With none listed nothing SHALL show for them.

#### Scenario: One run beside two sessions
- **WHEN** one session is working, one is stopped and one automated run's tree uses 216 MB
- **THEN** the counts read ● 1 working · ■ 1 stopped · ◇ 1 automated 216 MB, the last in grey

#### Scenario: Two runs
- **WHEN** two automated runs use 215 MB and 216 MB
- **THEN** the counts end ◇ 2 automated 431 MB

#### Scenario: A run whose process has gone
- **WHEN** an automated run's process ends between the listing and the measurement
- **THEN** the run is still counted and adds nothing to the memory

#### Scenario: No runs
- **WHEN** no automated run is listed
- **THEN** the counts show only the sessions' states, and with no session listed no counts show

#### Scenario: The listing fails
- **WHEN** a refresh fails after a listing that held one automated run
- **THEN** the counts keep ◇ 1 automated with its memory, as the rows keep what they last showed

### Requirement: The status line speaks only when there is something to say
Under the settings row, above the tabs, the sessions view SHALL show a status line only while there is a message or an action is running, the tabs moving back up otherwise. A message SHALL clear when the selection moves or the filter changes, Escape clearing a filter included. The status line SHALL NOT name the view or repeat the counts beside the tabs.

#### Scenario: A message cleared by the filter
- **WHEN** the status line says a folder was copied and the user starts a filter with / and types t
- **THEN** the status line goes and the tabs move back up a row

#### Scenario: A message cleared by Escape
- **WHEN** a filter of "temp" is set, the status line says a folder was copied and the user presses Esc
- **THEN** the filter is cleared, the status line goes and the tabs move back up a row

### Requirement: Versions are shown without noise
A local build SHALL show its prerelease label without the build timestamp, and a Claude version that cannot be read SHALL show as unknown.

#### Scenario: A local preview build
- **WHEN** the installed tool's version is 0.18.0-preview.12-local.20261031083825 and Vantage uses 35 MB
- **THEN** the first settings row starts Vantage 0.18.0-preview.12-local (35 MB)

### Requirement: An old Claude Code is flagged but still works
A Claude version older than the minimum Vantage supports SHALL show that minimum and ask for Claude Code to be updated, and the view SHALL work as usual.

#### Scenario: An older minor version
- **WHEN** the minimum is 2.1.309 and the Claude Code CLI reports 2.0.99
- **THEN** the first settings row reads Claude 2.0.99 (below 2.1.309, update Claude Code) and the sessions are listed as usual

### Requirement: A session id has the CLI's shape
A session id read from the CLI's listing or a state file SHALL be made of ASCII letters, digits and hyphens and SHALL start with a letter or digit; a listing row or a state entry whose id has any other shape SHALL be skipped as a malformed row is, so no id is ever passed to Claude Code where it could be read as an option or used to name a file outside the transcripts folder.

#### Scenario: An ordinary id
- **WHEN** the listing has a row whose sessionId is 81cdb0f9-8c3f-4c9b-b478-ca0a5ebf85c6
- **THEN** the row is shown

#### Scenario: An id that looks like an option
- **WHEN** the listing has a row whose sessionId is --dangerously-skip-permissions
- **THEN** that row is not shown, the other rows are, and no Claude Code command is ever given that id

#### Scenario: An id that names another folder
- **WHEN** the listing has a row whose sessionId is ..\..\Northwind
- **THEN** that row is not shown and no file is looked for under that name
