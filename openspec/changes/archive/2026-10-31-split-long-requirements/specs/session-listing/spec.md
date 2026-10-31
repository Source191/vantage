# Spec Delta

## MODIFIED Requirements

### Requirement: The screen is a header, a table and a footer
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
- **THEN** a status line under the pane says the folder was copied, and it goes when the user presses Down

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

## ADDED Requirements

### Requirement: The status line speaks only when there is something to say
Beneath the detail pane, above the key bar, the sessions view SHALL show a status line only while there is a message or an action is running, the table taking its row otherwise. A message SHALL clear when the selection moves or the filter changes. The status line SHALL NOT name the view or repeat the counts beside the tabs.

#### Scenario: A message cleared by the filter
- **WHEN** the status line says a folder was copied and the user starts a filter with / and types t
- **THEN** the status line goes and the table takes its row

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
