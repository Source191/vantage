# Proposal

## Why

With seven sessions open the machine is visibly short of memory and nothing says which session is to blame: Task Manager shows forty claude, node and cmd processes with no session names, and Vantage, which knows every session's process, shows nothing about memory. A spike on 29 October found that a session's own claude process is about 300 MB but its tree, one MCP server per configured server plus whatever the session has started, is three times that, so the useful figure is the tree and the useful detail is which server costs what.

## What Changes

- **A MEM column on the sessions view.** Each running session shows the memory its process tree uses (the session's process and everything it started), as a short figure such as `903 MB` or `1.2 GB`, refreshed with the rows. A stopped session, which has no process, shows a dash. The column's meaning joins the help and the header tooltip like every other column.
- **A memory breakdown on `m`.** On a running session, `m` opens a dialog that lists what the session's memory is made of: Claude Code itself, then one line per program the session started (an MCP server named from the package or executable that launched it, a shell, or anything else), each with its subtree's memory, largest first, and a total with the process count. The dialog is a snapshot taken when it opens and closes with Close or Esc. On a stopped session `m` says there is nothing to measure. The key is listed in the help and has no header hint, because the hint grid is full until the key bar replaces it.
- **Vantage's own figure in the header.** The Vantage line reads `0.17.0 (29 MB)`: Vantage's own process only, not the short-lived listing child it runs every five seconds.
- **Honest numbers.** Memory is the private working set, the figure Task Manager's Memory column shows, so the node runtime that thirty MCP processes share is not counted thirty times (summing working sets doubled the real figure in the spike). A child process counts towards a session only when it started after its parent, because Windows reuses process identifiers and the spike found a dictation app, launched by an installer that had exited, sitting under a session's reused identifier and adding 440 MB to it.
- **Measuring belongs to the process boundary.** The process registry port learns one query, the process trees under a list of identifiers, implemented over a Toolhelp snapshot and per-process memory and command-line queries; the fake grows the same. Command lines are read only to name a program and are never shown, logged or kept: the spike found an API key in one.

Out of scope: the daemon and pty-host processes Claude Code runs per session (neither Vantage's nor a session's work; the breakdown's total is the session tree only), the `claude attach` process in a session's tab, a memory figure on the worktrees view, sorting or filtering by memory, any limit or warning on memory, interactive sessions' tabs beyond what the tree already covers, and multiple Claude accounts, which is a later change. The breakdown lives in a dialog now; the detail pane in the planned redesign is where it moves later, and the rule that builds its lines is written once.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-listing`: "The header shows the versions and the roots" gains Vantage's memory on the Vantage line; three added requirements, "Rows show the session's memory", "A key shows what the memory is made of" and "Memory is measured honestly".
- `tool-updates`: "Vantage checks for a newer release at most once a day" and "The header names an available update" place the update notice after the memory figure inside the same brackets; "The check can be turned off" says the header then shows the version and memory alone.

## Impact

- **Core:** `Domain\IProcessRegistry.cs` (the tree query and its `RunningProcess` record), new `Domain\MemorySize.cs`, `Domain\ProcessTree.cs` (the tree rooted at a process, with the start-time rule) and `Domain\ProgramLabel.cs` (the name taken from a command line), new `Application\MemoryBreakdown.cs` (the dialog's lines), `Application\SessionRowEnricher.cs` (the figure per row), `Application\Views\SessionsView.cs` (the row's figure, the `m` key and its effect), `Application\WordingOfScreens.cs` (the column, the key, the header value, the dialog's words), `Application\DialogName.cs`.
- **Infrastructure:** `Processes\ProcessRegistry.cs` (Toolhelp snapshot, `GetProcessMemoryInfo`, `NtQueryInformationProcess` for the command line).
- **Tui:** `SessionsScreenDefinition.cs` (the MEM cell), new `MemoryDialog.cs`, `DialogEffects.cs` (the flow), `Program.cs` (Vantage's own figure in the header).
- **Tests:** `TestDoubles\FakeProcessRegistry.cs` grows memory and command lines; `ProcessTreeShould`, `ProgramLabelShould`, `MemorySizeShould`, `MemoryBreakdownShould`, `SessionRowEnricherShould`, `SessionsViewShould`, `WordingShould`, `WordingOfKeysAndColumnsShould`, `DialogEffectsShould`; a `ProcessRegistryShould` fact in the Infrastructure tests that measures a real child process.
- **Docs:** the README's sessions view section and key list; `Version` raised as the release rule requires.
