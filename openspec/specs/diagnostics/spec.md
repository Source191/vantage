# diagnostics Specification

## Purpose
Records what Vantage itself did, so a maintainer can diagnose a problem, while guaranteeing that nothing about the user, their machine or the code in their repositories is ever recorded.

## Requirements

### Requirement: Nothing personal is ever recorded
Every diagnostic record, in memory, in the `VANTAGE_LOG` file and in the crash file, SHALL hold only facts defined in Vantage's own code (kinds, fixed program and subcommand names, exit codes, durations, counts, versions, exception summaries). No record SHALL hold a path, a repository, branch, worktree or session name, an id, a prompt, pull-request detail, a file name, any program's output, a user or machine name, or anything typed.

#### Scenario: A session is attached
- **WHEN** the user opens the session "Fix flaky tests" (job 81cdb0f9) running in C:\Users\me\source\worktrees\Northwind.Orders-fix-123 in its own tab
- **THEN** the log records that the open-in-tab effect ran with its outcome kind, and neither "Fix flaky tests", "81cdb0f9", "Northwind.Orders" nor "C:\Users\me" appears in it

#### Scenario: git fails with a message naming the repository
- **WHEN** `git worktree remove` fails in C:\Users\me\source\repos\Northwind.Orders with "fatal: 'C:\Users\me\source\worktrees\Northwind.Orders-fix-123' contains modified or untracked files"
- **THEN** the counts hold git `worktree remove` exit 128, the log records that the remove effect failed with a `GitException`, and no part of git's message, the folder or the repository's name appears in either

#### Scenario: Text typed in a dialog
- **WHEN** the user types a session name and an opening prompt in the new session wizard and starts the session
- **THEN** the log records that the wizard opened and that a start effect ran with its outcome kind, and neither the name nor the prompt appears in it

#### Scenario: Terminal.Gui reports input it could not parse
- **WHEN** Terminal.Gui logs a warning that quotes an unrecognised input sequence
- **THEN** the log records a Terminal.Gui warning with no text from Terminal.Gui's message

