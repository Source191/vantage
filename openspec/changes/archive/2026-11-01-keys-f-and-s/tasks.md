# Tasks

## 1. Config: no standing description

- [x] 1.1 Red: `ConfigViewShould.Show_no_message_for_the_selected_row` (Shown is `StatusMessage.None` with a row selected and no message); green: `ConfigViewState.Shown => Message`; verify the fast suite passes
- [x] 1.2 Delete `StatusMessage.SettingHint`, its `ToneOf` arm, its `Wording` case and its exception in `StatusLayout.Closes`; update `StatusLayoutShould`, `StatusTonesShould` and `WordingOfConfigShould`; verify `dotnet build -c Debug` and the fast suite pass

## 2. Folder keys f and s

- [x] 2.1 Red: `SessionsViewShould` facts that f raises `OpenFolderInEditor`, s raises `FindSolution`, and e and v raise nothing; green: move the reducer cases; verify the tests pass
- [x] 2.2 Red: `WorktreesViewShould` facts that f, s and n raise `OpenFolderInEditor`, `FindSolution` and `StartSessionIn` (n refusing a leftover as s did), and e and v raise nothing; green: move the cases; verify the tests pass
- [x] 2.3 Red: `ConfigViewShould` facts that f opens the folder (the folder holding Settings file) and e raises nothing; green: move the case; verify the tests pass
- [x] 2.4 Red: `WordingOfKeyBarShould` and `WordingOfKeysAndColumnsShould` facts for the bars in the specs (f Open folder, s Open solution, n New session, Enter Open session, Config's c, f) and the help listing no e or v; green: `SessionsKeys`, `WorktreesKeys`, `ConfigKeys` and the bar rows in `WordingOfConsole`; verify every bar still fits 120 columns
- [x] 2.5 Move `tools\HeadlessTerminal\SmokeScript` to the hints New session and Open solution; verify `SmokeScriptShould` passes

## 3. Worktrees' Enter opens the session's tab

- [x] 3.1 Refactor: extract `SessionsView.Opening(SessionRow)` returning the `RowDecision` Sessions' Enter uses; verify the existing `SessionsViewShould` Enter facts pass unchanged
- [x] 3.2 Red: `WordingShould` and `StatusTonesShould` facts for `StatusMessage.SessionNotListed` (names the session, orange refusal); green: the case, its words and tone
- [x] 3.3 Red: `ConsoleViewShould` facts that `ConsoleView.OpenSession` answers `OpenTabForSession` for a listed background session with its live tab, `RunsInOwnTerminal` for a terminal session, `NeverSentAMessage` for one never sent a message, `SessionNotListed` for an id the Sessions rows do not hold, and picks the right one of two sessions sharing a name; green: the function
- [x] 3.4 Red: `WorktreesViewShould` fact that Enter raises `OpenWorktreeSession(id, name)` (and `NoSessionIn` with no session); green: replace `JumpToSession` with `OpenWorktreeSession` and the `IConsultsSessions` marker; remove `JumpToSession` from `ConsoleView.Apply`
- [x] 3.5 Red: `EffectRoutingShould` fails for an `IConsultsSessions` effect nothing routes; green: `TableBody` routes the marker to `ITabHost`, and `ConsoleScreen` asks `ConsoleView.OpenSession` and has the worktrees body dispatch the effect or show the message; verify `dotnet test -c Debug --no-build` at the solution passes

## 4. Documentation and release

- [x] 4.1 Update the README key tables (Sessions, Worktrees, Config) and the Config tab's description of the status line; verify no e, v or Worktrees s remains in them
- [x] 4.2 Update `CLAUDE.md` (the c, e, v keys, s on the worktrees row, `JumpToSession`, Config's standing message) and `openspec\config.yaml` (n on the worktrees view); verify with a search for the old letters
- [x] 4.3 Raise `Version` to 0.20.0-preview.1; verify `dotnet build -c Debug`, the solution's tests and `dotnet format --verify-no-changes` pass
- [x] 4.4 Install locally with `tools\install-local.ps1` and run the smoke check against it; verify by hand f, s, n and Enter on a worktree with a session that has a tab, and that the Config tab has no status line at rest

## Workflow follow-up

- Run `openspec validate keys-f-and-s` and archive the change in the pull request that completes it.
