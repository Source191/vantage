# Spec Delta

## Purpose
A pane under each table that shows the selected session or worktree in full, with its recent terminal output, so the user reads why a session waits and what a worktree holds without attaching or leaving the console.

## ADDED Requirements

### Requirement: The pane shows the selected session
On the Sessions tab the pane SHALL show, for the selected row: the state with the reason it waits, when it started, when it was last prompted, the session id, the full folder, the branch, the pull request with the key that opens it, its tab, the worktree's changed files, unpushed commits and last commit, and its memory with the key for the breakdown. A fact that cannot be read SHALL show as unavailable without hiding the rest.

#### Scenario: A session at a permission prompt
- **WHEN** fix-123 is selected, listed as waiting for a permission prompt, started at 14:02 today in a Vantage tab coloured yellow, in a worktree with three changed files
- **THEN** the pane is titled fix-123 and reads State needs input (permission prompt), Started 14:02 (5m ago), Tab open, yellow, and Worktree 3 changed files · 0 unpushed commits with the last commit's age

#### Scenario: A session never sent a prompt
- **WHEN** an idle, blocked session with no transcript is selected
- **THEN** the pane's State line reads needs input (no prompt sent yet)

#### Scenario: Memory in the pane
- **WHEN** a running session whose tree uses 903 MB is selected
- **THEN** the pane's Memory line reads 903 MB and names m for the breakdown

#### Scenario: A stopped session
- **WHEN** a stopped session is selected
- **THEN** the pane shows its state, folder, branch and pull request, and reads nothing to measure for its memory and no output for a session without a process

#### Scenario: A terminal session
- **WHEN** a plain claude session in its own terminal is selected
- **THEN** the pane shows its facts, its Tab line reads its own terminal, and the output area says a session outside the supervisor has no output to show

### Requirement: The last prompt is the transition Vantage saw
The pane's Prompted line SHALL show the most recent time, while this Vantage ran, that the session left needs input or idle for working, which is when a prompt or an answer reached it. When Vantage has not seen such a transition for the session, the line SHALL say so rather than guess, and the transcript SHALL NOT be read for it.

#### Scenario: An answer reaches the session
- **WHEN** fix-123 was needs input at one refresh and working at the next, at 14:03
- **THEN** its Prompted line reads 14:03 and the minutes since

#### Scenario: Not seen
- **WHEN** a session has been idle since Vantage started
- **THEN** its Prompted line reads not seen since Vantage started

### Requirement: The pane shows the session's recent output
The pane SHALL show the last lines of the selected background session's terminal output, obtained from the Claude Code CLI's logs command, with terminal escape sequences removed, as many lines as the pane has room for, refreshed with the rows for the selected row only. The output SHALL be drawn and discarded: never written to the diagnostic log, never part of a problem report, never kept after the selection moves.

#### Scenario: A question is on screen
- **WHEN** fix-123 has paused on "Allow edit to Processor.cs?" and is selected
- **THEN** the output area shows that question and its options as the session's screen shows them, without colour codes

#### Scenario: The output stays off the record
- **WHEN** the pane has shown a session's output and the user opens the problem report
- **THEN** no line of that output is in the report or the diagnostic log; the log records only that the logs command ran, how long it took and its exit code

#### Scenario: Logs fail
- **WHEN** the logs command fails or cannot be run
- **THEN** the output area says the output is unavailable and the rest of the pane is unaffected

### Requirement: The pane shows the selected worktree
On the Worktrees tab the pane SHALL show, for the selected row: the health with its reason in full, whether and when Vantage created it, the full folder, the branch and its merge state against origin's default branch, the pull request, the risk line, the session inside it with the key that goes to it, and its unpushed commits as far as they fit, or its changed files when none are unpushed.

#### Scenario: A worktree with unpushed commits
- **WHEN** Northwind.Billing-invoice-pdf, created by Vantage on 23 October, with twelve unpushed commits on invoice-pdf, is selected
- **THEN** the pane reads Health ok, Created by Vantage, 23 Oct 09:14, Branch invoice-pdf, not merged into origin/main, Risk 0 changed files · 12 unpushed commits, lists the newest commits with hash, age and subject, and ends with how many more

#### Scenario: A worktree with changes and nothing unpushed
- **WHEN** a worktree with five changed files and no unpushed commits is selected
- **THEN** the pane lists the five changed files in place of commits

#### Scenario: A broken leftover
- **WHEN** a broken leftover folder is selected
- **THEN** the pane's Health line gives the reason in full, and the git lines read unavailable

### Requirement: The pane is sized by the user
The pane SHALL take nine rows under the table by default on a 36-row terminal, SHALL be resizable by dragging the divider between the table and the pane with the mouse, and SHALL keep its size while the console runs.

#### Scenario: Drag the divider
- **WHEN** the user drags the divider up by four rows
- **THEN** the pane shows four more lines of output and the table four fewer rows, on both tabs, until Vantage quits

### Requirement: Detail never delays the listing
Reading a row's detail SHALL run off the UI thread, for the selected row only, when the selection changes and after each refresh, and SHALL NOT delay a refresh of the table; a detail that arrives after the selection has moved on SHALL be dropped.

#### Scenario: Slow git
- **WHEN** the merge check for the selected worktree takes four seconds
- **THEN** the table refreshes on time and the pane's Branch line fills in when the answer arrives

#### Scenario: Selection moves while loading
- **WHEN** the user moves the selection before the previous row's detail has arrived
- **THEN** the pane shows the new row's detail only
