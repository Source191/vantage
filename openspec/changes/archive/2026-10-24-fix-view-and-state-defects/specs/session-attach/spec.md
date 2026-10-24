# Spec Delta

## MODIFIED Requirements

### Requirement: Stop and delete resolve the session ID
Stop and delete SHALL act on the session's ID taken from the listing, never on its name, and SHALL act on the row that was selected when the key was pressed even if the listing refreshed before the command ran.

#### Scenario: Stop
- **WHEN** the user presses the stop key on a background session
- **THEN** the CLI stop command runs with that session's ID and the row shows stopped on the next refresh

#### Scenario: Listing refreshes between key and command
- **WHEN** the user presses the stop key and a refresh replaces the rows before the stop command runs
- **THEN** the stop command still runs with the ID of the session that was selected, and nothing fails
