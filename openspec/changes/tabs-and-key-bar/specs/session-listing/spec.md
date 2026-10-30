# Spec Delta

## MODIFIED Requirements

### Requirement: The screen is a header, a table and a footer
The sessions view SHALL be the Sessions tab of the console: under the settings line and the tabs' row, a bordered table whose title counts the rows shown, names the filter when one is set and names the selected session; a highlighted bar on the selected row; and beneath the table a footer crumb naming the view with a status line beside it, above the key bar.

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

### Requirement: The header shows the versions and the roots
The settings line SHALL show the Vantage version followed by the memory Vantage's own process uses in brackets, the Claude Code CLI version, the repository roots and the worktree root, and SHALL NOT show the machine name or the Claude executable's path. A local build SHALL show its prerelease label without the build timestamp, and a Claude version that cannot be read SHALL show as unknown. A Claude version older than the minimum Vantage supports SHALL show that minimum and ask for Claude Code to be updated; the view SHALL work as usual. The roots and the worktree root SHALL be the settings saved now, so a save from the settings dialog shows at once.

#### Scenario: Released versions
- **WHEN** Vantage 0.5.0 runs using 29 MB with Claude Code 2.1.310 and the root C:\Users\me\source\repos
- **THEN** the settings line reads Vantage 0.5.0 (29 MB) · Claude 2.1.310 · Repository roots C:\Users\me\source\repos · Worktree root C:\Users\me\source\worktrees

#### Scenario: Local build
- **WHEN** the installed tool's version is 0.5.0-local.20261025T101500 and Vantage uses 29 MB
- **THEN** the line starts Vantage 0.5.0-local (29 MB)

#### Scenario: An update is available
- **WHEN** Vantage 0.5.0 uses 29 MB and 0.6.0 is the newest release
- **THEN** the line starts Vantage 0.5.0 (29 MB; 0.6.0 available, U to update)

#### Scenario: Claude version unreadable
- **WHEN** the Claude Code CLI's version command fails or prints something without a version
- **THEN** the line reads Claude unknown and the view works as usual

#### Scenario: Claude version below the minimum
- **WHEN** the minimum is 2.1.309 and the Claude Code CLI reports 2.1.302
- **THEN** the line reads Claude 2.1.302 (below 2.1.309, update Claude Code) and the view works as usual

#### Scenario: Claude version at the minimum
- **WHEN** the minimum is 2.1.309 and the Claude Code CLI reports 2.1.309
- **THEN** the line reads Claude 2.1.309 with no warning

#### Scenario: Default worktree root
- **WHEN** Vantage starts with no settings file
- **THEN** the line ends Worktree root C:\Users\me\source\worktrees on both tabs

### Requirement: The header summarises sessions by state
The Sessions tab's title SHALL carry the number of sessions listed in brackets, and the counts beside the tabs SHALL show one glyph, count and state word per state present, needs input first; with no session listed the tab SHALL read Sessions (0) and the counts row SHALL be empty.

#### Scenario: Mixed states
- **WHEN** nine sessions are listed, four of them stopped and the rest needing input
- **THEN** the tab reads Sessions (9) and the counts read ◐ 5 need input · ■ 4 stopped

#### Scenario: Every session needs input
- **WHEN** six sessions are listed and every one needs input
- **THEN** the tab reads Sessions (6) and the counts read ◐ 6 need input

#### Scenario: No sessions
- **WHEN** no session is listed
- **THEN** the tab reads Sessions (0) and the counts row is empty

### Requirement: The header counts sessions needing input
The counts beside the tabs SHALL show how many sessions are in the needs-input state, first and in amber, whenever any is.

#### Scenario: Two sessions need input
- **WHEN** two sessions are in the needs-input state
- **THEN** the counts row starts ◐ 2 need input in amber

### Requirement: Key hints explain themselves on hover
Each item of the key bar SHALL show a tooltip while the mouse pointer rests on it, saying in one sentence what the key does and when to use it, drawn in its own colours apart from the bar's text.

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on x Stop in the key bar of the Sessions tab
- **THEN** a tooltip in colours distinct from the bar says that x stops the session, which stays listed as stopped and can be resumed

#### Scenario: Every key has a tip
- **WHEN** the help lists a key of either view
- **THEN** that key has a tooltip sentence of at most 80 characters

### Requirement: Long names never hide columns
The sessions table SHALL give WHERE the width its other columns leave and SHALL fit every column on a 120-column terminal; a WHERE value too long for its width SHALL lose the middle of its repository name, marked with an ellipsis, and keep its slug and branch whole. NAME SHALL keep its cap and ellipsis, the row SHALL keep the full values for every key that uses them, and Left and Right SHALL still scroll the table on a narrower terminal. The key bar SHALL show every key that applies within 120 columns, with its words.

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
- **WHEN** a running session is selected on the Sessions tab
- **THEN** m Memory is in the key bar's first row, and the help lists `m` as the memory breakdown key
