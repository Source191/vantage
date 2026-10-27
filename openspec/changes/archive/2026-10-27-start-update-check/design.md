# Design

## Context

`UpdateCheck` (Application) already does everything the spec asks once it runs: `RunAsync` reads the record into the field `Status` reads, decides with `UpdateCheckRule.ShouldCheck`, asks `IPackageVersions` (whose nuget.org adapter answers null on any HTTP, timeout or JSON failure) and writes the record. `UpdateCheckShould` covers all of that. Nothing calls `RunAsync`: `Program.cs` builds the check and hands it to the header (`ScreenContext`) and `DialogEffects`, and `Program.cs` is untested wiring. That is why the gap got through.

`JsonFileStore` answers null for a missing, locked or unreadable record, but `WriteUpdateCheckAsync` can still throw (for example `UnauthorizedAccessException`), and an awaited fault on a pool thread that nobody observes would be lost silently, or would surface as an unobserved task exception.

The header reads `updates.Status` on every redraw (`TableScreen` calls `ScreenContext.HeaderValues`), and the sessions screen redraws on its refresh timer, so a status that changes on a pool thread shows up without any signal to the screen.

## Goals / Non-Goals

**Goals:**
- A test fails if the console stops starting the check.
- The first screen never waits for the check, and a fault in it never reaches the screen.

**Non-Goals:**
- Changing the check's rules, the record's format or the `U` flow.
- Pushing a redraw when the check finishes: the refresh timer is enough.
- Checking again while a long-running Vantage stays open.

## Decisions

- **`ScreenLoop` starts the check, not `Program.cs`.** `ScreenLoop` takes the `UpdateCheck` as a constructor parameter and starts it at the top of `RunAsync`, after claiming its own tab and before the first screen runs. `ScreenLoopShould` already builds the loop with in-memory fakes, so the regression fact sits in a tested class; `Program.cs` only passes the instance on. The alternative, a `Task.Run` in `Program.cs`, would fix the bug but leave it untested, the same way it slipped through before.
- **On a pool thread, never awaited by the loop.** `Task.Run(() => updates.RunAsync(cancellationToken))` keeps the file read and the request off the UI thread. Terminal.Gui installs a synchronisation context once a screen runs, and nothing must block on it (CLAUDE.md). Quitting does not wait for the check: an unfinished request is abandoned with the process, and nothing has been recorded, so the next start asks again, as the spec says for a failure.
- **Faults are logged in `ScreenLoop`.** A private `CheckForUpdatesAsync` awaits the pool-thread check and catches every exception except cancellation, writing a new `HandoffLog.UpdateCheckFailed` line with it; nothing is rethrown. (A fault-only `ContinueWith` would leave the task cancelled after a successful check.) Cancellation is not a fault and is not logged. The loop already holds the `ILogger`, which is a `NullLogger` unless `VANTAGE_LOG` is set, so the failure goes nowhere else. `UpdateCheck` stays free of logging.
- **Tests wait on the check, not on a delay.** `ScreenLoop` keeps the started task and exposes it read-only (`UpdateCheckTask`, `Task.CompletedTask` before `RunAsync`), so `ScreenLoopShould` awaits it after the loop returns instead of sleeping. The facts: a record of 0.10.0 seen by a 0.9.0 loop leaves `Status` as `Available`; `IUpdateCheckRecords` that throws on write leaves the loop's exit code 0, `Status` up to date and one log line naming the failure.
- **`UpdateCheck`'s summary** says `ScreenLoop` starts it, instead of `Program.cs`.

## Risks / Trade-offs

- [The header shows the notice a moment after the first draw, not on it] → The record is usually read within milliseconds and the next refresh shows it; the spec asks only that it shows without a key being pressed.
- [A read-only property on `ScreenLoop` that only tests use] → Small, and it saves the tests from timing guesses; the alternative (polling `Status` with a timeout) is slower and flaky.
- [The hand-off harness never exercises this] → It sets `VANTAGE_NO_UPDATE_CHECK` on purpose. The manual test plan's section 13 covers the real install, with a 0.16.1 built as a plain release version.
