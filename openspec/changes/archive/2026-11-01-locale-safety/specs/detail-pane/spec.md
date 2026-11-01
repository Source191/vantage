## ADDED Requirements

### Requirement: The pane's times are local
Every time of day and date the pane shows (Started, Prompted and Created) SHALL be in the user's local time zone, using the offset that zone had at that moment, not the offset it has now. A time SHALL be shown as today, by hour and minute alone, only when it falls on the current local date. How long ago a time was SHALL NOT depend on the time zone.

#### Scenario: A summer start in the UK
- **WHEN** the local time zone is the UK's, it is 14:37 local time on 15 July and fix-123 started at 13:32 UTC that day
- **THEN** the pane reads Started 14:32 (5m ago)

#### Scenario: Before the clocks went back
- **WHEN** the local time zone is the UK's, it is 1 November and a worktree was created by Vantage at 08:14 UTC on 23 October
- **THEN** the pane reads Created by Vantage, 23 Oct 09:14

#### Scenario: Today is the local date
- **WHEN** the local time zone is Nairobi's (UTC+3), it is 03:30 local time on 2 November (00:30 UTC) and a session started at 21:30 UTC on 1 November
- **THEN** the pane reads Started 00:30 (3h00m ago), as a time today, though the two times fall on different UTC dates

### Requirement: Changed files are listed by their names
When the pane lists a worktree's changed files, each SHALL be shown by its path as named, whatever letters it holds, never as git's quoted escape of the name.

#### Scenario: An accented file name
- **WHEN** a worktree with no unpushed commits has the new file naïve.txt
- **THEN** the pane lists naïve.txt
