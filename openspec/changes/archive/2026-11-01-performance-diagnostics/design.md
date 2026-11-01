# Design

## Context

- `Application\Diagnostics` already has one `DiagnosticLog` (the latest 500 events, each also written to every sink), `ProcessCounts` (runs and longest per program, subcommand and exit code, filled by `RecordedProcessRunner` around the one process runner) and `CrashRecorder`. `DiagnosticLog.WriteCounts` writes the count lines to the sinks without keeping them as events; `Program.cs` calls it at exit and `CrashRecorder` on a crash. `ProblemReports` renders the process counts through `DiagnosticLine.Render(ProcessCounts)`.
- Every event is checked by `DiagnosticEventsShould` (numbers, durations, versions, enums, `CodeName`, `ExceptionSummary`, `ChildCommand` only) and every daily flow by `DiagnosticsPrivacyShould`.
- A Sessions refresh is `Program.cs`'s `LoadAsync`: `LatestSessions.ListAsync` (the `claude agents` listing) then `SessionRowEnricher.EnrichAsync`. A Worktrees scan is `ScanAsync`: it waits for the latest listing through `LatestSessions.SessionsAsync`, then runs `WorktreeInventory.ScanAsync`. Both the enricher and the inventory start `PullRequestCatalogue.RefreshStaleAsync` without awaiting it.
- `Tui\ConsoleScreen` drives its refreshes with `app.AddTimeout`. The test projects use `FakeTimeProvider` (Microsoft.Extensions.TimeProvider.Testing), which also drives `TimeProvider.CreateTimer`.

## Goals / Non-Goals

**Goals:**
- Every new figure is computed and judged in `Application`, tested with `FakeTimeProvider`; `Tui` and `Program.cs` only call it.
- Bounded memory whatever the run's length.
- One rendering of every new line in `DiagnosticLine`, so the file, the crash file and the problem report agree.

**Non-Goals:**
- No figure is shown on the screen, and nothing about how often or how Vantage refreshes changes.
- No new package, and no `System.Diagnostics.Metrics` or EventSource surface.

## Decisions

### A shared duration sample with exact percentiles over the latest 1,000

`DurationSample` keeps the run count, the longest duration and a ring of the latest 1,000 durations; its snapshot sorts a copy and takes the median and the 95th percentile by nearest rank. `ProcessCounts`' tally, the refresh counts and the stall counts all hold one.

- **Why 1,000:** at the 5-second refresh it covers the last 80 minutes or more of the busiest key, longer than the 10-minute writing interval, and costs 8 KB a key; about 40 keys are about 320 KB, which stays within the memory decision of 29 October 2026.
- **Rejected, every duration:** unbounded over a day (17,000 listings).
- **Rejected, fixed buckets (a histogram):** bounded and cumulative, but the percentiles become bucket edges and the bucket bounds need choosing per program; exact figures over a recent window answer "what does it cost now" better.
- **Rejected, resetting the sample at each write:** per-interval figures would be sharper, but the exit line, the crash file and the problem report would then hold only the last few minutes. The count and the longest stay cumulative; the window gives recency.

### Counts grouped in one record

A `PerformanceCounts(ProcessCounts Processes, RefreshCounts Refreshes, StallCounts Stalls)` record is what `DiagnosticLog.WriteCounts`, `CrashRecorder` and the periodic writer take, so a fourth kind later is one member, not a parameter on three signatures. `ProblemReports` keeps taking `ProcessCounts` alone: the report's 7,500-character limit already trims the counts, and refresh and stall figures belong in a performance investigation rather than a bug report.

### Refresh timing by a wrapper, not inside each reader

