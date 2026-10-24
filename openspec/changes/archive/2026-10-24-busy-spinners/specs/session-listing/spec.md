# Spec Delta

## ADDED Requirements

### Requirement: Waiting is visible
The sessions view SHALL show a spinner with the words "Listing sessions" in the middle of the table until the first listing arrives, and SHALL show a spinner beside the footer's working text while an action started from the view runs; neither spinner SHALL appear for the timed refreshes.

#### Scenario: First listing pending
- **WHEN** Vantage starts and the CLI listing has not yet returned
- **THEN** the table area shows a spinner and "Listing sessions", which disappear when the first rows arrive

#### Scenario: Action running
- **WHEN** the user starts a session from the wizard
- **THEN** the footer shows a spinner before "Starting name" until the start reports its outcome

#### Scenario: First listing fails
- **WHEN** the first CLI listing fails
- **THEN** the spinner is replaced by the failure banner
