# Spec Delta

## MODIFIED Requirements

### Requirement: The Config tab's counts and keys
Beside the tab titles the Config tab SHALL count its settings and, when any is missing, the setup to do, the latter in amber. Its key bar SHALL read Enter Edit, then c Copy, f Open folder, / Filter, ? Help, ! Report and q Quit. c SHALL copy a path value whole; f SHALL open a folder value in the editor, and the folder holding a file value; on a value that is not a path both SHALL say so.

#### Scenario: Counts
- **WHEN** there are two roots and the Windows Terminal profiles are not set up
- **THEN** the counts read 6 settings · 1 to set up, the second part in amber

#### Scenario: Copy a long root
- **WHEN** a repository root row's value is elided and the user presses c
- **THEN** the clipboard holds the full path

#### Scenario: Open the settings file's folder
- **WHEN** the user presses f on Settings file
- **THEN** the folder holding `config.json` opens with the folder opener

### Requirement: Each setting describes itself
Every Config row SHALL have a description of what the setting does or what the setup is for, shown as a tooltip when the mouse rests on the row. The status line SHALL NOT show the description; on the Config tab it SHALL show only messages, as on the other tabs.

#### Scenario: Status line for the selected row
- **WHEN** the user selects Worktree root and no key has reported anything
- **THEN** there is no status line

#### Scenario: The description follows the selection
- **WHEN** the user moves from Worktree root to Check for updates
- **THEN** the status line stays empty

#### Scenario: Tooltip on hover
- **WHEN** the mouse rests on the Claude bell row
- **THEN** a tooltip says the bell rings when a session finishes or needs permission while you are away

#### Scenario: A message wins
- **WHEN** a setting has just been saved and the user then moves the selection
- **THEN** the status line says it was saved, and goes when the selection moves
