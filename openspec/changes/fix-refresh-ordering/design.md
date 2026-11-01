# Design

## Context

`ListSelection.Reselect` (`src\Source191.Vantage.Core\Application\Views\ListSelection.cs:110`) falls through to `Load(Rows)`, whose first line (`:42`) sets `Loaded = true`, so `ClearFilter` (`:77`), Backspace and a typed letter (`:86-87`) mark an empty, unloaded list loaded and `TableBody.Redraw` (`src\Source191.Vantage.Tui\TableBody.cs:253`) hides the waiting spinner. `SessionsView.cs:32` clears the filter on every Escape with no guard, where `WorktreesView.cs:33` and `ConfigView.cs:48` take `when state.Filter.Text.Length > 0`, and none of the three clears `Message` there. `TableBody.TriggerLoad` (`:151-167`) runs the load on a pool thread and applies whatever comes back on the UI thread in finish order; `RunInBackground` (`:197-224`) sets a `bool running` (`:61`, `:200`, `:218`) and reads `State` before it runs `settle()` (`:219`). `ConsoleScreen.StartRefreshing` (`src\Source191.Vantage.Tui\ConsoleScreen.cs:394-406`) triggers the sessions load from the timer and at start-up with no reservation, while `ScanWorktrees` (`:426-432`) reserves through `RunningLoads`. `ProcessRunner.RunAsync` (`src\Source191.Vantage.Infrastructure\Processes\ProcessRunner.cs:14-25`) waits with the caller's token and never kills the child; only `Program.cs:132` passes a token that can fire. `LatestSessions.SessionsAsync` (`:38-43`) waits for the first listing's `finally`, so a listing that never returns holds the start-up scan for ever. `TableBody` and `ConsoleScreen` have no tests; the rules they apply must live in `Application`.

## Goals / Non-Goals

**Goals:**
- Every ordering and counting rule is a pure `Application` type with its own facts; `TableBody` and `ConsoleScreen` only call them.
- A child that never answers costs one banner and one minute, never a second child or a spinner for ever.
- No reducer, `ViewInput` or fake changes shape: `RowsLoaded` carries no number and `FakeProcessRunner` needs no edit.

**Non-Goals:**
- Making `TableBody` testable (that is `tidy-composition-root`'s direction, not this change's).
- A limit chosen per caller, a retry, or any change to how often the tabs refresh.

## Decisions

### Reselect keeps the list's Loaded flag
`Reselect` becomes `Selected is not null ? this : Load(Rows) with { Loaded = Loaded }`, so a filter edit before the first load reselects over the empty rows and leaves `Loaded` false; `Load` itself still sets it. Alternative: guard every caller of `Reselect` in the three reducers, which repeats the rule three times and misses the next caller.

### A bare Escape on Sessions is a no-op, and Escape clears the message
`SessionsView.cs:32` takes `when state.Filter.Text.Length > 0`, so Escape with no filter falls to `_ => Unchanged(state)` (`TabKeys.TryFor` does not claim Escape, checked). All three Escape arms gain `with { Message = StatusMessage.None }`, which the rule in `.claude\rules\console-screen.md` ("the reducers clear a message when the selection moves or the filter changes") already asks for. Alternative: make Escape on Sessions clear the message too when no filter is set, as a "dismiss" key; rejected, the close mark and the ten-second timer already dismiss, and the other two tabs use that Escape to leave.

### Each load carries a ticket from an Application LoadSequence
New `Application\LoadSequence`: `int Begin()` hands out the next number, `bool Accept(int ticket)` answers true and remembers the ticket when it is above the last accepted, false otherwise. `TableBody` holds one per tab, calls `Begin()` inside `TriggerLoad` on the UI thread before `Task.Run`, and in the `app.Invoke` continuation returns before `Apply(input)` when `Accept` is false, so a stale result reaches neither `definition.OnInput` (where `StateTransitionNotifier` and `SessionActivity` observe) nor `Reduce`, success and `LoadFailed` alike. Alternative: a sequence number on `RowsLoaded` and `WorktreeRowsLoaded` checked in the reducers; rejected because it spreads one rule over three reducers and their states and still needs the Tui to stamp it. Alternative: single-flight for every load; rejected because the reload after an action must never be skipped, so overlap stays possible by design.

