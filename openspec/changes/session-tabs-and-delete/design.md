# Design

## Context

A session tab is `wt new-tab --title <name> --suppressApplicationTitle --startingDirectory <cwd> pwsh.exe -NoExit -EncodedCommand <script>` (`WindowsTerminal.OpenTabAsync`), and the script (`TabScripts.Attach`) is `vantage claim-tab <guid> $PID`, `Clear-Host`, `claude attach <jobId>`. `TabClaimer` stamps the pending registration with the shell's pid and start time; `TabRegistration.IsLive` is "pending and under 60 seconds old" or "that pid still has that start time"; `SessionRowEnricher` removes stale registrations on every read and sets a row's `HasLiveTab`. `OpenTabHandler` trusts the row flag: `SwitchToTabAsync` takes the first registration for the session, finds its owning `WindowsTerminal.exe` through the parent chain (null once the shell has gone, which makes `UiAutomationTabs` search every window) and selects a tab item by the session's current name. The only code that closes a tab is `CloseEmptyTerminalsHandler`: `TerminalTab.ShellOf(claudePid)`, `Terminate(claude)`, `EndCleanly(shell)`, where `EndCleanly` is `TerminateProcess(handle, 0)` plus a wait of up to five seconds, after which Windows Terminal closes the tab because its root shell exited with 0. `RenameSessionHandler`, `StopSessionHandler`, `DeleteSessionHandler` and `AdoptSessionHandler` never touch the registry or a shell. `DeleteSessionHandler` runs `claude rm`, then `WorktreeInspector.HealthAsync` and `CountAsync` (git status in the folder) whatever the health, then acts only on a `RemoveWorktree` decision; `RemoveWorktreeHandler` acts on all three decisions and already deletes a broken folder and prunes. `RemovalDecision` already maps `Broken` to delete-and-prune. `IGit.RemoveWorktreeAsync` throws `GitException` with git's message on failure.

## Goals / Non-Goals

**Goals:**
- A tab Vantage opened never outlives the attach it was opened for, and never holds a worktree folder open.
- "Open" in the TAB column and the footer's "switched" mean a tab that exists now.
- Delete finishes what it can and says exactly what it could not.

**Non-Goals:**
- Retitling a tab after a rename: `--suppressApplicationTitle` blocks it from inside and `wt.exe` cannot do it from outside, so the tab is replaced.
- Detecting a foreign process's working directory before removal: git's own failure is the signal.

## Decisions

- **`SessionTabs` is the one place that closes a session's tabs.** `Application\Actions\SessionTabs` over `ITabRegistry`, `IProcessRegistry` and `TimeProvider`: `CloseAsync(SessionId)` ends every live claimed registration's shell with `EndCleanly` (which waits), removes every registration for the session (a pending one too, with nothing to end) and answers `TabClosure.Closed` or `TabClosure.NoTab`. Rename, stop and delete call it; adopt cannot, because a plain claude tab has no registration, so it keeps the `ShellOf` walk `CloseEmptyTerminalsHandler` uses, taken before `Terminate` (the parent chain is unreadable once claude is gone) and ended with `EndCleanly` only after the resume reported `Continued`.
- **The attach script exits on success.** `TabScripts.Attach` ends with `if ($LASTEXITCODE -eq 0) { exit }`: a detach, a stop and a removal end the attach cleanly and the tab goes; a failed attach (an untrusted folder, a session already gone) keeps the prompt and its output. Task 1 records attach's exit code in each case first; if a detach exits non-zero, the condition lists that code too. `Shell` and `Update` keep `-NoExit`.
  - *Rejected: a release-tab step that clears the registration and keeps the shell.* It fixes the TAB column but not the locked folder or the owner's dislike of leftover prompts.
- **`o` reads liveness at the key.** `SwitchToTabAsync` reads the session's registration and asks `IsLive(now, processes)`; stale or absent, it removes the registration and falls through to `OpenAsync`, so UI Automation is only asked about a tab whose shell exists. The window-wide search remains for a pending registration in its grace period. `OpenAsync` removes earlier registrations for the session before writing the new pending one. `HasLiveTab` on the effect stays for the reducer's Enter refusal.
- **Rename moves the tab.** Capture the registration (its colour) before `claude rm`, remove, `SessionTabs.CloseAsync`, resume; on `Continued` with a tab closed, `OpenTabHandler.OpenAsync(session with { Name = newName })`, which reuses the job id (the first eight characters of the unchanged session id) and takes the freed colour first. The outcome is `Renamed(OldName, NewName, TabMoved)`. A failed resume restores the old name as today and opens no tab; the outcome says the tab was closed. `RenameSessionHandler` gains `SessionTabs` and `OpenTabHandler` from `Composition`, where the handler instance already exists.
  - *Fallback if task 1 finds the resumed session has a new id:* close only, and the footer says `o` opens a tab under the new name.
- **Delete orders its work: rm, tab, facts, worktree.** `SessionTabs.CloseAsync` runs before `FactsAsync`, so the shell has exited (the wait is inside `EndCleanly`) before git touches the folder. `FactsAsync` counts files only when the health is healthy; otherwise the counts are zero and the health decides. The decision is carried out by a new `WorktreeRemover` (`Application\Actions`), lifted from `RemoveWorktreeHandler`, so both handlers delete a broken folder and prune the same way.
- **"In use" is a git fact read in the git adapter.** `GitCli.RemoveWorktreeAsync` recognises git's "failed to delete ... Permission denied" (and "Directory not empty") and throws `GitException` with `InUse` true, so no handler reads git's words. Both removers catch it and answer `WorktreeInUse(folder)`; the registration stays, the files git already deleted are gone, and the next remove sees a broken or empty leftover and finishes. `DeleteSessionHandler` keeps `Deleted` for the session part, so the outcome reads "Deleted X; its worktree folder is still in use by a tab or program: close it and remove the folder from the worktrees view."
  - *Rejected: a rename probe of the folder before removal.* It moves a folder the user has not asked to move, and git's failure already tells the truth.
- **One parent-chain walker.** `TerminalTab.ShellOf` (case-insensitive, cap 8) and `TabRegistration.OwningTerminal` (case-sensitive, cap 5) become one Domain function with the looser rule, used by both.
- **Stop closes the tab after the stop**, so the attach has already exited when the shell is ended; the outcome gains `TabClosed`.

## Risks / Trade-offs

- [`EndCleanly` terminates the shell while the attach still runs inside it, if the stop or removal has not ended the attach yet] → the CLI calls complete before `CloseAsync` runs, and the exit-on-success means a finished attach has usually ended the tab before Vantage asks.
- [A rename reopens a tab the user had closed on purpose moments before] → the tab is reopened only when one was alive at the rename.
- [Windows keeps the folder locked by the owner's own shell, not a Vantage tab] → reported as in use with the folder named; nothing is forced.
- [Attach's exit code on a detach is not 0] → task 1 measures it before the script changes.

## Open Questions

- Whether `claude attach` exits 0 on Ctrl+Z, after `claude stop` and after `claude rm` (task 1.1); the script's condition follows the answer without changing the specs.
