# Spec Delta

## MODIFIED Requirements

### Requirement: Session state is shown in six display states
Vantage SHALL map each session's process presence, status, waitingFor and state fields to exactly one of: working, needs input, idle, completed, failed, stopped. A row without a process identifier is stopped or failed, never working. A background session with a process identifier and no status yet is starting up, and SHALL show the word starting in the working state.

#### Scenario: Busy session
- **WHEN** a session reports status busy and state working
- **THEN** its row shows working

#### Scenario: Waiting on a permission prompt
- **WHEN** a session reports status waiting, waitingFor "permission prompt" and state blocked
- **THEN** its row shows needs input and the peek text names the permission prompt

#### Scenario: Idle session that has not started
- **WHEN** a session reports status idle and state blocked
- **THEN** its row shows needs input

#### Scenario: Finished session with a live process
- **WHEN** a session reports status idle and state done
- **THEN** its row shows completed

#### Scenario: Deliberately stopped session
- **WHEN** a session row has no process identifier and no status, and its state is done or blocked
- **THEN** its row shows stopped

#### Scenario: Background session starting up
- **WHEN** a background session reports a process identifier, no status and state blocked
- **THEN** its row shows starting in the working colour and glyph, never "/blocked"

#### Scenario: Unknown combination
- **WHEN** a session reports a status or state Vantage does not recognise
- **THEN** its row shows the raw value rather than guessing a display state

### Requirement: The header summarises sessions by state
The table's title beside the tabs SHALL count the sessions listed, and the counts beside the tabs SHALL show one glyph, count and state word per state present, needs input first; with no session listed the title SHALL read 0 sessions and no counts SHALL show. Sessions starting up SHALL be counted with the working ones under the word working.

#### Scenario: Mixed states
- **WHEN** nine sessions are listed, four of them stopped and the rest needing input
- **THEN** the title starts 9 sessions and the counts read ◐ 5 need input · ■ 4 stopped

#### Scenario: Every session needs input
- **WHEN** six sessions are listed and every one needs input
- **THEN** the title starts 6 sessions and the counts read ◐ 6 need input

#### Scenario: No sessions
- **WHEN** no session is listed
- **THEN** the title reads 0 sessions and no counts show

#### Scenario: Starting and working together
- **WHEN** one session is starting up and one is working, the starting one first in the table
- **THEN** the counts read ● 2 working