### The sessions listing is reserved like the worktrees scan
`ConsoleScreen` takes a second `RunningLoads listings`; a `ListSessions()` beside `ScanWorktrees()` reserves it and the timer and start-up call that, while `TableBody.RunInBackground`'s `TriggerLoad()` keeps calling the tab directly, counted by `Track` but never refused. `Program.cs` wraps `LoadAsync` in `listings.Track<ViewInput>`. Why both this and the ticket: the ticket keeps the rows right whatever overlaps; the reservation bounds the children, because a listing slower than five seconds would otherwise be joined by another every tick (at the 60-second limit, twelve 189 MB children before the first is ended). Alternative: rely on the ticket alone; rejected for that memory reason, which the review's C-01 is really about.

### The runner limits every captured run and kills on any cancellation
`ProcessRunner` takes `TimeSpan limit` in its constructor (`Program.cs` passes `ProcessRunner.DefaultLimit`, 60 seconds; a `claude agents` listing takes about two, the slowest `gh` or `az` call a few). `RunAsync` links the caller's token with a `CancellationTokenSource(limit)` and waits on the linked token; in `catch (OperationCanceledException)` it calls `process.Kill(entireProcessTree: true)` inside a `try` that swallows `InvalidOperationException` (already exited), then rethrows when the caller's token is cancelled and otherwise throws `Ports\ProcessTimedOutException(fileName, limit)`. The two `ReadToEndAsync` calls take no token (today they take the caller's): the kill closes the pipes, so they complete, and they are awaited after the kill so nothing is left pending and their own cancellation can never pre-empt the exception below. `RunInteractiveAsync` is unchanged: a hand-off lasts as long as the user wants. Alternative: an `init` property on `ProcessRequest` with a default, so a caller could choose; rejected as YAGNI, no caller has a reason to differ today, and a Ports default would be one more thing every fake must honour.

### The timeout is its own exception, recorded as its own event
`ProcessTimedOutException` is sealed, in its own file beside `IProcessRunner` as `GitException` sits beside `IGit`, derives from `Exception` (not `OperationCanceledException`: a limit is Vantage giving up, not the caller), and carries the limit and the program's bare file name, which the adapter takes with `Path.GetFileName` before constructing it, so no folder reaches a message and Ports touches no `System.IO`. Its message, "claude.exe did not finish within 60 seconds and was ended", is what `Attempt.RunAsync` puts after "listing failed:" on the Sessions tab, which the wording rule allows (an exception's message only to report what failed, in a load failure). `RecordedProcessRunner.Started` catches it first and records a new `ProcessTimedOut(ChildCommand Command, TimeSpan Limit)` event (both member types pass `DiagnosticEventsShould`) instead of `ProcessFailedToStart`, and `DiagnosticLine` renders it as `timed out claude agents after 60000 ms`; `ClaudeCli.VersionAsync` adds it to the exceptions it reads as `NotRead`. Alternative: a typed `LoadFailed` reason worded by `Wording`; bigger, and the owner may prefer it later, so this is the decision most open to overturning.

### Two TableBody edits with no test seam
C-03: `running = false` (now the count), then `StatusMessage settled = settle();`, then `State = definition.WithMessage(State, settled)`, so a `Dispatch` inside the continuation re-increments the count and the rows a dialog's nested `app.Run` let in are kept. C-06: `bool running` becomes `int inFlight`, `Running => inFlight > 0`, incremented before `Task.Run` and decremented first in the continuation. Both are rendering, verified by the headless smoke check and by hand (an `m` dialog left open over a listing; two `d` presses in a row).

## Risks / Trade-offs

- [A legitimate child takes over 60 seconds, such as `gh` on a slow network] → it is reported as a failure and the next refresh retries; the pull-request catalogue already keeps its last answer, and the limit is one constant to raise.
- [`Process.Kill(entireProcessTree: true)` throws `AggregateException` for a descendant it cannot end (review C-08)] → caught alongside `InvalidOperationException` in the kill; the exception that follows still reports the timeout.
- [The ticket drops a failure that followed a success, so the banner never shows] → intended: the newer rows are the truth, and the next load reports its own result.
- [Reserving the listing delays a timed refresh behind a slow one] → the reload after an action is never refused, and a listing over 60 seconds is ended.
- [`LatestSessions.firstListing` is released by a timed-out first listing] → already so: `TrySetResult` is in the `finally` (`LatestSessions.cs:31-34`), covered by `LatestSessionsShould.AnswerNoSessionsWhenTheFirstListingFails`; no new test.
