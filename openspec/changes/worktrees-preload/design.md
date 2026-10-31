# Design

## Context

- `ConsoleScreen.Frame.Show` (`src\Source191.Vantage.Tui\ConsoleScreen.cs`, about line 255) calls `worktrees.TriggerLoad()` the first time the Worktrees tab is shown and sets `worktreesScanned`; `Start(worktrees, always: false)` adds a 15-second timer that loads only while `body == Active && worktreesScanned`. The Sessions tab loads at once and every five seconds, shown or not.
- `TableBody.TriggerLoad` runs the tab's load function on a pool thread and applies its `ViewInput` through `app.Invoke`. It has no guard against a load already running, and `RunInBackground` calls it again after every action so the rows show the action's result.
- The waiting spinner is already gated on the rows never having loaded (`waiting.Show(isShown && !State.Loaded && State.Banner is null)` in `TableBody.Redraw`), so a load triggered after the first has landed is silent with no change.
- `Program.cs` hands the console `LoadAsync` (`claude.ListSessionsAsync`, then the enricher) and `ScanAsync` (`claude.ListSessionsAsync` again for its `Sessions`, then `WorktreeInventory.ScanAsync`). `WorktreeInventory.ScanAsync` also starts `pullRequests.RefreshStaleAsync` without awaiting it.
- The Tui layer has no in-memory tests; scheduling rules that need a test go into `Application`, with the Tui holding only wiring, as `ConsoleView` and the reducers already do.
- The README gives about forty seconds for a first scan of fifty repositories.

## Goals / Non-Goals

**Goals:**
- Worktree rows ready by the time the user first opens the tab, without slowing the Sessions tab's first listing.
- No `claude agents` child of the worktrees scan's own.
- No two show- or timer-triggered scans running at once.

**Non-Goals:**
- Rescanning while the Worktrees tab is hidden.
- A per-path scan cache (the README roadmap item stays: it would make each scan cheaper, which this change does not).
- Guarding the Sessions tab's loads or the reload after an action.

## Decisions

### A holder of the latest sessions listing, in Application

`Application\LatestSessions` (name settled in the first task) wraps `IClaudeCli`:
- `ListAsync(token)` asks `claude.ListSessionsAsync`, keeps the listing's `Sessions`, marks the first listing as returned and returns the `SessionListing`. A failure also marks the first listing as returned, keeps the previous sessions, and rethrows, so the Sessions tab's banner is unchanged.
- `SessionsAsync(token)` waits for the first listing to return (a `TaskCompletionSource`, its wait cancellable through `WaitAsync(token)`), then answers the kept sessions, or none when no listing has succeeded.
- The kept sessions are one immutable list replaced whole by the Sessions tab's load and read by scans. When shared-snapshot has merged, they are a `SharedSnapshot<IReadOnlyList<Session>>` (its `CLAUDE.md` rule for a value one thread replaces and another reads); until then, they sit behind a `System.Threading.Lock`. The first-listing signal is the `TaskCompletionSource`, whose `TrySetResult` is safe from any thread. Neither holds null: before a listing succeeds, the sessions are an empty list.

`Program.cs`'s `LoadAsync` lists through `ListAsync`; `ScanAsync` reads `SessionsAsync` and no longer calls `claude.ListSessionsAsync`.

Why here: the wait-for-first-listing is the whole sequencing of the start-up scan, so the Tui needs no "sessions arrived" signal and the rule is tested in Core.Tests with `ClaudeCli` over the `FakeProcessRunner`, as the other Claude listing tests are. Alternative rejected: a callback from the Sessions tab's first load into the Frame to start the scan, which puts an untestable ordering rule in the Tui.

A rescan reads a listing up to five seconds old (the Sessions tab's interval). The SESSION cell and the live-session refusal for removal (`WorktreeInventory.LiveSessionIn`, carried on the row) both come from it. Both were already as old as the last scan, up to 15 seconds by the time a key is pressed; the held listing adds at most one Sessions refresh to that, which is accepted. A session started in a worktree in the last five seconds can therefore be missed by one rescan more than today; the force-remove confirmation is unchanged.

A failed first listing leaves the scan with no sessions rather than failing the scan: the worktrees still list, and the next successful listing fills the SESSION cells at the next rescan. Keeping the previous sessions after a later failure avoids SESSION cells flickering empty on one failed listing.

### One running scan at a time, counted in Application

`Application\RunningLoads` (name settled in its task) keeps two counts behind a `Lock`, reserved and running (a lock rather than a `SharedSnapshot`, because the two are read and changed together):
- `TryReserve()` is called by the Frame on the UI thread before a start-up, show- or timer-triggered `TriggerLoad`: when nothing is reserved or running it reserves one and answers true; otherwise it answers false and the Frame triggers nothing.
- `Track(load)` wraps the scan function: as a call starts it counts one running and takes one reservation if any is held; as it completes or throws it stops counting it. `IsRunning` is reserved plus running above zero.

The reservation is needed because `TableBody.TriggerLoad` calls the load function inside `Task.Run`: a count kept only by the wrapper would start on a pool thread some time after `TriggerLoad` returned, and a show or timer tick in that gap would start a second scan. Reserving on the UI thread closes the gap, and the scan stays counted from the moment it is asked for, including its wait for the first listing, so pressing 2 early starts no second scan and the spinner shows over the one that is running.

The reload after an action calls the wrapped function without reserving, so it is never skipped (skipping it could leave the rows showing the state before the action), but it is counted, so a show during it starts nothing. If it starts while a reservation is held, it takes that reservation, and the reserved scan then counts itself as running; either way the counts reach zero only when both have finished.

Alternatives rejected: an in-flight flag inside `TableBody.TriggerLoad`, which would also gate the Sessions tab and the reload after an action; a counter in `TableBody` reported to the Frame, which would be correct but untested, at the Tui level.

### The Frame's part

- `Start(worktrees, always: false)` becomes a start-up `TriggerLoad()` behind `scans.TryReserve()`, and the timer, whose condition is `body == Active && scans.TryReserve()`.
- `Show(ConsoleTab.Worktrees)` triggers a load when `scans.TryReserve()` answers true, as `ReadConfig` already loads the Config tab on every show.
- `worktreesScanned` and its field go.

### The start-up pull request refresh stays

`WorktreeInventory.ScanAsync` asks for stale pull requests after every scan; the start-up scan therefore runs `gh` or `az` once at start-up even if the Worktrees tab is never opened. It never makes a row wait, the catalogue keeps its one-minute answers and four-at-once limit, and the Sessions tab already looks up its rows' branches through the same catalogue, so the lookups overlap rather than add. Kept; not gated on the tab.

## Risks / Trade-offs

- **Start-up work for a tab never opened:** one full scan (git in every repository and worktree under the roots) per console run. On a large roots folder that is tens of seconds of git processes in the background after the first listing; accepted for the rows being ready, and bounded to one scan.
- **SESSION cell lag after `s` on a worktree:** the reload after the action reads the held listing, which may not yet contain the new session; it appears at the next rescan (15 seconds while shown, or the next show). Previously the reload's own listing could miss it too, if the session had not yet registered.
- **Stale rows for a moment on show:** the rows from the last scan show at once and are replaced when the rescan lands; a worktree removed outside Vantage stays on screen until then. The rescan is quiet by design.
- **Overlapping scans from actions:** two actions in quick succession still start two reloads, as today.
