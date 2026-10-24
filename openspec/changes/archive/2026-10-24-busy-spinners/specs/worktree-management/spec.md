# Spec Delta

## ADDED Requirements

### Requirement: Scanning is visible
The worktrees view SHALL show a spinner with the words "Scanning worktrees" in the middle of the table until the first scan arrives, and SHALL show a spinner beside the footer's working text while an action started from the view runs; neither spinner SHALL appear for the timed rescans.

#### Scenario: First scan pending
- **WHEN** the user opens the worktrees view and the scan of the repository roots has not yet returned
- **THEN** the table area shows a spinner and "Scanning worktrees", which disappear when the first rows arrive

#### Scenario: Action running
- **WHEN** the user removes a worktree
- **THEN** the footer shows a spinner before "Removing name" until the removal reports its outcome
