# Tasks

## 1. The first version (recorded; the screen and the drag were removed in group 4)

- [x] 1.1 The `claude logs` spike, the screen rebuilt from it and a pane showing its last rows under a draggable divider.
  - Done and checked 30 October 2026 (CLI 2.1.314, 0.18.0-preview.8): `claude logs <id>` answered 276 to 345 KB in 1.2 to 2.5 s, a session's whole screen history in one of two shapes (rows padded to the session's width, or the screen drawn line by line), which a terminal replay at the width of Claude's input-box rule rebuilt; the contract test passed against the CLI. The divider drag worked in Terminal.Gui 2.5.0 (`ViewArrangement.TopResizable`; the arranger leaves the frame absolute, so the pane re-anchored itself). On the owner's terminal the pane showed four rows of screen, too few to read a question, and facts that repeated the table; the owner chose facts only, no `claude logs`, and fewer table columns (30 October 2026). Group 4 removes the screen, the logs port, its contract test and CLI facts, and the drag.

## 2. The rules and the ports (Domain, Ports, Adapters)

- [x] 2.1 Red: `ClaudeAgentsListingShould.KeepTheWaitingReason` asserts a listing row with waitingFor "permission prompt" gives a session whose `WaitingFor` is that text and null when absent. Green: `Session.WaitingFor`, read in the listing. Verify the Infrastructure suite passes.
- [x] 2.2 Red: `GitCliIntegrationShould` (real git) asserts `UnpushedCommitsAsync` lists the hash, age and subject of commits not on any remote, newest first, up to the limit, and `ChangedFilesAsync` lists changed and untracked paths up to the limit. Green: the two `IGit` methods in `GitCli` (`UnpushedCommit` in Ports). Verify the Infrastructure suite passes.

## 3. The read model (Application)

- [x] 3.1 `SessionActivityShould`: a session moving from needs input or idle to working records the clock's time, working to needs input does not, an interactive session is ignored, an unseen session answers null. `SessionActivity.Observe` is called from `SessionsScreenDefinition.OnInput` beside the notifier.
- [x] 3.2 `DetailLoaderShould`: a session's counts and last prompt, a worktree's created record, merge state and commits or changed files, each fact unavailable on its own when its read fails.
- [x] 3.3 `DetailRequestsShould`, `DetailLayoutShould`, `DetailSourcesShould`: the order of requests and which answer shows, the facts set out in three columns at the pane's width, and each tab's source.

## 4. Facts only, and slimmer tables (after the owner's check)

- [ ] 4.1 Remove the screen: `Domain\TerminalReplay`, `SessionScreen` and `AnsiText` (the harness's `EscapeSequences` keeps its own expressions, as on `main`), `IClaudeCli.LogsAsync` and `ClaudeCli`'s, `logs` in `ChildCommandWords`, the contract test, the logs flow in `DiagnosticsPrivacyShould`, config.yaml's and CLAUDE.md's logs facts, `SessionOutput`, the screen in `DetailRequests` and `DetailContent`; and the drag (`TopResizable`, the shared height). Verify the build and every suite pass and nothing names `claude logs` outside this change's history.
- [ ] 4.2 Red: `WordingOfKeysAndColumnsShould` asserts the sessions columns NAME, TAB, STATE, REPOSITORY, PR, AGE and the worktrees columns WORKTREE, LAST COMMIT, SESSION, and each help's Pane section. Green: `Wording.SessionsColumns`, `WorktreesColumns`, `SessionsPane`, `WorktreesPane` and the helps; the definitions' cells (REPOSITORY the repository or the folder's label, elastic). Verify the Core suite passes.
- [ ] 4.3 Red: `WordingOfDetailShould` asserts the session's rows (Waiting for, Started, Prompted / Folder, Session / Worktree, Branch, Tab / Changes, Memory, with the worktree as slug, path, main checkout or not in a repository) and the worktree's rows (Health / Branch, PR / Folder, Created / Changes) and listing. Green: `Wording.Detail`. Verify the Core suite passes.
- [ ] 4.4 The pane fits its facts: each `IDetailSource` states its rows (five for sessions, eleven for worktrees, which list up to five items), `TableBody` sizes the table above it, and the waiting spinner centres on the table. Verify headless at 120 x 36: the sessions table shows REPOSITORY with a five-row pane; the worktrees table shows WORKTREE, LAST COMMIT and SESSION with an eleven-row pane and a listing; the spinner of the first worktrees scan sits in the table's area; with `VANTAGE_LOG` set the listing still refreshes every five seconds. Record here.
- [ ] 4.5 README: "The detail pane" (each line, the heights, no pane on Config), both tables' columns and the worktrees mockup, "What Vantage records" unchanged by the pane; CLAUDE.md: the pane in the architecture paragraph. Verify by reading both against `Wording`.

## 5. Version and checks

- [x] 5.1 Raise `Version` one preview step above `main`'s: 0.18.0-preview.8 (main is 0.18.0-preview.7).
- [ ] 5.2 `dotnet build -c Debug` (no warnings), `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes`, `openspec validate detail-pane --strict`; the hand-off check and the smoke check against the packed tool. Verify all pass and both checks exit 0.
- [ ] 5.3 The product owner installs the build with `tools\install-local.ps1`, reads a waiting session's reason and Prompted in the pane, reads a worktree's branch, changes and unpushed commits in the pane, and checks long repository and worktree names show whole at 120 columns. Record here.

## Workflow follow-up

- Archive after where-column, tabs-and-key-bar and config-tab have archived, in its own pull request, syncing the new detail-pane spec and the session-listing and worktree-management deltas into `openspec\specs`.
- Once the series is archived, the owner decides when `Version` drops its preview suffix for 0.18.0.
