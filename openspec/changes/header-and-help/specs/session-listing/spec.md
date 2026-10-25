# Spec Delta

## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: The header shows the versions and the roots
The header's context SHALL show the Vantage version, the Claude Code CLI version and the repository roots, and SHALL NOT show the machine name or the Claude executable's path. A local build SHALL show its prerelease label without the build timestamp, and a Claude version that cannot be read SHALL show as unknown.

#### Scenario: Released versions
- **WHEN** Vantage 0.5.0 runs with Claude Code 2.1.310 and the root C:\Users\me\source\repos
- **THEN** the header reads Vantage: 0.5.0, Claude: 2.1.310 and Roots: C:\Users\me\source\repos above the session counts

#### Scenario: Local build
- **WHEN** the installed tool's version is 0.5.0-local.20261025T101500
- **THEN** the header reads Vantage: 0.5.0-local

#### Scenario: Claude version unreadable
- **WHEN** the Claude Code CLI's version command fails or prints something without a version
- **THEN** the header reads Claude: unknown and the view works as usual

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