### Requirement: Vantage records its own behaviour
Vantage SHALL record a diagnostic event when it starts (its version, the Claude Code version, the Windows version, the .NET runtime and the terminal's size), when the user switches view or opens a dialog, and when an effect runs (its kind, the kind of outcome and how long it took). A refresh that succeeds within 3 seconds SHALL NOT be recorded as an event; it is only counted.

#### Scenario: An effect runs
- **WHEN** the user stops a session with `x`
- **THEN** the log has a line naming the stop effect, the kind of its outcome and its duration

#### Scenario: The sessions view refreshes
- **WHEN** the sessions view refreshes every 5 seconds for an hour without a failure and each part of each refresh takes under 3 seconds
- **THEN** the log gains no event for those refreshes, and the refresh counts hold about 720 runs of the sessions listing and of the sessions enrichment

### Requirement: Child programs are counted
Vantage SHALL count every child program it runs by program (git, claude, gh, az, pwsh, wt, vswhere, dotnet, or another program), its subcommand when the leading arguments are words on Vantage's fixed list for that program, and its exit code, keeping the number of runs, the median and 95th percentile duration of the latest 1,000 runs, and the longest duration. A run that fails to start or takes longer than 5 seconds SHALL also be recorded as a line.

#### Scenario: Worktree lists are read
- **WHEN** a scan runs `git worktree list --porcelain` in three repositories and each exits 0
- **THEN** the counts hold git `worktree list` exit 0 three times with the median, the 95th percentile and the longest duration in milliseconds, and the log gains no line for them

#### Scenario: Typical and worst runs differ
- **WHEN** `claude agents --json` exits 0 one hundred times, ninety-nine of them in 1,600 ms and one in 4,900 ms
- **THEN** the counts hold claude `agents` exit 0 with 100 runs, median 1600 ms, 95th percentile 1600 ms and longest 4900 ms

#### Scenario: More runs than the sample holds
- **WHEN** git `status` exits 0 for the 1,001st time
- **THEN** its count says 1001 runs, its longest is the longest of all 1,001, and its median and 95th percentile are taken over the latest 1,000

#### Scenario: A slow child
- **WHEN** `claude agents --json` takes 6.2 seconds
- **THEN** the log has a line naming claude, `agents`, exit code 0 and 6200 ms

#### Scenario: A program Vantage has no word for
- **WHEN** Vantage runs C:\Users\me\tools\MyTool.exe with the argument C:\Users\me\source\repos\Northwind.Orders
- **THEN** the counts name it as another program with no subcommand, and no part of its path or arguments is recorded

#### Scenario: An argument that is not on the list
- **WHEN** Vantage runs `git branch -d fix-123`
- **THEN** the counts name git `branch` and record neither `-d` nor `fix-123`

### Requirement: Failures are recorded with a summary
When an effect, a view's load, a dialog flow, the update check or a background task fails, Vantage SHALL record the failure with an exception summary: the type of the exception and of each inner exception, and the stack frames as method names with the source file's name and line where known, without the exception's message, any folder in a file path or any value. The screen SHALL show the failure as it does today.

#### Scenario: A load fails
- **WHEN** the sessions view's load throws an `InvalidOperationException` whose message names a folder
- **THEN** the status line shows the message as before, and the log records a load failure of the sessions view with `InvalidOperationException` and its frames, without the message

#### Scenario: Stack frames of a local build
- **WHEN** a Vantage built in C:\Users\me\source\repos\vantage fails inside `GitCli.RunOrThrowAsync`
- **THEN** the frame reads with the method name and `GitCli.cs` and its line, and "C:\Users\me" does not appear

### Requirement: The log is always on and bounded
Vantage SHALL keep the most recent 500 diagnostic events in memory from start to exit, whether or not `VANTAGE_LOG` is set, dropping the oldest first. Each event SHALL carry its UTC time. The in-memory log SHALL never be written to disc except as the crash file or through `VANTAGE_LOG`.

#### Scenario: More events than the log holds
- **WHEN** Vantage records its 501st event
- **THEN** the in-memory log holds the latest 500 and the first is gone

#### Scenario: No log variable
- **WHEN** Vantage runs without `VANTAGE_LOG`
- **THEN** it writes no log file and still keeps the in-memory log

### Requirement: VANTAGE_LOG writes the same events to a file
When `VANTAGE_LOG` names a file, Vantage SHALL append each diagnostic event to it as one line as it is recorded, with exactly the text the in-memory log holds for that event.

#### Scenario: Log variable set
- **WHEN** Vantage runs with `VANTAGE_LOG` set to %TEMP%\vantage.log and the user opens a session in its tab and quits
- **THEN** the file gains the start line and the line naming the open-in-tab effect, each with a UTC time, and the child program, refresh and stall counts when Vantage exits, each count line with the UTC time it was written

#### Scenario: Vantage runs for a working day
- **WHEN** Vantage runs with `VANTAGE_LOG` set from 09:00 to 17:30 UTC and is then ended by closing its tab
- **THEN** the file holds the counts written at 09:10, 09:20 and every 10 minutes until 17:30, so the figures up to the last 10 minutes survive without an exit

### Requirement: A crash leaves a crash file
When an exception ends Vantage, it SHALL write the in-memory log followed by the crash's exception summary to `%LOCALAPPDATA%\Vantage\last-crash.log`, replacing any earlier crash file, before it exits. A failure to write the crash file SHALL NOT hide the original exception.

#### Scenario: Unhandled exception
- **WHEN** an exception escapes the console and ends Vantage
- **THEN** `last-crash.log` holds the events recorded before it, the child program, refresh and stall counts, and the crash's summary, and the exception is still reported as today

#### Scenario: Normal exit
- **WHEN** the user quits Vantage with `q`
- **THEN** no crash file is written and an existing one is left as it was

### Requirement: Refreshes are timed
Vantage SHALL count each part of a refresh by part (the sessions listing, the sessions enrichment, the worktrees scan, the pull-request refresh), keeping the number of completed runs, the median and 95th percentile of the latest 1,000, and the longest. A part that takes longer than 3 seconds SHALL also be recorded as a line naming the part and its duration. A cancelled part SHALL NOT be counted; a failed part is recorded as a failure as today.

#### Scenario: A slow scan
- **WHEN** the worktrees scan takes 3.4 seconds
- **THEN** the refresh counts gain a worktrees scan run and the log has a line naming the worktrees scan and 3400 ms, with no folder, repository or worktree name

#### Scenario: The scan waits for the first listing
- **WHEN** the first worktrees scan waits 9 seconds for the sessions tab's first listing and then scans for 0.8 seconds
- **THEN** the worktrees scan is counted as 800 ms and no slow line is recorded for it

#### Scenario: A refresh is cancelled
- **WHEN** the user leaves the Worktrees tab while a scan is running and the scan is cancelled
- **THEN** the refresh counts do not change and no line is recorded

### Requirement: The UI thread is watched
Vantage SHALL measure how late the UI thread runs a regular heartbeat. A beat more than 100 ms late SHALL be counted as a stall, keeping the number, the median and 95th percentile of the latest 1,000 and the longest; a beat more than 500 ms late SHALL also be recorded as a line with its lateness. A gap longer than 30 seconds SHALL be taken as the machine sleeping and neither counted nor recorded.

#### Scenario: A short stall
- **WHEN** the UI thread runs a beat 180 ms late
- **THEN** the stall counts gain one stall of 180 ms and the log gains no line

#### Scenario: A freeze the user feels
- **WHEN** the UI thread runs a beat 820 ms late
- **THEN** the stall counts gain one stall of 820 ms and the log has a line saying the UI thread stalled for 820 ms

#### Scenario: The laptop sleeps
- **WHEN** the machine sleeps for 40 minutes and the next beat runs on waking
- **THEN** no stall is counted and no line is recorded

### Requirement: Counts are written while Vantage runs
When `VANTAGE_LOG` names a file, Vantage SHALL write the child program, refresh and stall counts to it every 10 minutes while it runs, as well as when it exits, each count line carrying the UTC time it was written. The counts SHALL NOT be kept as events in the in-memory log, and nothing SHALL be written when `VANTAGE_LOG` is not set.

#### Scenario: Ten minutes pass
- **WHEN** Vantage has run for 10 minutes with `VANTAGE_LOG` set
- **THEN** the file gains every count line with that moment's UTC time, and the in-memory log holds no count line

#### Scenario: No log variable
- **WHEN** Vantage runs for an hour without `VANTAGE_LOG`
- **THEN** no file is written and the counts are still kept for the crash file
