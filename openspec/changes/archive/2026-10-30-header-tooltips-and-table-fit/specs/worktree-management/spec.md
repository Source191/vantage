# Spec Delta

## MODIFIED Requirements

### Requirement: The worktrees header and help match the sessions view
The worktrees view's header SHALL show the Vantage version, the Claude Code CLI version and the repository roots as the sessions view's does; its help SHALL explain each of its columns and SHALL be visible or reachable by scrolling in full on a terminal of 36 rows or more, as the sessions help is; each of its key hints SHALL show the key's tooltip sentence, in its own colours, while the mouse pointer rests on the key or on its words, and each of its column headers SHALL show what the column shows, keeping the help's line breaks.

#### Scenario: Worktrees help
- **WHEN** the user presses the help key in the worktrees view
- **THEN** the help names every column of the worktrees table with its meaning, including what each health word means

#### Scenario: Worktrees help on a short terminal
- **WHEN** the user presses the help key in the worktrees view on a 120 x 36 terminal
- **THEN** every line of the help is reachable with the arrow keys and PageDown

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on the p key, or on the word Prune beside it, in the worktrees view
- **THEN** a tooltip in colours distinct from the header says that P prunes the stale worktree registrations of the row's repository

#### Scenario: Hover over a column header
- **WHEN** the mouse pointer rests on the HEALTH header
- **THEN** a tooltip names the column and explains what each health word means, on the lines the help uses
