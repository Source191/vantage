# Spec Delta

## MODIFIED Requirements

### Requirement: Settings can be edited from either view
The settings SHALL be shown and edited on the Config tab, a row at a time: the repository roots together, one per line, and the worktree root, the editor and the default worktree base each in a dialog for that one value, the update check by Enter on its row. Esc SHALL close a dialog without changing anything. No other key SHALL open a settings dialog.

#### Scenario: Open settings
- **WHEN** the user presses Right from the Worktrees tab, or clicks the Config title
- **THEN** the Config tab shows the current roots, worktree root, editor, default base and update check

#### Scenario: Cancel
- **WHEN** the user changes the worktree root in its dialog and presses Esc
- **THEN** `config.json` is unchanged and the Worktree root row still shows the old worktree root

#### Scenario: The comma key
- **WHEN** the user presses , on any tab
- **THEN** nothing opens and nothing changes
