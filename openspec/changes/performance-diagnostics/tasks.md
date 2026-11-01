# Tasks

Each task is one red-green-refactor cycle (or smaller), with the test named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 1. The guide to monitoring with today's tools

- [x] 1.1 `docs\performance-monitoring.md` describes a working day with `VANTAGE_LOG`, `dotnet-counters`, `dotnet-trace` and the Windows Performance Recorder, and `CONTRIBUTING.md` links it from "The build and test loop". Verified by reading: every command is PowerShell, no em dash, and it describes only what Vantage records today (delivered in the pull request that proposes this change).

## 2. Typical durations for child programs

- [ ] 2.1 `DurationSampleShould.TakeTheMedianAndNinetyFifthPercentileByNearestRank`: ninety-nine runs of 1,600 ms and one of 4,900 ms give median 1600 ms, 95th percentile 1600 ms, longest 4900 ms and 100 runs. Red, then `Application\Diagnostics\DurationSample` green. Verify the fact passes.
- [ ] 2.2 `DurationSampleShould.KeepTheLatestThousandButCountAndLongestOverEveryRun`: after 1,001 runs where only the first is the longest, the count is 1001, the longest is the first run's and the percentiles exclude it. Verify the fact passes.
- [ ] 2.3 `ProcessCountsShould.HoldTheTypicalDurationsOfEachCommand` (the spec's "Typical and worst runs differ"): `ProcessCounts`' tally keeps a `DurationSample` and `ProcessCount` gains `Median` and `NinetyFifth`. Verify the fact and the existing `ProcessCountsShould` facts pass.
- [ ] 2.4 `DiagnosticLineShould.RenderACountWithItsWriteTimeAndTypicalFigures`: `counts claude agents exit 0: 120 runs, median 1650 ms, p95 2100 ms, longest 4900 ms` after the UTC time. Red, then `DiagnosticLine.Render(ProcessCounts, DateTimeOffset)` green; `DiagnosticLog.WriteCounts`, `CrashRecorder` and `ProblemReports` pass the time from their clock. Verify `DiagnosticLogShould`, `CrashRecorderShould` and the problem report facts pass with their expected lines updated.
- [ ] 2.5 README "What Vantage records": the child programs bullet names the median and 95th percentile and the time on each count line. Verify by reading against the spec delta.

## 3. Refreshes are timed

- [ ] 3.1 `RefreshCountsShould.CountACompletedPart`: a `TimeAsync(RefreshPart.SessionsListing, ...)` that takes 1.7 s on a `FakeTimeProvider` adds one run of 1700 ms and records no event. Red, then `RefreshPart` and `RefreshCounts` green. Verify the fact passes.
- [ ] 3.2 `RefreshCountsShould.RecordAPartSlowerThanThreeSeconds` (the spec's "A slow scan"): 3.4 s for `WorktreesScan` adds a run and records `RefreshSlow(WorktreesScan, 3400 ms)`, rendered `slow refresh worktrees scan in 3400 ms` (`DiagnosticLineShould`). Verify the facts and `DiagnosticEventsShould` pass.
- [ ] 3.3 `RefreshCountsShould.NotCountACancelledOrFailedPart`: an `OperationCanceledException` and an `InvalidOperationException` both pass through and leave the counts empty and the log without a line. Verify the fact passes.
- [ ] 3.4 `PullRequestCatalogueShould.TimeARefreshThatStartsALookup` and `NotTimeARefreshWithNothingStale`: `PullRequestCatalogue` takes `RefreshCounts` and adds a `PullRequestRefresh` run from the call to the last lookup landing, only when one started. Verify the facts and the existing catalogue facts pass.
- [ ] 3.5 `PerformanceCounts(ProcessCounts, RefreshCounts, StallCounts)` with an empty `StallCounts` for now: `DiagnosticLogShould.WriteEveryRefreshPartsCount` (`refreshes sessions listing: ...` per part with runs) and `CrashRecorderShould` write all three; `Program.cs` wraps the listing and the enrichment in `LoadAsync` and only `inventory.ScanAsync` in `ScanAsync` (the spec's "The scan waits for the first listing"). Verify the facts pass and `Program.cs` builds.
- [ ] 3.6 `DiagnosticsPrivacyShould` gains a slow scan over the Northwind fixtures and fails on any fixture value in the output. Verify the fact passes.
- [ ] 3.7 README "What Vantage records": a refreshes bullet (parts, typical figures, the 3-second line). Verify by reading against the spec delta.

## 4. Counts are written while Vantage runs

- [ ] 4.1 `PeriodicCountsShould.WriteTheCountsEveryTenMinutes`: with a `RecordingSink` and a `FakeTimeProvider` advanced 25 minutes, the sink holds two sets of count lines stamped at 10 and 20 minutes, and `DiagnosticLog.Snapshot` holds no count line. Red, then `PeriodicCounts` on `TimeProvider.CreateTimer` green. Verify the fact passes.
- [ ] 4.2 `PeriodicCountsShould.StopWritingOnceDisposed`: advancing past 10 minutes after `Dispose` writes nothing. Verify the fact passes.
- [ ] 4.3 `Program.cs` starts `PeriodicCounts` only when `DiagnosticLogSetting` is `File` and disposes it before the exit write. Verify with `tools\install-local.ps1`, then a console run with `VANTAGE_LOG` set for 11 minutes shows a stamped set of count lines in the file before quitting.
- [ ] 4.4 README "What Vantage records" and the troubleshooting entry for `VANTAGE_LOG`: the counts are written every 10 minutes as well as at exit. Verify by reading against the spec delta.

## 5. The UI thread is watched

- [ ] 5.1 Check in a real console whether Terminal.Gui 2.5.0 runs `app.AddTimeout` callbacks while a modal dialog (the Keys help) is open for 5 seconds, with a temporary log line per beat on a local build. Record the answer with its date and version in `.claude\rules\terminal-gui.md`. Verify the rule's line names the date, the version and the observation.
- [ ] 5.2 `HeartbeatShould.SetTheBaselineOnTheFirstBeat`: a first `Beat()` counts and records nothing. Red, then `Heartbeat` (250 ms interval) and `StallCounts` green. Verify the fact passes.
- [ ] 5.3 `HeartbeatShould.CountABeatMoreThanAHundredMillisecondsLate` (the spec's "A short stall"): beats 250 ms then 430 ms apart count one stall of 180 ms and record no event; a beat 50 ms late counts nothing. Verify the fact passes.
- [ ] 5.4 `HeartbeatShould.RecordABeatMoreThanHalfASecondLate` (the spec's "A freeze the user feels"): 1,070 ms apart counts one stall of 820 ms and records `UiThreadStalled(820 ms)`, rendered `stalled ui thread for 820 ms` (`DiagnosticLineShould`). Verify the facts and `DiagnosticEventsShould` pass.
- [ ] 5.5 `HeartbeatShould.IgnoreAGapLongerThanThirtySeconds` (the spec's "The laptop sleeps"): 40 minutes apart counts and records nothing, and the next beat 250 ms later is judged from the new baseline. Verify the fact passes.
- [ ] 5.6 Only if 5.1 found that timeouts pause under a modal dialog: `HeartbeatShould.JudgeTheFirstBeatAfterARestartFromTheRestart`, and `DialogEffects` calls `Heartbeat.Restart()` when a dialog closes. Verify the fact and `DialogEffectsShould` pass; otherwise mark this task not needed with the reason.
- [ ] 5.7 `DiagnosticLineShould.RenderTheStallCountsEvenWithNone`: `stalls ui thread: 0 stalls` and `stalls ui thread: 3 stalls, median 180 ms, p95 820 ms, longest 820 ms`; `PerformanceCounts` now carries the real `StallCounts`. Verify the facts pass.
- [ ] 5.8 `ConsoleScreen` adds one `app.AddTimeout(Heartbeat.Interval, ...)` that calls `Beat()`, removed with the other timers. Verify the hand-off check (`dotnet run --project tools\HeadlessTerminal -- check`) and the smoke check of the packed tool both exit 0.
- [ ] 5.9 README "What Vantage records": a UI thread bullet (stalls over 100 ms counted, over 500 ms a line, sleep ignored). Verify by reading against the spec delta.

## 6. The guide, the version and the whole-solution check

- [ ] 6.1 `docs\performance-monitoring.md` gains how to read the new lines (refresh, stall and count lines, pulling each kind out with `Select-String`) and drops its "not yet recorded" notes. Verify by reading it against the design's line formats.
- [ ] 6.2 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next minor preview above the stack's latest at the time (0.21.0-preview.1 if `main` is still at 0.20.0-preview.7). Verify `Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 6.3 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate performance-diagnostics --strict` all pass; then a working day with `VANTAGE_LOG` set on the owner's machine produces stamped count lines every 10 minutes and plausible refresh figures. Record the results here.

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs\diagnostics` describes the timing, heartbeat and periodic counts.
