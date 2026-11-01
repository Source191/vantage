# Proposal

## Why

Vantage can be watched from outside (`dotnet-counters`, `dotnet-trace`, the Windows Performance Recorder), but its own diagnostic log cannot answer the questions a day of real use raises: how long a Sessions refresh or a Worktrees scan takes and which part of it, whether the UI thread ever stalls, and what a typical child program run costs rather than only the longest. The process counts are also written only when Vantage exits, so a day's figures are lost when the terminal is closed or the process is killed. The owner wants to run Vantage with `VANTAGE_LOG` set during day-to-day work and find the bottlenecks from the file.

## What Changes

- **Refreshes are timed:** each part of a refresh (the Sessions tab's `claude agents` listing, the Sessions rows' enrichment, the Worktrees scan, the pull-request catalogue's refresh) is counted by part, with the number of runs, the median, the 95th percentile and the longest duration. A part that takes longer than 3 seconds is also recorded as a line. A refresh that succeeds in time still adds no line, so the 500-event log is not filled by the 5-second refresh.
- **The UI thread is watched:** a heartbeat on the UI thread measures how late it runs. A beat more than 100 ms late is counted as a stall, with the same median, 95th percentile and longest; one more than 500 ms late is also recorded as a line. A gap over 30 seconds is taken as the machine sleeping and is neither counted nor recorded.
- **Child program counts gain typical times:** each program, subcommand and exit code keeps the median and 95th percentile of its latest 1,000 runs beside the number of runs and the longest.
- **Counts are written while Vantage runs:** when `VANTAGE_LOG` is set, the process, refresh and stall counts are written to the file every 10 minutes as well as at exit, each count line carrying the UTC time it was written. The in-memory log still keeps events only.
- **Guide:** `docs\performance-monitoring.md`, linked from `CONTRIBUTING.md`, says how to monitor Vantage during a working day with the tools that exist today (this pull request), and gains the new lines when this change is implemented.

Out of scope: a live view of the figures inside Vantage, any change to refresh cadence or to how Vantage runs git or the Claude CLI (this change only measures; what to change follows from the figures), exporting through `System.Diagnostics.Metrics` or OpenTelemetry, timing the detail pane's loads, telemetry of any kind leaving the machine, memory figures (decided on 29 October 2026 not to be worth pursuing), and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `diagnostics`: the behaviour requirement no longer forbids every refresh record (a refresh is counted, and a slow one recorded as a line); the child program counts gain the median and 95th percentile and are written every 10 minutes; new requirements for refresh timing and the UI-thread heartbeat; the `VANTAGE_LOG` requirement's scenario covers the periodic counts.

## Impact

- `Application\Diagnostics`: a duration sample with median and 95th percentile (shared by process, refresh and stall counts), `ProcessCounts` keeping it, new `RefreshCounts` with a `RefreshPart` enum and a timing wrapper, `RefreshSlow`, `UiThreadStalled` events, a `Heartbeat` that judges lateness from `TimeProvider`, a periodic counts writer on `TimeProvider.CreateTimer`, and `DiagnosticLine` rendering the new lines and the time on count lines.
- `DiagnosticLog.WriteCounts` and `CrashRecorder` take the refresh and stall counts too. `ProblemReports` keeps only the child program counts, which it renders as the log does, so its count lines gain the median, the 95th percentile and the time; its 7,500-character trimming is unchanged.
- `Tui\ConsoleScreen` adds one `app.AddTimeout` heartbeat that calls the Application `Heartbeat`.
- `Program.cs` wraps the listing, enrichment and scan in the timing wrapper and starts the periodic writer; `WorktreeInventory` or `PullRequestCatalogue` times the pull-request refresh it starts.
- `DiagnosticEventsShould` and `DiagnosticsPrivacyShould` cover the new events and lines; README's "What Vantage records"; `docs\performance-monitoring.md`; `Version` raised (minor: new behaviour, no key changes).
