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
- [What the log does not record yet](#what-the-log-does-not-record-yet)

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

The file gets every line of the diagnostic log as it happens: the start (versions and terminal size), each view and dialog, each key's effect with its duration, failures, Terminal.Gui's warnings and any child program slower than 5 seconds. When you quit with `q` it also gets the child program counts: runs per program, subcommand and exit code, with the longest run. Close Vantage with `q` at the end of the day, or the counts are lost.

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

# Every child program that took over 5 seconds
Select-String -Path $log -Pattern ' slow ' | ForEach-Object Line

# The slowest key effects
Select-String -Path $log -Pattern ' effect (\S+) -> (\S+) in (\d+) ms' |
    ForEach-Object { [pscustomobject]@{ Effect = $_.Matches.Groups[1].Value; Outcome = $_.Matches.Groups[2].Value; Milliseconds = [int]$_.Matches.Groups[3].Value } } |
    Sort-Object -Property Milliseconds -Descending |
    Select-Object -First 20

# The child program counts written when Vantage quit
Select-String -Path $log -Pattern '^counts ' | ForEach-Object Line
```

Questions worth answering from a day's files:

- **Which child program costs most:** runs times a typical duration. A program run thousands of times at a modest cost usually beats a rare slow one.
- **Whether a refresh overlaps the next:** a `claude agents` run near or over 5 seconds means the Sessions tab is always refreshing.
- **Whether effects are slow or their programs are:** compare a slow effect's time with the slow child lines around it.
- **Whether Vantage itself is busy:** the CPU and thread-pool counters while you were not touching it.

Bring the log, the counters file and any trace to a Vantage change (see [CONTRIBUTING.md](../CONTRIBUTING.md)) rather than tuning by feel; a change should say which figure it moves.

## What the log does not record yet

The OpenSpec change `performance-diagnostics` (in `openspec\changes`) adds what this guide cannot yet get from the log. Until it is implemented:

- **Refreshes are not timed:** there is no line for how long a Sessions refresh or a Worktrees scan takes, or which part of it. Use the `slow` lines and a trace.
- **UI stalls are not detected:** a frozen screen shows only as a gap in what you did. Use a trace taken while it happens.
- **Typical durations are not kept:** the counts hold the longest run only, not the median or the 95th percentile.
- **Counts are written only at exit:** closing the tab or a crash before `q` loses them (a crash writes them to `last-crash.log` instead).

Once the change is implemented, this section is replaced by how to read the new lines.
