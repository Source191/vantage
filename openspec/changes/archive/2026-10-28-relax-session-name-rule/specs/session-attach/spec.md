# Spec Delta

## MODIFIED Requirements

### Requirement: Rename without attaching
The rename key SHALL ask for a new name in a dialog and then, because the CLI has no rename verb and resuming a listed background job with new flags starts a copy, SHALL remove the job and resume it by session ID under the new name, which keeps the session ID and its conversation. Renaming SHALL be refused while the session is working or when it has never been sent a message, and SHALL resume the job under its old name when the resume under the new name fails. A new name the launch rules would refuse SHALL be refused before anything runs, with the same sentence `vantage here` gives for that name.

#### Scenario: Rename an idle session
- **WHEN** the user presses the rename key on an idle background session and types a new name
- **THEN** the job is removed and resumed by session ID with the new name, and the row shows the new name with the same session ID on the next refresh

#### Scenario: Session is working
- **WHEN** the user presses the rename key on a working session
- **THEN** nothing happens and the footer explains that renaming restarts the session

#### Scenario: New name refused
- **WHEN** the user leaves the new name blank
- **THEN** nothing runs and the footer shows exactly "A session name is required.", with nothing after it

#### Scenario: New name with a semicolon
- **WHEN** the user renames an idle session to `fix a;b`
- **THEN** the job is resumed under the name `fix a;b`, and the row shows that name on the next refresh