`RefreshCounts.TimeAsync<T>(RefreshPart part, Func<Task<T>> run)` measures with `TimeProvider.GetTimestamp`, adds the duration on success, records `RefreshSlow(part, duration)` above 3 seconds, and lets an `OperationCanceledException` or any other exception pass uncounted (a failure is already recorded by the view's load-failure path). `RefreshPart` is `SessionsListing`, `SessionsEnrichment`, `WorktreesScan` and `PullRequestRefresh`.

- `Program.cs`'s `LoadAsync` wraps the listing and the enrichment separately, and `ScanAsync` wraps only `inventory.ScanAsync`, so the first scan's wait for the first listing is not counted as scan time.
- `PullRequestCatalogue` takes `RefreshCounts` and times each `RefreshStaleAsync` that starts at least one lookup, from the call to the last lookup landing; a call with nothing stale is not counted, or the near-zero runs would hide the real ones in the median.
- **Why 3 seconds:** the listing normally takes 1.6 to 2 seconds; 3 is clearly worse than normal and still under the 5-second Sessions cadence, so a slow line means the next refresh is at risk. One constant beside the wrapper.
- **Rejected, timing inside `SessionRowEnricher` and `WorktreeInventory`:** each would need the clock and the counts, and their tests would grow timing concerns; the wrapper is tested once.

### A heartbeat judged in Application, beaten from the UI thread

`Heartbeat(TimeProvider clock, StallCounts stalls, DiagnosticLog log)` has an `Interval` of 250 ms and a `Beat()` that measures the time since the previous beat; the first beat only sets the baseline. Lateness is that time less the interval. Over 100 ms it is a stall, counted; over 500 ms `UiThreadStalled(lateness)` is also recorded; over 30 seconds it is taken as sleep and ignored. `ConsoleScreen` adds one `app.AddTimeout(Heartbeat.Interval, ...)` that calls `Beat()` and returns true.

- **Why 100 and 500 ms:** around 100 ms is where a key press starts to feel sluggish, and half a second is a freeze anyone notices; logging only the latter keeps a sustained slowdown (four late beats a second) from pushing every other event out of the 500-event log, while the count still shows it.
- **Rejected, a pool thread posting `app.Invoke` and timing the round trip:** it measures the same main loop with an extra thread and a race at shutdown.

### Periodic writing on `TimeProvider.CreateTimer`

`PeriodicCounts(TimeProvider clock, DiagnosticLog log, PerformanceCounts counts)` creates a 10-minute timer that calls `log.WriteCounts(counts)`; it is disposable. `Program.cs` starts it only when `DiagnosticLogSetting` is `File`, disposes it before the exit write, and the exit write stays as today. `WriteCounts` already holds the log's lock, and each counts class locks its own snapshot, so a write from the timer's thread is safe beside events from the UI thread.

### Line formats

Count lines gain the time they were written, like event lines, and the typical figures:

```
2026-11-01T10:00:00.000Z counts claude agents exit 0: 120 runs, median 1650 ms, p95 2100 ms, longest 4900 ms
2026-11-01T10:00:00.000Z refreshes sessions listing: 120 runs, median 1700 ms, p95 2200 ms, longest 5000 ms
2026-11-01T10:00:00.000Z stalls ui thread: 3 stalls, median 180 ms, p95 820 ms, longest 820 ms
2026-11-01T10:03:12.417Z slow refresh worktrees scan in 3400 ms
2026-11-01T10:04:55.002Z stalled ui thread for 820 ms
```

The stall line is written even with no stalls (`0 stalls`), so a file shows the watcher ran. `DiagnosticLine.Render` takes the write time for count lines; `ProblemReports` passes the time it builds the report. The words `counts`, `refreshes`, `stalls`, `slow refresh` and `stalled` are distinct first words, so a `Select-String` per kind is enough to pull them out.

## Risks / Trade-offs

- [Terminal.Gui may not run `AddTimeout` callbacks while a modal dialog runs its own loop, so the first beat after a long dialog would read as one long stall] → Task 5.1 checks it in a real console first; if timeouts pause, `DialogEffects` calls `Heartbeat.Restart()` (a new baseline) when a dialog closes, and the check is recorded in `.claude\rules\terminal-gui.md` with its date and version either way.
- [A busy machine (a build, a virus scan) makes Vantage look slow when it is not] → the guide says to note what else ran; the figures are for comparison over a day, not alarms.
- [Count lines change shape, so anything a maintainer wrote to read them breaks] → only maintainers read them, and the README describes the new shape.
- [The heartbeat itself costs a callback four times a second] → it allocates nothing on the fast path; a beat is a timestamp read and a subtraction.
- [Thresholds are guesses] → they are constants beside the code that uses them, and the first day's figures say whether they are right.

## Migration Plan

None: the log file and crash file gain lines and count lines change shape; nothing reads them but people. Rollback is reverting the pull request.
