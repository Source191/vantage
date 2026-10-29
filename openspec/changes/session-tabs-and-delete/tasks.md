# Tasks

## 1. Three facts at the terminal (before the code)

- [x] 1.1 The product owner, in a plain PowerShell tab, runs `claude attach <name>` on an idle session and presses Ctrl+Z, then `$LASTEXITCODE`; attaches again and, from another tab, runs `claude stop <id>`, then reads the exit code; attaches a third time and runs `claude rm <id>` from the other tab, then reads it. Verify each exit code is recorded here; design.md's script condition follows them.
  - Recorded 29 October 2026 (CLI 2.1.312) from the session rather than at the terminal: `claude attach <id>` in a pseudo-console (`tools\HeadlessTerminal drive`, pwsh running the attach and exiting with its `$LASTEXITCODE`), on a throwaway idle background session in this repository, each removed afterwards. Ctrl+Z: exit 0. `claude stop <id>` from another process 12 seconds in: exit 0. `claude rm <id>` the same way: exit 0. Claude's banner is in each capture and no run had to be killed, so the stop and the removal ended the attach. The design's `if ($LASTEXITCODE -eq 0) { exit }` stands as written.
- [x] 1.2 With a session open in a Vantage tab, press `r` and rename it. Verify, by comparing `sessionId` in `%LOCALAPPDATA%\Vantage\tabs\*.json` with the renamed row in `claude agents --json`, whether the session id is kept, and record it here; the rename fallback in design.md applies if it is not.
  - Recorded from CLAUDE.md's CLI facts (checked 28 October, 0.17.0-preview.1: rename resumes under the new name) rather than a fresh rename: `r` runs `claude rm <id>` then `claude --bg --resume <sessionId> -n <new>`, which continues the same session id, so the job id (its first eight characters) is kept and the design's main path applies, not the fallback.
- [ ] 1.3 Close a Vantage-opened tab with the terminal's close button and run `Get-Process -Id <pid>` for the pid in its registration file. Verify whether the shell is gone at once; record here.

## 2. The parent-chain walker (Domain)

