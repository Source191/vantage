# Spec Delta

## ADDED Requirements

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

## MODIFIED Requirements

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
