# Spec Delta

## MODIFIED Requirements

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
Vantage SHALL record a diagnostic event when it starts (its version, the Claude Code version, the Windows version, the .NET runtime and the terminal's size), when the user switches view or opens a dialog, and when an effect runs (its kind, the kind of outcome and how long it took). A refresh that succeeds SHALL NOT be recorded.

#### Scenario: An effect runs
- **WHEN** the user stops a session with `x`
- **THEN** the log has a line naming the stop effect, the kind of its outcome and its duration

#### Scenario: The sessions view refreshes
- **WHEN** the sessions view refreshes every 5 seconds for an hour without a failure
- **THEN** the log gains no line for those refreshes

### Requirement: VANTAGE_LOG writes the same events to a file
When `VANTAGE_LOG` names a file, Vantage SHALL append each diagnostic event to it as one line as it is recorded, with exactly the text the in-memory log holds for that event.

#### Scenario: Log variable set
- **WHEN** Vantage runs with `VANTAGE_LOG` set to %TEMP%\vantage.log and the user opens a session in its tab and quits
- **THEN** the file gains the start line and the line naming the open-in-tab effect, each with a UTC time, and the child program counts when Vantage exits
