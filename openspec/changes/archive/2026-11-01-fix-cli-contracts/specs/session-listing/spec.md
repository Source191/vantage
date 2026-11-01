## MODIFIED Requirements

### Requirement: Sessions come from the supported CLI listing
Vantage SHALL obtain the session list by running the Claude Code CLI's JSON listing of active and completed sessions, and SHALL NOT read transcript files or the sessions folder of the Claude configuration directory. A listing that has not returned within the child program time limit SHALL be ended and reported as a failed listing. When the listing command exits with an error, the failure banner SHALL show the CLI's own words and no JSON parser's message.

#### Scenario: Listing succeeds
- **WHEN** the CLI listing returns three sessions
- **THEN** the sessions view shows three rows, each with the session's name, state and working directory

#### Scenario: Listing fails
- **WHEN** the CLI cannot be run or returns invalid JSON
- **THEN** the view keeps the rows it last showed, displays an error banner naming the failure, and does not exit

#### Scenario: Listing hangs
- **WHEN** the CLI listing has not exited after the time limit
- **THEN** the listing's process tree is ended, the view keeps the rows it last showed, the banner says the listing failed and names claude and the limit, and the next timed refresh runs as usual

#### Scenario: The CLI refuses the listing
- **WHEN** the listing command exits with an error and prints "Couldn't connect to the background service" to its error output and nothing to its output
- **THEN** the banner reads listing failed: Couldn't connect to the background service, with no mention of JSON tokens, the view keeps the rows it last showed, and the next refresh runs as usual