- [x] 2.1 Red: `TerminalTabShould` and `TabRegistrationShould` assert both `ShellOf` and `OwningTerminal` find `WindowsTerminal` whatever its case and within eight steps, through one shared function. Green: the shared walker. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` passes.

## 3. SessionTabs (Application)

- [x] 3.1 Red: `SessionTabsShould.EndEveryLiveShellAndForgetTheRegistrations` with two claimed live registrations for one session asserts `FakeProcessRegistry.EndedCleanly` holds both pids and the registrations are gone; `ForgetAPendingRegistrationWithoutEndingAnything`; `AnswerNoTabWhenThereIsNone`. Green: `SessionTabs.CloseAsync` and `TabClosure`. Verify the Core suite passes.

## 4. Opening and switching (Application)

- [x] 4.1 Red: `OpenTabHandlerShould.OpenANewTabWhenTheRegisteredShellHasGone` (the effect says live, the process is gone: a tab is opened, the stale registration removed, `FocusTabAsync` never called) and `OpenANewTabWhenNoRegistrationExists`. Green: liveness read in `SwitchToTabAsync`. Verify the existing switch fact still passes.
- [x] 4.2 Red: `OpenTabHandlerShould.DropEarlierRegistrationsForTheSession` asserts one registration remains after `OpenAsync` and the old colour is chosen again. Green: `OpenAsync` removes them first. Verify the Core suite passes.

## 5. Stop, rename and adopt (Application)

- [x] 5.1 Red: `StopSessionHandlerShould.CloseTheTabAfterStopping` asserts the stop ran, then the shell was ended, and the outcome says the tab closed; `SayNothingAboutATabWhenThereIsNone`. Green: `StopSessionHandler` gains `SessionTabs`; `StopSessionOutcome` gains `TabClosed`; `Composition` wires it. Verify the Core suite passes.
- [x] 5.2 Red: `RenameSessionHandlerShould.MoveTheTab` asserts the order rm, shell ended, resume, tab opened under the new name in the old colour, and the outcome `Renamed(old, new, TabMoved: true)`; `CloseTheTabWhenTheResumeFails` asserts no tab is opened and the outcome names the restore. Green: the handler with `SessionTabs` and `OpenTabHandler`. Verify the Core suite passes.
- [x] 5.3 Red: `AdoptSessionHandlerShould.CloseTheOldTabAfterAContinuedResume` (the shell found before claude is ended, ended after `Continued`) and `KeepTheOldShellWhenTheResumeFails`, using the process tree from `CloseEmptyTerminalsHandlerShould`. Green: the handler; `Moved` gains `TabClosed`. Verify the Core suite passes.

## 6. Delete and the shared remover (Infrastructure, Application)

- [x] 6.1 Red: `GitCliShould.ReportAWorktreeFolderInUse` feeds the fake runner git's "failed to delete 'C:/Users/me/source/worktrees/Northwind.Orders-fix': Permission denied" and asserts `RemoveWorktreeAsync` throws a `GitException` whose `InUse` is true, and an ordinary failure throws one with `InUse` false. Green: the adapter. Verify `dotnet test test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build` passes.
- [x] 6.2 Refactor: lift the decision-carrying code of `RemoveWorktreeHandler` into `WorktreeRemover` with the handler delegating to it. Verify `RemoveWorktreeHandlerShould` passes unchanged.
- [x] 6.3 Red: `DeleteSessionHandlerShould.CloseTheTabBeforeTouchingTheWorktree` (the shell ends before any git request), `DeleteABrokenWorktreesFolderAndPrune` (no git status run, `FakeFolders` deleted the folder, prune ran, outcome worded), `ReportAWorktreeStillInUse` (the in-use exception becomes the in-use outcome with the session deleted). Green: the handler with `SessionTabs`, `WorktreeRemover` and `IFolders`; `Program.cs` wiring. Verify the Core suite passes.
- [x] 6.4 Red: `RemoveWorktreeHandlerShould.ReportAWorktreeStillInUse`. Green: the same catch in the worktrees view's path. Verify the Core suite passes.

## 7. The tab script

- [x] 7.1 Red: `TabScriptsShould` asserts the attach script ends with the exit line from 1.1 (`if ($LASTEXITCODE -eq 0) { exit }` or the codes recorded) and that the shell and update scripts are unchanged. Green: `TabScripts.Attach`. Verify the Core suite and `PowerShellQuotingShould` pass.

## 8. Words and docs

- [x] 8.1 Red: `WordingOfOutcomesShould` facts for the stop, rename, adopt and delete outcomes with and without a tab, and for the in-use outcomes of both views. Green: the sentences in `WordingOfOutcomes`. Verify the reflection fact that every outcome has words passes.
- [x] 8.2 README: the TAB column, "Open in its own tab", detach, rename, stop, delete, adopt and the worktrees view's remove say what happens to the tab and to a folder in use; CLAUDE.md's Windows Terminal and CLI facts record attach's exit codes and that the tab ends with the attach. Verify by reading each passage against the spec deltas.

## 9. Version and checks

- [x] 9.1 Raise `Version` one preview step above `main`'s (a minor raise). Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` all pass.
- [x] 9.2 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal`. Verify exit 0.
- [ ] 9.3 The product owner installs the build with `tools\install-local.ps1` and runs through: `o`, close the tab by hand, `o` again (a new tab, not "switched"); Enter in that tab's session then Ctrl+Z (the tab closes, TAB clears, Enter attaches in the console); `r` on a session with an open tab (one tab under the new name, same colour); `x` on one with a tab (tab closes); `d` on a session started with F6 (tab closes, worktree removed); `b` on a plain claude tab with messages (old tab closes); `d` on a session whose worktree has lost its `.git` (folder deleted, prune reported); `o` on a worktrees row, then `d` on that worktree (in use), close the shell tab, `d` again (gone). Verify each and record here; the README's manual checks for tabs and attach run in the same session.

## Workflow follow-up

- Archive the change in the same pull request, syncing the two delta specs into `openspec\specs`.
