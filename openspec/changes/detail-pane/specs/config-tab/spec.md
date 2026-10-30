# Spec Delta

## ADDED Requirements

### Requirement: Each setting describes itself
Every Config row SHALL have a description of what the setting does or what the setup is for. The status line SHALL show the selected row's description whenever no other message is shown, in place of a fixed hint, and resting the mouse on a row SHALL show the same description as a tooltip.

#### Scenario: Status line for the selected row
- **WHEN** the user selects Worktree root and no message is shown
- **THEN** the status line says where new worktrees go and how their folders are named

#### Scenario: The description follows the selection
- **WHEN** the user moves from Worktree root to Check for updates
- **THEN** the status line changes to Check for updates' description

#### Scenario: Tooltip on hover
- **WHEN** the mouse rests on the Claude bell row
- **THEN** a tooltip says the bell rings when a session finishes or needs permission while you are away

#### Scenario: A message wins
- **WHEN** a setting has just been saved
- **THEN** the status line says it was saved, and goes back to the description when the selection moves
