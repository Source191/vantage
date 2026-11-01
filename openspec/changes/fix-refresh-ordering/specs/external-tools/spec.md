## ADDED Requirements

### Requirement: A captured child program is given a time limit
Every child program Vantage runs for its output SHALL be ended, with every process it started, when it has not exited within 60 seconds or when the part of Vantage that ran it gives up on it, and SHALL then be reported as a failure naming the program and the limit, never as a success with empty output. A program Vantage hands the terminal to SHALL have no time limit.

#### Scenario: A listing that never exits
- **WHEN** `claude agents --json --all` has not exited after 60 seconds
- **THEN** claude and every process it started are ended, and the caller is told claude.exe did not finish within 60 seconds

#### Scenario: A version check given up
- **WHEN** `claude --version` has not answered within the five seconds the console allows it as it opens
- **THEN** claude is ended and the settings row shows the version as unread

#### Scenario: A child that finishes in time
- **WHEN** `git worktree list --porcelain` exits after two seconds
- **THEN** its output and exit code are returned as before, and nothing is ended

#### Scenario: An attach is never limited
- **WHEN** `vantage here` attaches to a session and the user works in it for an hour
- **THEN** the attach runs until the user detaches or the session ends
