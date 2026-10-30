# Spec Delta

## ADDED Requirements

### Requirement: Automated runs are not sessions
An interactive entry in the CLI listing that has no status SHALL be treated as an automated run (a `claude` started through the Agent SDK or `claude -p`), not a session: it SHALL NOT be a row, SHALL NOT be counted in the table's title and SHALL take no keys. The decision SHALL use only the listing's fields. Every other entry SHALL be listed as before.

#### Scenario: A plugin's background review
- **WHEN** the listing holds the background session fix-123 and an interactive entry with a process, fix-123's folder, no name and no status
- **THEN** the table shows one row, fix-123, and its title starts 1 session

#### Scenario: A named automated run
- **WHEN** the listing holds an interactive entry with a process, the name tmp-9d and no status, as `claude -p` gives
- **THEN** no row shows for it

#### Scenario: A real interactive session
- **WHEN** the listing holds an interactive entry with a process and the status idle
- **THEN** it shows as an interactive session row, as before

#### Scenario: A real interactive session that has just started
- **WHEN** a `claude` started in a terminal is listed in the moment before it has a status, and has the status idle at the next refresh
- **THEN** it shows no row for that one listing and shows as an interactive session row from the next

#### Scenario: A stopped background session
- **WHEN** the listing holds a background entry with no process and no status
- **THEN** it shows as a stopped row, as before

### Requirement: The counts show the automated runs
While any automated run is listed, the counts beside the tabs SHALL end with a grey ◇, how many runs, the word automated and the memory of their process trees together, measured and written as the detail pane's Memory line measures and writes a session's. With none listed nothing SHALL show for them.

#### Scenario: One run beside two sessions
- **WHEN** one session is working, one is stopped and one automated run's tree uses 216 MB
- **THEN** the counts read ● 1 working · ■ 1 stopped · ◇ 1 automated 216 MB, the last in grey

#### Scenario: Two runs
- **WHEN** two automated runs use 215 MB and 216 MB
- **THEN** the counts end ◇ 2 automated 431 MB

#### Scenario: A run whose process has gone
- **WHEN** an automated run's process ends between the listing and the measurement
- **THEN** the run is still counted and adds nothing to the memory

#### Scenario: No runs
- **WHEN** no automated run is listed
- **THEN** the counts show only the sessions' states, and with no session listed no counts show

#### Scenario: The listing fails
- **WHEN** a refresh fails after a listing that held one automated run
- **THEN** the counts keep ◇ 1 automated with its memory, as the rows keep what they last showed
