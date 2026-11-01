# Proposal

## Why

The code review of 0.20.0-preview.7 on 1 November 2026 found that the console's refreshes are applied in the order they finish, not the order they started, and that nothing bounds a child program that never answers. A listing started before `x` or `d` can land after the one their completion started and bring the dead row back for up to five seconds (C-02); a dialog opened from an effect's continuation (`m`, `s`, `!`, the branch question) reverts the rows loaded while it was open (C-03); one `claude agents --json --all` that never exits means a new 189 MB child every five seconds, a start-up worktrees scan that waits for ever and spinners that never stop, because `ProcessRunner` neither times a child out nor kills it on cancellation (C-01, I-01, C-05); and two small view bugs: a filter typed or Escape pressed before the first listing hides the "Listing sessions" or "Scanning worktrees" spinner and shows an empty table (V-01), Escape clearing a filter leaves a stale status message where `/`, typing and Up and Down clear it (V-02), and the first of two overlapping actions stops the status spinner for the second (C-06). Main has moved to 0.20.0-preview.13 since the review and none of these has been fixed on the way; the code they sit in (`ListSelection`, the three reducers, `TableBody`, `ProcessRunner`, `RunningLoads`, `LatestSessions`) is unchanged, and only the wiring around it has moved.

## What Changes

- **The spinner survives a filter:** typing a filter, Backspace or Escape before the first load arrives no longer marks the list loaded, so "Listing sessions" and "Scanning worktrees" stay until the rows do. A bare Escape on the Sessions tab with no filter does nothing, as on the Worktrees and Config tabs.
- **Escape clears the message:** clearing a filter with Escape clears the status message in all three views, as every other filter change and selection move does.
- **Loads apply in start order:** each load a tab starts is numbered on the UI thread, and a result from a load older than the last one applied is dropped, success or failure, so a stop or delete never sees its row come back and the bell and "last prompted" never fire on a reordering.
- **A dialog never reverts the rows:** an effect's continuation runs before the tab's state is read, so rows loaded while a dialog it opened was up are kept when it closes.
- **The spinner waits for every action:** the status line's spinner turns until every action the tab started has reported, not until the first does.
- **One sessions listing at a time:** the timer and start-up reserve the listing as the worktrees scan is reserved, so a slow or stuck listing is never joined by another every five seconds; the reload after an action still runs and is counted.
- **A child program has a time limit:** every captured run (`claude`, `git`, `gh`, `az`, `pwsh`, `vswhere`) that has not exited within the limit is ended with its process tree and reported as a failure naming the program, which the Sessions tab shows as its "listing failed" banner and the diagnostics record as a timed-out run; a run its caller cancels is ended the same way instead of running on. Interactive runs (`claude attach`) have no limit.

Out of scope: a per-caller limit (one limit serves every captured run today); refusing a second key while an action runs; the `created.json` lost update, `config.json` truncation and the other review findings, each its own change; and multiple Claude accounts, which is a later change.

Where this change sits against what landed on main after the review: `tidy-composition-root` landed in PR #177, so the sessions load is `Application\ConsoleLoads.LoadSessionsAsync` and the console is wired in `src\Source191.Vantage\Composition\ConsoleComposition.cs`, which is where the sessions reservation's `Track` now goes; `performance-diagnostics` landed in PR #179, so that load already times its two parts through `RefreshCounts`, and the reservation wraps the whole load outside that timing; `show-release-notes` landed in PR #180, so the implementing pull request also adds its line to `CHANGELOG.md`. The design reads against that code.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-listing`: overlapping refreshes show the listing started last, a listing returned during a dialog is kept, the timer starts no second listing while one runs; the waiting spinner survives a filter or Escape before the first listing; Escape clears the message; a listing that hangs is ended and reported as a failed listing.
- `worktree-management`: the "Scanning worktrees" spinner survives a filter before the first scan; a scan that returns after a later one is dropped.
- `console-layout`: Esc on the Sessions tab with no filter does nothing; the status line's spinner stays until every overlapping action has reported.
- `external-tools`: a captured child program that does not finish within the limit, or whose caller gives up, is ended with its process tree and reported; an interactive run is never limited.
- `diagnostics`: a run ended at its time limit is recorded as a line, beside the slow and not-started runs, and is not counted as a run.

## Impact

- `Application\Views\ListSelection.Reselect` (keeps `Loaded`); `Application\Views\SessionsView`, `WorktreesView`, `ConfigView` (the Escape arm); their facts in `ListSelectionShould`, `SessionsViewShould`, `WorktreesViewShould`, `ConfigViewShould`.
- New `Application\LoadSequence` with `LoadSequenceShould`; `Tui\TableBody` (numbers each load, drops a stale result, runs the continuation before reading `State`, counts the actions in flight); `Tui\ConsoleScreen` (a `RunningLoads` for the sessions listing, reserved by the timer and start-up, beside the heartbeat timer PR #179 added there).
- `Ports\ProcessTimedOutException` (new, beside `IProcessRunner`, shaped like PR #178's `ProgramNotStartedException`); `Adapters\Processes\ProcessRunner` (the limit, the kill, the exception); `Adapters\Claude\ClaudeCli.VersionAsync` (catches it into `NotRead`); `Application\Diagnostics\RecordedProcessRunner` and a new `ProcessTimedOut` event with its `DiagnosticLine` arm, beside the `RefreshSlow` and `UiThreadStalled` events PR #179 added; `ProcessRunnerIntegrationShould`, `ClaudeCliShould`, `RecordedProcessRunnerShould`, `DiagnosticLineShould`.
- `src\Source191.Vantage\Composition\SharedParts.cs` (the runner's limit) and `ConsoleComposition.cs` (the second `RunningLoads` and `Track` around `ConsoleLoads.LoadSessionsAsync`); `Program.cs` is untouched.
- README: the troubleshooting line for a listing that times out; `Version` raised and a `CHANGELOG.md` line under Fixed in the implementing pull request (patch: fixes, no new key).
