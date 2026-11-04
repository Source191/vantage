## RENAMED Requirements

- FROM: `### Requirement: The TAB block matches the tab as it looks from Vantage`
- TO: `### Requirement: The TAB block matches the tab as it looks from Torni`

## MODIFIED Requirements

### Requirement: Sessions come from the supported CLI listing
Torni SHALL obtain the session list by running the Claude Code CLI's JSON listing of active and completed sessions, and SHALL NOT read transcript files or the sessions folder of the Claude configuration directory. A listing that has not returned within the child program time limit SHALL be ended and reported as a failed listing. When the listing command exits with an error, the failure banner SHALL show the CLI's own words and no JSON parser's message.

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
Torni SHALL map each session's process presence, status, waitingFor and state fields to exactly one of: working, needs input, idle, completed, failed, stopped. A row without a process identifier is stopped or failed, never working. A background session with a process identifier and no status yet is starting up, and SHALL show the word starting in the working state.

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
- **WHEN** a session reports a status or state Torni does not recognise
- **THEN** its row shows the raw value rather than guessing a display state

### Requirement: Rows show repository, branch and worktree
For each session Torni SHALL derive the repository, the checked-out branch and the folder it runs in. The table SHALL show the repository's name in a REPOSITORY column, or the folder-actions label of a folder outside any repository; the worktree's place (its slug, or the path inside the repository, or the main checkout), the branch and the full folder SHALL be shown in the detail pane. The filter SHALL still match the name, the repository and the branch.

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

### Requirement: The list refreshes without losing the selection
Torni SHALL refresh the list automatically at least every five seconds and on demand, SHALL keep the selected session selected across a refresh when it still exists, and SHALL stay consistent when one refresh is still running as the next begins: a refresh SHALL never show rows older than the ones already shown, and rows that arrive while a dialog is open SHALL be kept when it closes. The timed refresh SHALL NOT start a listing while one is running; the refresh after an action SHALL always run.

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

### Requirement: States are colour coded
Each state SHALL be shown as a glyph and its word in its own colour, each state with a glyph of its own: ◐ needs input amber, ● working cyan, ○ idle white, ✓ completed green, ✗ failed red, ■ stopped grey and ? unknown grey, an unknown state's word being the status and state Claude Code listed; and each row SHALL be drawn in its state's colour, none dimmed further.

#### Scenario: State colours
- **WHEN** the table shows a working row, a needs-input row and a stopped row
- **THEN** the working state reads ● working in cyan, the needs-input state ◐ needs input in amber, and the stopped row is grey

#### Scenario: Stopped and unknown apart
- **WHEN** the table shows a stopped row and a row whose listed status and state Torni does not know, busy/paused
- **THEN** both rows are grey, one STATE cell reads ■ stopped and the other ? busy/paused

### Requirement: Waiting is visible
The sessions view SHALL show a spinner with the words "Listing sessions" in the middle of the table until the first listing arrives, and SHALL show a spinner beside the status line's working text while an action started from the view runs; neither spinner SHALL appear for the timed refreshes. Typing a filter, Backspace or Escape before the first listing SHALL NOT end the waiting spinner.

#### Scenario: First listing pending
- **WHEN** Torni starts and the CLI listing has not yet returned
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
The settings rows SHALL show the Torni version followed by the memory Torni's own process uses in brackets, the Claude Code CLI version, the repository roots and the worktree root, and SHALL NOT show the machine name or the Claude executable's path. The roots and the worktree root SHALL be the settings saved now, so a save from the settings dialog shows at once.

#### Scenario: Released versions
- **WHEN** Torni 0.5.0 runs using 29 MB with Claude Code 2.1.310 and the root C:\Users\me\source\repos
- **THEN** the first settings row reads Torni 0.5.0 (29 MB) · Claude 2.1.310 and the second Repository roots C:\Users\me\source\repos · Worktree root C:\Users\me\source\worktrees

#### Scenario: Local build
- **WHEN** the installed tool's version is 0.5.0-local.20261025T101500 and Torni uses 29 MB
- **THEN** the first settings row starts Torni 0.5.0-local (29 MB)

