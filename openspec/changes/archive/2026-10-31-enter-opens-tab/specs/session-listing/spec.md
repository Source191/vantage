# Spec Delta

## MODIFIED Requirements

### Requirement: Interactive sessions are shown with limited actions
Sessions whose kind is interactive SHALL appear in the list with their kind marked by the hollow block in the TAB column, and SHALL NOT accept open-in-tab (Enter), stop or rename actions; the adopt key moves them under the supervisor and, for one never sent a message, the delete key closes its tab (session-attach spec).

#### Scenario: Action on an interactive row
- **WHEN** the user presses Enter on an interactive session's row
- **THEN** the footer explains that the session runs in its own terminal and names the adopt key, and nothing else happens
