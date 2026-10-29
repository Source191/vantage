# Spec Delta

## ADDED Requirements

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

## MODIFIED Requirements

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
