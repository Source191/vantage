# Spec Delta

## ADDED Requirements

### Requirement: Grey text is legible
Text the console draws in grey (stopped and unknown sessions, empty worktrees, the Config tab's setup rows, dialog hints, the automated count, the tab frames and the separators between parts of a line) SHALL have a contrast ratio of at least 4.5:1 against the console's background.

#### Scenario: A stopped row
- **WHEN** a stopped session's row is drawn on the console's background
- **THEN** its text has a contrast ratio of at least 4.5:1 against that background

#### Scenario: A dialog hint
- **WHEN** the rename dialog shows its hint, Esc keeps the current name
- **THEN** the hint has a contrast ratio of at least 4.5:1 against the console's background
