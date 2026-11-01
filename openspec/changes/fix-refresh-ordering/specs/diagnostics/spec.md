## MODIFIED Requirements

### Requirement: Child programs are counted
Vantage SHALL count every child program it runs by program (git, claude, gh, az, pwsh, wt, vswhere, dotnet, or another program), its subcommand when the leading arguments are words on Vantage's fixed list for that program, and its exit code, keeping the number of runs, the median and 95th percentile duration of the latest 1,000 runs, and the longest duration. A run that fails to start, takes longer than 5 seconds or is ended at its time limit SHALL also be recorded as a line.

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

#### Scenario: A child ended at its limit
- **WHEN** `claude agents --json` is ended after 60 seconds without exiting
- **THEN** the log has a line saying claude `agents` timed out after 60000 ms, with no exit code and nothing of its output, it is not recorded as a program that failed to start, and the counts gain no run for it

#### Scenario: A program Vantage has no word for
- **WHEN** Vantage runs C:\Users\me\tools\MyTool.exe with the argument C:\Users\me\source\repos\Northwind.Orders
- **THEN** the counts name it as another program with no subcommand, and no part of its path or arguments is recorded

#### Scenario: An argument that is not on the list
- **WHEN** Vantage runs `git branch -d fix-123`
- **THEN** the counts name git `branch` and record neither `-d` nor `fix-123`
