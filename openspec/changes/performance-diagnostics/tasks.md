# Tasks

Each task is one red-green-refactor cycle (or smaller), with the test named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 1. The guide to monitoring with today's tools

- [x] 1.1 `docs\performance-monitoring.md` describes a working day with `VANTAGE_LOG`, `dotnet-counters`, `dotnet-trace` and the Windows Performance Recorder, and `CONTRIBUTING.md` links it from "The build and test loop". Verified by reading: every command is PowerShell, no em dash, and it describes only what Vantage records today (delivered in the pull request that proposes this change).

## 2. Typical durations for child programs

- [x] 2.1 `DurationSampleShould.TakeTheMedianAndNinetyFifthPercentileByNearestRank`: ninety-nine runs of 1,600 ms and one of 4,900 ms give median 1600 ms, 95th percentile 1600 ms, longest 4900 ms and 100 runs. Red, then `Application\Diagnostics\DurationSample` green. Verify the fact passes.
  Done: red folded into green (the type did not exist, so the build failed); `RankTheLatestRunsInOrder` and `HaveNoFiguresBeforeAnyRun` beside it. The four figures are one shared record, `DurationFigures`, which the process, refresh and stall counts all carry.
- [x] 2.2 `DurationSampleShould.KeepTheLatestThousandButCountAndLongestOverEveryRun`: after 1,001 runs where only the first is the longest, the count is 1001, the longest is the first run's and the percentiles exclude it. Verify the fact passes.
- [x] 2.3 `ProcessCountsShould.HoldTheTypicalDurationsOfEachCommand` (the spec's "Typical and worst runs differ"): `ProcessCounts`' tally keeps a `DurationSample` and `ProcessCount` gains `Median` and `NinetyFifth`. Verify the fact and the existing `ProcessCountsShould` facts pass.
  Done: `ProcessCount` carries one `DurationFigures` (runs, median, 95th percentile, longest) rather than four loose members; red folded into green, as the record's shape changed. All four `ProcessCountsShould` facts pass.
- [x] 2.4 `DiagnosticLineShould.RenderACountWithItsWriteTimeAndTypicalFigures`: `counts claude agents exit 0: 120 runs, median 1650 ms, p95 2100 ms, longest 4900 ms` after the UTC time. Red, then `DiagnosticLine.Render(ProcessCounts, DateTimeOffset)` green; `DiagnosticLog.WriteCounts`, `CrashRecorder` and `ProblemReports` pass the time from their clock. Verify `DiagnosticLogShould`, `CrashRecorderShould` and the problem report facts pass with their expected lines updated.
  Done: the line reads `counts claude agents --json exit 0: ...`, because `ChildCommand` already keeps `--json` as one of claude's listed words; the design's example left it out. `CrashRecorder` and `ProblemReports` take a `TimeProvider` (the tool passes `TimeProvider.System`). All 2,430 runnable facts at the solution pass.
- [x] 2.5 README "What Vantage records": the child programs bullet names the median and 95th percentile and the time on each count line. Verify by reading against the spec delta.
  Done: the README now has one "What it records" bullet under Security and privacy (docs-for-going-live merged the old list), so the counts are named there; read against the spec delta.

## 3. Refreshes are timed

- [x] 3.1 `RefreshCountsShould.CountACompletedPart`: a `TimeAsync(RefreshPart.SessionsListing, ...)` that takes 1.7 s on a `FakeTimeProvider` adds one run of 1700 ms and records no event. Red, then `RefreshPart` and `RefreshCounts` green. Verify the fact passes.
- [x] 3.2 `RefreshCountsShould.RecordAPartSlowerThanThreeSeconds` (the spec's "A slow scan"): 3.4 s for `WorktreesScan` adds a run and records `RefreshSlow(WorktreesScan, 3400 ms)`, rendered `slow refresh worktrees scan in 3400 ms` (`DiagnosticLineShould`). Verify the facts and `DiagnosticEventsShould` pass.
- [x] 3.3 `RefreshCountsShould.NotCountACancelledOrFailedPart`: an `OperationCanceledException` and an `InvalidOperationException` both pass through and leave the counts empty and the log without a line. Verify the fact passes.
  Done (3.1 to 3.3, one cycle, red folded into green as the types were new): beside them `NotRecordAPartOfExactlyThreeSeconds`, `ListThePartsInTheirOrder` and the theory `DiagnosticLineShould.RenderASlowRefreshByPart`, which names the pull-request part `pull-request refresh`. `RefreshCounts.Add(part, duration)` is public for a caller that times itself (the catalogue, 3.4). Core suite: 2,070 passed.
- [x] 3.4 `PullRequestCatalogueShould.TimeARefreshThatStartsALookup` and `NotTimeARefreshWithNothingStale`: `PullRequestCatalogue` takes `RefreshCounts` and adds a `PullRequestRefresh` run from the call to the last lookup landing, only when one started. Verify the facts and the existing catalogue facts pass.
  Done: "started" means this call created a lookup; a call that only joins lookups already running is not timed either (`NotTimeARefreshThatOnlyJoinsALookupAlreadyRunning`), or a burst of rows asking at once would count the same lookup several times. `RecordARefreshSlowerThanThreeSeconds` beside them. The catalogue starts the clock as the call begins and hands the duration to `RefreshCounts.Add`. Whole solution: 2,445 passed, 9 skipped.
- [x] 3.5 `PerformanceCounts(ProcessCounts, RefreshCounts, StallCounts)` with an empty `StallCounts` for now: `DiagnosticLogShould.WriteEveryRefreshPartsCount` (`refreshes sessions listing: ...` per part with runs) and `CrashRecorderShould` write all three; `Program.cs` wraps the listing and the enrichment in `LoadAsync` and only `inventory.ScanAsync` in `ScanAsync` (the spec's "The scan waits for the first listing"). Verify the facts pass and `Program.cs` builds.
  Done, moved: tidy-composition-root (#177) took the loads out of `Program.cs`, so the wrapping is in `Application\ConsoleLoads` (`LoadSessionsAsync` times the listing and the enrichment apart, `ScanWorktreesAsync` only the inventory scan), where it is tested: `ConsoleLoadsShould.TimeTheListingAndTheEnrichmentApart`, `TimeTheScanWithoutItsWaitForTheFirstListing` (a 9 s listing, the scan counted as 0 ms with no slow line) and `NotTimeAFailedListing`. `FakeProcessRunner` gains an `OnRun` hook so a run takes time on the fake clock. `SharedParts` holds one `PerformanceCounts` (it replaces the `ProcessCounts` member) and `ConsoleComposition` hands its `RefreshCounts` to the loads and the catalogue. Whole solution: 2,449 passed, 9 skipped.
- [x] 3.6 `DiagnosticsPrivacyShould` gains a slow scan over the Northwind fixtures and fails on any fixture value in the output. Verify the fact passes.
  Done with 3.5 in one commit: the daily flows scan a linked Northwind worktree through the real `WorktreeInventory`, each git run taking 4 s on the fake clock, and the fact asserts the `slow refresh worktrees scan` line and checks the refresh counts with everything else.
- [x] 3.7 README "What Vantage records": a refreshes bullet (parts, typical figures, the 3-second line). Verify by reading against the spec delta.
  Done: a sentence in the one "What it records" bullet rather than a bullet of its own (see 2.5); read against the spec delta.

## 4. Counts are written while Vantage runs

- [x] 4.1 `PeriodicCountsShould.WriteTheCountsEveryTenMinutes`: with a `RecordingSink` and a `FakeTimeProvider` advanced 25 minutes, the sink holds two sets of count lines stamped at 10 and 20 minutes, and `DiagnosticLog.Snapshot` holds no count line. Red, then `PeriodicCounts` on `TimeProvider.CreateTimer` green. Verify the fact passes.
  Done: red folded into green (a new type); the clock is advanced a minute at a time so each write is stamped at its own due time.
- [x] 4.2 `PeriodicCountsShould.StopWritingOnceDisposed`: advancing past 10 minutes after `Dispose` writes nothing. Verify the fact passes.
- [ ] 4.3 `Program.cs` starts `PeriodicCounts` only when `DiagnosticLogSetting` is `File` and disposes it before the exit write. Verify with `tools\install-local.ps1`, then a console run with `VANTAGE_LOG` set for 11 minutes shows a stamped set of count lines in the file before quitting.
  Done, moved: since tidy-composition-root (#177) the exit write is `CrashRecorder.GuardAsync`'s, around `ConsoleComposition.RunAsync`, so `RunAsync` starts it (`using`, only for `DiagnosticLogSetting.File`) and disposes it as it returns, before that write. Only the console writes periodically; `vantage here` keeps its exit write only. The tool builds.
  Owed: the owner runs `tools\install-local.ps1`, then `$env:VANTAGE_LOG = "$env:TEMP\vantage-perf.log"; vantage` for 11 minutes and quits; the file should hold one set of count lines stamped about 10 minutes in, then the exit set. Not run here: an unattended run must not replace the installed tool or touch the real `%LOCALAPPDATA%\Vantage`.
- [x] 4.4 README "What Vantage records" and the troubleshooting entry for `VANTAGE_LOG`: the counts are written every 10 minutes as well as at exit. Verify by reading against the spec delta.
  Done: read against the spec delta ("Counts are written while Vantage runs", the crash file's counts).

## 5. The UI thread is watched

- [x] 5.1 Check in a real console whether Terminal.Gui 2.5.0 runs `app.AddTimeout` callbacks while a modal dialog (the Keys help) is open for 5 seconds, with a temporary log line per beat on a local build. Record the answer with its date and version in `.claude\rules\terminal-gui.md`. Verify the rule's line names the date, the version and the observation.
  Done another way: a console run of Vantage reads and writes the real `%LOCALAPPDATA%\Vantage`, so instead a throwaway Terminal.Gui 2.5.0 program (outside the repository) ran in the headless terminal with a 250 ms timeout and a 5-second modal dialog opened by a key: 18 beats about 280 ms apart while it was open. The decompiled assembly agrees (one `TimedEvents` per application, run on every iteration of the nested `Run`). Timeouts do not pause; recorded in `terminal-gui.md` as observed and read, 1 November 2026, with the lock that makes a dialog opened inside a timeout callback block other threads' `app.Invoke`.
- [x] 5.2 `HeartbeatShould.SetTheBaselineOnTheFirstBeat`: a first `Beat()` counts and records nothing. Red, then `Heartbeat` (250 ms interval) and `StallCounts` green. Verify the fact passes.
  Done (5.2 to 5.5, one cycle, red folded into green as `Heartbeat` was new; `StallCounts` came with `PerformanceCounts` in 3.5): beside them `BeatFourTimesASecond`, and the edges (exactly 100 ms late is no stall, exactly 500 ms is counted but not recorded) are in the facts. `DiagnosticLineShould.RenderAStall` covers the line. Core suite: 2,088 passed.
- [x] 5.3 `HeartbeatShould.CountABeatMoreThanAHundredMillisecondsLate` (the spec's "A short stall"): beats 250 ms then 430 ms apart count one stall of 180 ms and record no event; a beat 50 ms late counts nothing. Verify the fact passes.
- [x] 5.4 `HeartbeatShould.RecordABeatMoreThanHalfASecondLate` (the spec's "A freeze the user feels"): 1,070 ms apart counts one stall of 820 ms and records `UiThreadStalled(820 ms)`, rendered `stalled ui thread for 820 ms` (`DiagnosticLineShould`). Verify the facts and `DiagnosticEventsShould` pass.
- [x] 5.5 `HeartbeatShould.IgnoreAGapLongerThanThirtySeconds` (the spec's "The laptop sleeps"): 40 minutes apart counts and records nothing, and the next beat 250 ms later is judged from the new baseline. Verify the fact passes.
- [x] 5.6 Only if 5.1 found that timeouts pause under a modal dialog: `HeartbeatShould.JudgeTheFirstBeatAfterARestartFromTheRestart`, and `DialogEffects` calls `Heartbeat.Restart()` when a dialog closes. Verify the fact and `DialogEffectsShould` pass; otherwise mark this task not needed with the reason.
  Not needed: 5.1 observed (and read from the assembly) that timeouts keep running under a modal dialog, so the first beat after a dialog is judged like any other and no restart is wanted. `Heartbeat` has no `Restart`; its summary says why.
- [x] 5.7 `DiagnosticLineShould.RenderTheStallCountsEvenWithNone`: `stalls ui thread: 0 stalls` and `stalls ui thread: 3 stalls, median 180 ms, p95 820 ms, longest 820 ms`; `PerformanceCounts` now carries the real `StallCounts`. Verify the facts pass.
  Done: the stall line ends every set of counts, so `DiagnosticLogShould`, `CrashRecorderShould` (now with one stall) and `PeriodicCountsShould` expect it. Core suite: 2,089 passed.
- [ ] 5.8 `ConsoleScreen` adds one `app.AddTimeout(Heartbeat.Interval, ...)` that calls `Beat()`, removed with the other timers. Verify the hand-off check (`dotnet run --project tools\HeadlessTerminal -- check`) and the smoke check of the packed tool both exit 0.
- [ ] 5.9 README "What Vantage records": a UI thread bullet (stalls over 100 ms counted, over 500 ms a line, sleep ignored). Verify by reading against the spec delta.

## 6. The guide, the version and the whole-solution check

- [ ] 6.1 `docs\performance-monitoring.md` gains how to read the new lines (refresh, stall and count lines, pulling each kind out with `Select-String`) and drops its "not yet recorded" notes. Verify by reading it against the design's line formats.
- [ ] 6.2 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next minor preview above the stack's latest at the time (0.21.0-preview.1 if `main` is still at 0.20.0-preview.7). Verify `Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 6.3 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate performance-diagnostics --strict` all pass; then a working day with `VANTAGE_LOG` set on the owner's machine produces stamped count lines every 10 minutes and plausible refresh figures. Record the results here.

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs\diagnostics` describes the timing, heartbeat and periodic counts.