#### Scenario: An update is available
- **WHEN** Torni 0.5.0 uses 29 MB and 0.6.0 is the newest release
- **THEN** the first settings row starts Torni 0.5.0 (29 MB; 0.6.0 available, U to update)

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
- **WHEN** Torni starts with no settings file
- **THEN** the second settings row ends Worktree root C:\Users\me\source\worktrees on both tabs

### Requirement: Help explains the columns
The help key SHALL show, beneath the keys, one line for each column of the current view saying what it shows, and the path of the Claude Code executable Torni drives. Every line of the help SHALL be visible or reachable by scrolling on a terminal of 36 rows or more, and the whole text SHALL be visible without scrolling on a terminal of 40 rows.

#### Scenario: Sessions help
- **WHEN** the user presses the help key in the sessions view
- **THEN** the help names every column of the sessions table with its meaning, and the Claude executable's path

#### Scenario: Help on a short terminal
- **WHEN** the user presses the help key on a 120 x 36 terminal
- **THEN** the help shows its first lines, the arrow keys and PageDown scroll it, and its last line, the Claude Code executable's path, is reached

#### Scenario: Help on a 40-row terminal
- **WHEN** the user presses the help key on a 120 x 40 terminal
- **THEN** the TAB column's line and the Claude Code executable's path are visible without scrolling

### Requirement: The TAB column is a swatch
The sessions table's TAB column SHALL show a two-cell block in the colour of the Torni tab showing the session while that tab is alive, in the shade that tab shows from Torni's own tab, with a cell of space before the next column, on the highlighted row as on every other; SHALL show a hollow grey block for a session running as plain claude in its own terminal; and nothing otherwise. The help and the column's tooltip SHALL say what each means.

#### Scenario: A session open in a Vantage tab
- **WHEN** fix-123 is shown in a Torni tab coloured yellow
- **THEN** its TAB cell shows a two-cell yellow block, then a space before the state

#### Scenario: A terminal session
- **WHEN** study was started as plain claude in a tab of its own
- **THEN** its TAB cell is a hollow block drawn in grey

#### Scenario: No tab
- **WHEN** a background session has no live Torni tab
- **THEN** its TAB cell is blank

#### Scenario: The help names the blocks
- **WHEN** the help is shown on the sessions view
- **THEN** TAB's line says a block in the tab's colour marks a session a Torni tab shows and a hollow block a plain claude in its own terminal

#### Scenario: The highlighted row keeps the colour
- **WHEN** fix-123, shown in a Torni tab coloured red, is the selected row
- **THEN** its TAB block is the same shade of red as on any other row, not the highlight's colour

### Requirement: The TAB block matches the tab as it looks from Torni
The TAB column's block SHALL be drawn in the shade Windows Terminal shows for that session's tab while another tab is selected, because Torni's own tab is the selected one whenever the table is read. The shade SHALL be the same on the selected row as on any other, and SHALL come from one rule applied to the tab's colour.

#### Scenario: A red tab
- **WHEN** a session's Torni tab is red and the Torni tab is selected
- **THEN** the session's TAB block is the faded red its tab shows in the tab strip, not the bright red the tab shows when selected

#### Scenario: The selected row
- **WHEN** the row of a session with a blue tab is selected
- **THEN** its TAB block is the same faded blue on the selection bar

#### Scenario: Every palette colour
- **WHEN** each colour of the tab palette is turned into its shade
- **THEN** each matches the colour measured from Windows Terminal's tab strip for that colour, within one step in each channel

### Requirement: Versions are shown without noise
A local build SHALL show its prerelease label without the build timestamp, and a Claude version that cannot be read SHALL show as unknown.

#### Scenario: A local preview build
- **WHEN** the installed tool's version is 0.18.0-preview.12-local.20261031083825 and Torni uses 35 MB
- **THEN** the first settings row starts Torni 0.18.0-preview.12-local (35 MB)

### Requirement: An old Claude Code is flagged but still works
A Claude version older than the minimum Torni supports SHALL show that minimum and ask for Claude Code to be updated, and the view SHALL work as usual.

#### Scenario: An older minor version
- **WHEN** the minimum is 2.1.309 and the Claude Code CLI reports 2.0.99
- **THEN** the first settings row reads Claude 2.0.99 (below 2.1.309, update Claude Code) and the sessions are listed as usual
