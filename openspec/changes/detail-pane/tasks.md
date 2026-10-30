# Tasks

## 1. The claude logs spike (recorded, no code kept)

- [ ] 1.1 Against a background session that has run a task, time `claude logs <id>` five times and record wall time, output size in bytes and lines, whether it holds CSI, OSC or DCS sequences or cursor movement, and what it answers for a stopped session, for an interactive session's id and for a part-name matching two sessions. Verify by recording the figures here and in CLAUDE.md's CLI facts (with the date and CLI version), and by setting the output refresh interval and the size cap in design.md from them.

## 2. The rules and the port (Domain, Ports, Adapters)

- [ ] 2.1 Red: `AnsiTextShould` asserts `Strip` removes a colour sequence, an OSC title, a DCS block and a cursor-move sequence from a sample and keeps its text. Green: `Domain\AnsiText`; the harness's `EscapeSequences.Strip` delegates to it. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` and the HeadlessTerminal tests pass.
- [ ] 2.2 Red: `ClaudeAgentsListingShould.KeepTheWaitingReason` asserts a listing row with waitingFor "permission prompt" gives a session whose `WaitingFor` is that text and null when absent. Green: `Session.WaitingFor`, read in the listing. Verify the Infrastructure suite passes.
- [ ] 2.3 Red: `ClaudeCliShould.ReadLogsByJobId` asserts `LogsAsync` runs `logs <jobId>` and answers the standard output, and null on a non-zero exit; `ChildCommandShould` asserts `logs` is a Claude subcommand word. Green: `IClaudeCli.LogsAsync`, `ClaudeCli`, `ChildCommandWords.Claude`. Verify the Infrastructure and Core suites pass, `DiagnosticEventsShould` included.
- [ ] 2.4 Red: `GitCliShould` (real git) asserts `UnpushedCommitsAsync` lists the hash, age and subject of commits not on any remote, newest first, up to the limit, and `ChangedFilesAsync` lists changed and untracked paths up to the limit. Green: the two `IGit` methods in `GitCli`; the `FakeGit` double answers lists. Verify the Infrastructure suite passes.
- [ ] 2.5 The contract test: `ClaudeContractShould.PrintLogsForTheStartedSession` runs `claude logs <id>` on the session the contract starts and asserts exit 0; `ClaudeHelpSurfaceShould` already lists `logs`. Verify with `$env:VANTAGE_CLAUDE_CONTRACT = 1; dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug -- --filter-trait "Category=ClaudeContract"`; config.yaml's CLI list gains `claude logs <id>`.

## 3. The read model (Application)

- [ ] 3.1 Red: `SessionActivityShould` asserts a session moving from needs input to working records the clock's time, idle to working records it, working to needs input does not, an interactive session is ignored, and an unseen session answers null. Green: `SessionActivity.Observe` and `LastPromptedAt`; `SessionsScreenDefinition.OnInput` calls it beside the notifier. Verify the Core suite passes.
- [ ] 3.2 Red: `DetailLoaderShould` asserts a session detail carries the reason, the started time, the activity's time, the id, folder, branch, pull request, tab colour, the inspector's counts, the memory and the stripped last lines of the fake CLI's logs; a worktree detail carries the health, the created record, the merge answer, the counts, the session and the unpushed commits or changed files; a failing fake git marks that line unavailable and the rest is filled. Green: `SessionDetail`, `WorktreeDetail`, `DetailLoader`. Verify the Core suite passes.
- [ ] 3.3 Red: `WordingOfDetailShould` holds the lines of the detail-pane spec's scenarios verbatim (the permission prompt, no prompt sent yet, not seen since Vantage started, nothing to measure, the terminal session's output line, unavailable, the worktree lines, "… 7 more"). Green: `Wording.DetailLines` for both records. Verify the Core suite passes.
- [ ] 3.4 Red: `WordingOfKeysAndColumnsShould` asserts `SessionsColumns` has no MEM and the `m` key's line names the pane; `DiagnosticsPrivacyShould` gains a logs fixture whose lines must never appear in the diagnostics output or a problem report. Green: the column list, the words, the fixture. Verify the Core suite passes and `SmokeScript.ColumnOnlyIn` still resolves to NAME for the sessions table.

## 4. The pane (Tui)

- [ ] 4.1 `DetailPane`: a `FrameView` under each `TableBody`'s table, `Arrangement = TopResizable`, nine rows by default, one height kept by `ConsoleScreen` for both tabs; it draws `Wording.DetailLines` and the output lines. Verify with `dotnet run --project tools\HeadlessTerminal -- drive` at 120 x 36: the capture (`grid`) shows the pane titled with the selected row's name under the table with the facts lines; a drag of the divider (SGR press, motion and release on the pane's top border) changes the pane's height. Record whether the drag works in 2.5.0 here; if not, stop and revise the spec's drag scenario with the owner.
- [ ] 4.2 `ConsoleScreen` requests the selected row's detail from `DetailLoader` on selection change and after each `RowsLoaded`, with a rising sequence number, applies only the latest answer, and never awaits it on the refresh path; `SessionsScreenDefinition` drops the MEM cell. Verify with a headless drive against a session at a permission prompt: the capture shows the question in the output area within one refresh of selecting the row, and the table's own refresh timestamps in the diagnostic log are unchanged; then Down to a stopped row shows nothing to measure and no output. Record here.
- [ ] 4.3 README: a "The detail pane" section (what each line is, that the output is `claude logs` and never recorded, the divider), the sessions columns without MEM, the memory key's words, "What Vantage records" naming the logs command as counted only; CLAUDE.md: the CLI fact from the spike, the pane in the architecture paragraph, the diagnostics rule naming program output from logs. Verify by reading both against `Wording` and the spike's record.

## 5. Version and checks

- [ ] 5.1 Raise `Version` one preview step above `main`'s. Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` all pass.
- [ ] 5.2 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal` and the smoke check against the packed tool. Verify both exit 0.
- [ ] 5.3 The product owner installs the build with `tools\install-local.ps1`, starts a session with a prompt that will ask for permission, selects it and reads the question in the pane before attaching, answers it, and reads Prompted update; selects a worktree with unpushed commits and reads them; drags the divider; opens `!` and confirms no output line is in the preview. Record here.

## Workflow follow-up

- Archive after where-column, tabs-and-key-bar and config-tab have archived, in its own pull request, syncing the new detail-pane spec and the session-listing delta into `openspec\specs`.
- Once the series is archived, the owner decides when `Version` drops its preview suffix for 0.18.0.
