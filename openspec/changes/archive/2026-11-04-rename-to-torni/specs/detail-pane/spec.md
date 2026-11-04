## RENAMED Requirements

- FROM: `### Requirement: The last prompt is the transition Vantage saw`
- TO: `### Requirement: The last prompt is the transition Torni saw`

## MODIFIED Requirements

### Requirement: The pane shows the selected session
On the Sessions tab the pane SHALL show what the selected row does not, titled with its name: what it waits for, when it started and was last prompted, the full folder, the session id, the worktree, the branch, its tab, the folder's changes and its memory. A fact that cannot be read SHALL show as unavailable without hiding the rest.

#### Scenario: A session at a permission prompt
- **WHEN** fix-123 is selected, listed as waiting for a permission prompt, started at 14:02 today in a Torni tab coloured yellow, in the worktree Northwind.Orders-fix-123 on branch fix-123 with three changed files
- **THEN** the pane is titled fix-123 and reads Waiting permission prompt, Started 14:02 (5m ago), Worktree fix-123, Branch fix-123, Tab open, yellow, and Changes 3 changed files · 0 unpushed commits with the last commit's age

#### Scenario: A session never sent a prompt
- **WHEN** a session that needs input and has no transcript is selected
- **THEN** the pane's Waiting line reads a first prompt

#### Scenario: A working session
- **WHEN** a working session is selected
- **THEN** the pane's Waiting line reads nothing

#### Scenario: The main checkout
- **WHEN** a session running in C:\Users\me\source\repos\Northwind.Orders on branch main is selected
- **THEN** the pane reads Worktree main checkout and Branch main

#### Scenario: Memory in the pane
- **WHEN** a running session whose tree uses 903 MB is selected
- **THEN** the pane's Memory line reads 903 MB and names m for the breakdown

#### Scenario: A stopped session
- **WHEN** a stopped session is selected
- **THEN** the pane shows its folder and branch and reads nothing to measure for its memory

#### Scenario: A terminal session
- **WHEN** a plain claude session in its own terminal is selected
- **THEN** the pane shows its facts and its Tab line reads its own terminal

### Requirement: The last prompt is the transition Torni saw
The pane's Prompted line SHALL show the most recent time, while this Torni ran, that the session left needs input or idle for working, which is when a prompt or an answer reached it. When Torni has not seen such a transition for the session, the line SHALL say so rather than guess, and the transcript SHALL NOT be read for it.

#### Scenario: An answer reaches the session
- **WHEN** fix-123 was needs input at one refresh and working at the next, at 14:03
- **THEN** its Prompted line reads 14:03 and the minutes since

#### Scenario: Not seen
- **WHEN** a session has been idle since Torni started
- **THEN** its Prompted line reads not seen since Torni started

### Requirement: The pane shows the selected worktree
On the Worktrees tab the pane SHALL show, for the selected row and titled with its folder's name: the health with its reason in full, the branch and its merge state against origin's default branch, the pull request, the full folder, whether and when Torni created it, its changed files and unpushed commits, and beneath them its unpushed commits as far as five, or its changed files when none are unpushed, then how many more.

#### Scenario: A worktree with unpushed commits
- **WHEN** Northwind.Billing-invoice-pdf, created by Torni on 23 October, with twelve unpushed commits on invoice-pdf, is selected
- **THEN** the pane reads Health ok, Branch invoice-pdf, not merged into origin/main, Created by Torni, 23 Oct 09:14, Changes 0 changed files · 12 unpushed commits, lists the five newest commits with hash, age and subject, and ends with 7 more

#### Scenario: A worktree with changes and nothing unpushed
- **WHEN** a worktree with two changed files and no unpushed commits is selected
- **THEN** the pane lists the two changed files in place of commits

#### Scenario: A broken leftover
- **WHEN** a broken leftover folder is selected
- **THEN** the pane's Health line gives the reason in full, and the git lines read unavailable

#### Scenario: A locked worktree
- **WHEN** a worktree git reports as locked with a reason is selected
- **THEN** the pane's Health line reads locked and the reason

### Requirement: The pane's times are local
Every time of day and date the pane shows (Started, Prompted and Created) SHALL be in the user's local time zone, using the offset that zone had at that moment, not the offset it has now. A time SHALL be shown as today, by hour and minute alone, only when it falls on the current local date. How long ago a time was SHALL NOT depend on the time zone.

#### Scenario: A summer start in the UK
- **WHEN** the local time zone is the UK's, it is 14:37 local time on 15 July and fix-123 started at 13:32 UTC that day
- **THEN** the pane reads Started 14:32 (5m ago)

#### Scenario: Before the clocks went back
- **WHEN** the local time zone is the UK's, it is 1 November and a worktree was created by Torni at 08:14 UTC on 23 October
- **THEN** the pane reads Created by Torni, 23 Oct 09:14

#### Scenario: Today is the local date
- **WHEN** the local time zone is Nairobi's (UTC+3), it is 03:30 local time on 2 November (00:30 UTC) and a session started at 21:30 UTC on 1 November
- **THEN** the pane reads Started 00:30 (3h00m ago), as a time today, though the two times fall on different UTC dates
