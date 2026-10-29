# Tasks

## 1. Domain: sizes, the process table and the tree rule

- [x] 1.1 `MemorySizeShould` (Domain): zero, `FromBytes`, addition, and ordering; then `Domain\MemorySize.cs` as an immutable record struct over bytes. Verify with the Core tests.
- [x] 1.2 `IProcessRegistry` gains `Snapshot()` returning `ProcessFacts(ProcessId, ParentId, Name)` for every process and `UsageOf(processId)` returning `ProcessUsage(StartedAt, PrivateMemory, CommandLine)` or null; `FakeProcessRegistry` grows `Using(id, startedAt, bytes, commandLine)` and answers both from `InTree` and `Using`. Verify the solution builds and every existing test still passes (eleven test files use the fake).
- [x] 1.3 `ProcessTreeShould`: a root with two children and a grandchild sums their private memory and counts four processes; a root with no usage answers null; then `Domain\ProcessTree.cs` with `Under(rootId, snapshot, usageOf)`, `Total`, `Count`, `Root` and `Children` (each a subtree). Verify with the Core tests.
- [x] 1.4 `ProcessTreeShould`: a child whose start time precedes its parent's is left out with everything under it (the reused-identifier case); a member whose usage is null counts as zero and its children are still walked, each judged against the nearest measured ancestor's start time. Verify with the Core tests.
- [x] 1.5 `ProgramLabelShould`: the nine real command lines from the design (`npx` with and without `-y`, the `^"`-escaped forms, `@version` suffixes, `uvx mcp-grafana@latest`, `gk-alpha.exe mcp ...`, `node_repl.exe`, the `chcp.com ... pwsh.exe` wrapper, an API key never in the result) give `@azure-devops/mcp`, `@playwright/mcp`, `@azure/mcp`, `@upstash/context7-mcp`, `mcp-grafana`, `gk-alpha`, `node_repl`, `pwsh`; then `Domain\ProgramLabel.cs` with `From(name, commandLine)`. Verify with the Core tests.

## 2. Application: the figure on the row, the breakdown, the words

- [ ] 2.1 `WordingShould`: `Wording.Memory` gives `0 MB`, `29 MB`, `903 MB`, `1.2 GB`, `12.3 GB`, with a megabyte 1,048,576 bytes as Task Manager counts it; then the method in `WordingOfScreens`. Verify with the Core tests.
- [ ] 2.2 `SessionRowEnricherShould`: a running session's row carries the tree's total as `Memory`; a stopped session's row carries null; a session whose root has gone from the snapshot carries null; one snapshot is taken per refresh (the fake counts). Then `SessionRow.Memory` and the enricher's measurement. Verify with the Core tests.
- [ ] 2.3 `MemoryBreakdownShould`: Claude Code first, children largest first with their labels, total and count, from a tree built with the fake; then `Application\MemoryBreakdown.cs` (`Of(sessionName, tree)`) over `ProgramLabel`. Verify with the Core tests.
- [ ] 2.4 `WordingShould`: `Wording.MemoryBreakdownLines` renders the spec's fix-123 example as its lines and `Wording.MemoryDialogTitle` gives `Memory: fix-123`; then the methods. Verify with the Core tests.
- [ ] 2.5 `SessionsViewShould`: `m` on a running session raises `ShowMemory(session, processId)`; `m` on a stopped session sets `Footer.NothingToMeasure` and raises nothing; `m` with no selection does nothing. Then the effect record (`IDialogEffect`), the footer case, the reducer arm, and its words in `Wording` with a `WordingShould` fact (the reflection fact fails until the words exist). Verify with the Core tests.
- [ ] 2.6 `WordingOfKeysAndColumnsShould`: `SessionsColumns` ends with MEM and its meaning; `SessionsKeys` lists `m` help-only (`Hint: null`, as `b` and `U` are) with the help line "what its memory is made of", so `KeyHintColumnsShould` and the hint layout are untouched. Then the two lines in `WordingOfScreens`. Verify with the Core tests.
- [ ] 2.7 `WordingOfScreensShould`: `HeaderValues` writes `0.5.0 (29 MB)`, `0.5.0-local (29 MB)` and `0.5.0 (29 MB; 0.6.0 available, U to update)`; `DialogName.Memory` has the tab title `Vantage (Memory)`. Then the `MemorySize` parameter on `HeaderValues` and `VantageVersion`, the enum member and its title. Verify with the Core tests and the `UpdateCheck` tests that assert the header text.

## 3. Infrastructure: the adapter

- [x] 3.1 `ProcessRegistryShould` (Infrastructure, real processes): `Snapshot()` lists the test process with its parent and a child it starts (`pwsh -NoExit` through the existing runner pattern, ended in a `finally`); then the Toolhelp walk in `ProcessRegistry`. Verify with the Infrastructure tests.
- [x] 3.2 `ProcessRegistryShould`: `UsageOf` for the child gives a start time within a minute, a positive private memory and a command line containing `pwsh`; for an identifier that has gone it gives null; for the test's own process the private memory is below its working set. Then `GetProcessTimes`, `GetProcessMemoryInfo` with `PROCESS_MEMORY_COUNTERS_EX2` (working-set fallback when the call fails, logged once) and `NtQueryInformationProcess` class 60 in `ProcessRegistry`. Verify with the Infrastructure tests.
- [ ] 3.3 Record the verified facts in CLAUDE.md beside the Windows Terminal facts: private working set through `PROCESS_MEMORY_COUNTERS_EX2`, Toolhelp parent ids and identifier reuse, command lines through information class 60, and that command lines can carry secrets. Verify the section reads as the code does.

## 4. Tui: the column, the dialog and the header

- [ ] 4.1 `SessionsScreenDefinition.Cell` maps column 8 to `Wording.Memory(row.Memory)` or `-`; verify in the headless capture (`drive` with a running session) that the MEM header and a figure appear and the table-fit scroll still reaches it at 120 columns.
- [ ] 4.2 `DialogEffectsShould`: `ShowMemory` has a flow; then `MemoryDialog.Show(app, title, lines)` (Close button with `IsDefault`, Esc closes, `DialogTabTitle.Apply` with `DialogName.Memory`, Height from the line count capped at 24) and the flow in `DialogEffects` that snapshots, builds the tree and the breakdown through `RunInBackground` with `Footer.Working`, then shows the dialog. Verify with the Core tests and by opening the dialog on a running session.
- [ ] 4.3 `Program.cs` reads `processes.UsageOf(Environment.ProcessId)` inside the header function and passes the memory to `HeaderValues`; verify the header shows `(.. MB)` on both views and changes after a refresh.

## 5. Release

- [ ] 5.1 README: the MEM column in the sessions view's column list, `m` in the key list, and a sentence on what is measured (private working set of the session's process tree, not the daemon or the tab) and that command lines are never shown; verify the README's key and column lists match `Wording`.
- [ ] 5.2 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one step as the release rule requires and verify `.github\scripts\Assert-VersionBumped.ps1` passes against `main`.
- [ ] 5.3 Run the full loop: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes`, and `dotnet run --project tools\HeadlessTerminal -- check`; verify all pass.

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs` describes `main`.
