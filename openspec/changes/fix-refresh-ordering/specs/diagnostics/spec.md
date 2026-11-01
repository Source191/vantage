## MODIFIED Requirements

### Requirement: Child programs are counted
Vantage SHALL count every child program it runs by program (git, claude, gh, az, pwsh, wt, vswhere, dotnet, or another program), its subcommand when the leading arguments are words on Vantage's fixed list for that program, and its exit code, keeping the number of runs and the longest duration. A run that fails to start, takes longer than 5 seconds or is ended at its time limit SHALL also be recorded as a line. The counts SHALL be part of the crash file and SHALL be written to the `VANTAGE_LOG` file when Vantage exits.

#### Scenario: Worktree lists are read
- **WHEN** a scan runs `git worktree list --porcelain` in three repositories and each exits 0
- **THEN** the counts hold git `worktree list` exit 0 three times with the longest duration in milliseconds, and the log gains no line for them

#### Scenario: A slow child
- **WHEN** `claude agents --json` takes 6.2 seconds
- **THEN** the log has a line naming claude, `agents`, exit code 0 and 6200 ms

#### Scenario: A child ended at its limit
- **WHEN** `claude agents --json` is ended after 60 seconds without exiting
- **THEN** the log has a line saying claude `agents` timed out after 60000 ms, with no exit code and nothing of its output, and it is not recorded as a program that failed to start

#### Scenario: A program Vantage has no word for
- **WHEN** Vantage runs C:\Users\me\tools\MyTool.exe with the argument C:\Users\me\source\repos\Northwind.Orders
- **THEN** the counts name it as another program with no subcommand, and no part of its path or arguments is recorded

#### Scenario: An argument that is not on the list
- **WHEN** Vantage runs `git branch -d fix-123`
- **THEN** the counts name git `branch` and record neither `-d` nor `fix-123`
