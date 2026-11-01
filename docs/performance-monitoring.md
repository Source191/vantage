# Monitoring Vantage's performance

How to watch Vantage during a normal working day and find what makes it slow. Everything here runs on your own machine and nothing leaves it. There are three layers, from cheapest to most detailed:

1. **All day:** Vantage's own diagnostic log and the .NET runtime counters, both written to files.
2. **When something feels slow:** a short trace of Vantage's threads, or of every process on the machine.
3. **Afterwards:** reading the files to decide what to change.

## Contents

- [What to expect](#what-to-expect)
- [All day: the log and the counters](#all-day-the-log-and-the-counters)
- [When something feels slow: traces](#when-something-feels-slow-traces)
- [Reading the results](#reading-the-results)
- [The lines the log writes](#the-lines-the-log-writes)

## What to expect

Most of Vantage's cost is in the programs it runs, not in its own process:

- **The Sessions tab:** every 5 seconds it runs `claude agents --json --all` (about 1.6 to 2 seconds, and a Node process of about 190 MB while it runs), then reads git and the process table for each row.
- **The Worktrees tab:** every 15 seconds while it is shown, it runs `git worktree list` in each repository under the roots, plus git's status and log for each worktree.
- **Pull requests:** at most once a minute per branch, `gh` or `az` looks up the branch's pull request, four at a time.
- **Vantage itself:** about 35 MB of private memory, flat over hours (measured 29 October 2026). Memory is not worth chasing.

So the first question is usually which child program, how often and how long, and the log answers that.

## All day: the log and the counters

Start Vantage from a plain Windows Terminal tab with the log on. Never start it from inside a Claude Code session: the tabs it opens would inherit Claude Code's environment and run monochrome.

```powershell
$env:VANTAGE_LOG = "$env:TEMP\vantage-$(Get-Date -Format yyyy-MM-dd).log"
vantage
```

The file gets every line of the diagnostic log as it happens: the start (versions and terminal size), each view and dialog, each key's effect with its duration, failures, Terminal.Gui's warnings, any child program slower than 5 seconds, any part of a refresh slower than 3 seconds and any time the screen's thread ran more than half a second late. Every 10 minutes, and again when you quit with `q`, it also gets the counts: the child programs, the refresh parts and the screen thread's stalls, each with the number of runs, the median, the 95th percentile and the longest. Closing the tab without `q` loses at most the last 10 minutes of counts.

In a second tab, record the .NET runtime counters to a CSV file every 10 seconds. `dnx` runs the tool without installing it (.NET 10 SDK); `dotnet tool install --global dotnet-counters` installs it if you prefer. `dnx` keeps a few options for itself, such as `--help` and `--version`, so ask an installed tool for those.

```powershell
$vantage = Get-Process -Name vantage | Sort-Object -Property StartTime | Select-Object -First 1
dnx dotnet-counters collect --process-id $vantage.Id `
    --refresh-interval 10 `
    --counters System.Runtime `
    --format csv `
    --output "$env:TEMP\vantage-counters-$(Get-Date -Format yyyy-MM-dd).csv"
```

Pick the process by id rather than by name: `vantage here` and the hidden `claim-tab` command are also `vantage` processes, and the console is normally the oldest. Press `q` in that tab to stop recording. A day at 10 seconds is a few megabytes.

The counters worth looking at in the CSV:

| Counter | What it tells you |
| --- | --- |
| dotnet.process.cpu.time | Vantage's own CPU; a steady climb while idle means a loop doing too much |
| dotnet.thread_pool.queue.length | Work waiting for a thread; anything above a handful for long means blocking calls |
| dotnet.monitor.lock_contentions | Threads waiting on each other's locks |
| dotnet.exceptions | Exceptions thrown, by type; a steady rate means a failure being retried quietly |
| dotnet.gc.heap.total_allocated | Allocation; its slope matters more than its value |
| dotnet.process.memory.working_set | Should stay flat; a slope is a leak |

Each row names the counter with its unit and attributes, such as `dotnet.process.cpu.time (s / 10 sec)[cpu.mode=user]`, so filter by the start of the name:

```powershell
Import-Csv -Path "$env:TEMP\vantage-counters-$(Get-Date -Format yyyy-MM-dd).csv" |
    Where-Object -Property 'Counter Name' -Like 'dotnet.thread_pool.queue.length*' |
    Sort-Object -Property { [double]$_.'Mean/Increment' } -Descending |
    Select-Object -First 10
```

Make a short note of anything unusual you did (a big build, a rebase across many worktrees, a sleep) and when, so a spike can be matched to a cause.

## When something feels slow: traces

### Vantage's own threads

When a key or a refresh feels slow, take a one-minute trace while you repeat it:

```powershell
$vantage = Get-Process -Name vantage | Sort-Object -Property StartTime | Select-Object -First 1
dnx dotnet-trace collect --process-id $vantage.Id `
    --profile dotnet-sampled-thread-time,dotnet-common `
    --duration 00:00:01:00 `
    --output "$env:TEMP\vantage-$(Get-Date -Format HHmmss).nettrace"
```

The `dotnet-sampled-thread-time` profile samples every thread's stack about a hundred times a second, so it shows where time goes, including time spent waiting; `dotnet-common` adds garbage collection, JIT and exception events. (Older guides name a `cpu-sampling` profile: it has been removed from `dotnet-trace`.)

A minute is about 70 MB. Open the `.nettrace` file in Visual Studio or [PerfView](https://github.com/microsoft/perfview). For a quick answer in the terminal, list the methods where the samples landed:

```powershell
dnx dotnet-trace report "$env:TEMP\vantage-<time>.nettrace" topN --number 25
```

Most samples land in waits (`LowLevelLifoSemaphore.WaitForSignal`, `WaitHandle.WaitOneNoCheck`): idle pool threads, which are not a cost. Look past them, and in Visual Studio or PerfView at the UI thread first (the one in Terminal.Gui's main loop): time it spends in anything but waiting for input is time the screen cannot answer a key. `--inclusive` ranks by time on the stack instead, which is mostly thread start-up frames.

### Every process on the machine

`dotnet-trace` sees only Vantage's own threads, not git, Claude Code, `gh` or `az`. To see them, record with the Windows Performance Recorder, which is part of Windows, from an elevated PowerShell:

```powershell
wpr -start GeneralProfile -filemode
# use Vantage for a few minutes
wpr -stop "$env:TEMP\vantage-$(Get-Date -Format HHmmss).etl"
```

Open the `.etl` file in Windows Performance Analyzer (from the Microsoft Store or the Windows Assessment and Deployment Kit). The Processes table, filtered to children of `vantage.exe`, shows each program's start, lifetime and CPU; it is the best view of how much the refresh cadence costs the whole machine. Keep these recordings short: a few minutes is hundreds of megabytes.

## Reading the results

The log is plain text, one line per event, each starting with its UTC time. A few ways in:

```powershell
$log = "$env:TEMP\vantage-$(Get-Date -Format yyyy-MM-dd).log"

# Every child program that took over 5 seconds, and every refresh part over 3 seconds
Select-String -Path $log -Pattern ' slow ' | ForEach-Object Line

# The slowest key effects
Select-String -Path $log -Pattern ' effect (\S+) -> (\S+) in (\d+) ms' |
    ForEach-Object { [pscustomobject]@{ Effect = $_.Matches.Groups[1].Value; Outcome = $_.Matches.Groups[2].Value; Milliseconds = [int]$_.Matches.Groups[3].Value } } |
    Sort-Object -Property Milliseconds -Descending |
    Select-Object -First 20

# The last set of child program counts
Select-String -Path $log -Pattern ' counts ' | Select-Object -Last 40 | ForEach-Object Line
```

Questions worth answering from a day's files:

- **Which child program costs most:** runs times a typical duration. A program run thousands of times at a modest cost usually beats a rare slow one.
- **Whether a refresh overlaps the next:** a `sessions listing` 95th percentile near or over 5 seconds means the Sessions tab is always refreshing.
- **Whether the screen froze:** the `stalls` line and the `stalled` events, matched against what you were doing.
- **Whether effects are slow or their programs are:** compare a slow effect's time with the slow child lines around it.
- **Whether Vantage itself is busy:** the CPU and thread-pool counters while you were not touching it.

Bring the log, the counters file and any trace to a Vantage change (see [CONTRIBUTING.md](../CONTRIBUTING.md)) rather than tuning by feel; a change should say which figure it moves.

## The lines the log writes

Every line starts with its UTC time; the word after it says what kind of line it is, so one `Select-String` per kind pulls them apart. The counts are written as a set every 10 minutes and at exit, each line stamped with the time of the set; the latest set holds the figures for the whole run so far (the number of runs and the longest over every run, the median and the 95th percentile over each key's latest 1,000).

```
2026-11-01T10:00:00.000Z counts claude agents --json exit 0: 120 runs, median 1650 ms, p95 2100 ms, longest 4900 ms
2026-11-01T10:00:00.000Z refreshes sessions listing: 120 runs, median 1700 ms, p95 2200 ms, longest 5000 ms
2026-11-01T10:00:00.000Z stalls ui thread: 3 stalls, median 180 ms, p95 820 ms, longest 820 ms
2026-11-01T10:03:12.417Z slow refresh worktrees scan in 3400 ms
2026-11-01T10:04:55.002Z stalled ui thread for 820 ms
```

- **counts:** one line per child program, subcommand and exit code.
- **refreshes:** one line per part of a refresh that has run: `sessions listing` (the `claude agents` run), `sessions enrichment` (git, transcripts and processes for the rows), `worktrees scan` (not counting the first scan's wait for the first listing) and `pull-request refresh` (from a refresh that starts a lookup until its last lookup lands). A cancelled or failed part is not counted.
- **stalls:** how late the screen's thread ran a heartbeat it is asked to run four times a second, for every beat more than 100 ms late. The line is written even with `0 stalls`, so a file shows the heartbeat ran. A gap over 30 seconds is the machine sleeping and is ignored.
- **slow refresh** and **stalled:** a single refresh part over 3 seconds, and a single beat more than 500 ms late, as events in the log at the moment they happen.

```powershell
$log = "$env:TEMP\vantage-$(Get-Date -Format yyyy-MM-dd).log"

# How the refresh parts have gone, set by set through the day
Select-String -Path $log -Pattern ' refreshes ' | ForEach-Object Line

# The stall counts, and every freeze with its time
Select-String -Path $log -Pattern ' stalls ui thread' | ForEach-Object Line
Select-String -Path $log -Pattern ' stalled ui thread for (\d+) ms' |
    ForEach-Object { [pscustomobject]@{ At = $_.Line.Substring(0, 24); Milliseconds = [int]$_.Matches.Groups[1].Value } }

# The sessions listing's 95th percentile in each set, to see it drift
Select-String -Path $log -Pattern '^(\S+) refreshes sessions listing: .* p95 (\d+) ms' |
    ForEach-Object { [pscustomobject]@{ At = $_.Matches.Groups[1].Value; NinetyFifth = [int]$_.Matches.Groups[2].Value } }
```

A median near the 95th percentile means a part is steadily that slow; a 95th percentile far above the median means occasional slow runs, worth matching against the `slow` lines and your notes.