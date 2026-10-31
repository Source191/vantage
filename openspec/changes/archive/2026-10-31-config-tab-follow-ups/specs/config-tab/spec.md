# Spec Delta

## ADDED Requirements

### Requirement: The repository roots dialog browses for folders
The repository roots dialog SHALL offer Browse (F3), which opens a folder picker inside the terminal that can choose several folders; each folder chosen SHALL be added as a new line after the roots already listed, and the dialog SHALL check the list on F5 as it does for typed roots. Cancelling the picker SHALL add nothing.

#### Scenario: Add a root by browsing
- **WHEN** the roots are C:\Users\me\source\repos and the user presses F3, chooses D:\work and confirms
- **THEN** the roots box reads C:\Users\me\source\repos and D:\work on its own line, and nothing is saved until F5

#### Scenario: Two folders at once
- **WHEN** the user chooses D:\work and E:\personal in the picker
- **THEN** both are added, each on a line of its own, in the order chosen

#### Scenario: Cancel the picker
- **WHEN** the user presses F3 and then Esc in the picker
- **THEN** the roots box is unchanged

#### Scenario: A folder already listed
- **WHEN** the user browses to C:\Users\me\source\repos, already a root, and presses F5
- **THEN** nothing is saved and the dialog says it is already a repository root

### Requirement: The worktree root dialog browses for a folder
The worktree root dialog SHALL offer Browse (F3), which opens the same folder picker for one folder, starting in the folder holding the current worktree root; the folder chosen SHALL replace the path in the box, and the dialog SHALL check it on F5 as it does for a typed path. Cancelling the picker SHALL change nothing.

#### Scenario: Choose the worktree root by browsing
- **WHEN** the worktree root is C:\Users\me\source\worktrees and the user presses F3, chooses D:\trees and confirms
- **THEN** the box reads D:\trees, and nothing is saved until F5

#### Scenario: Cancel the picker for the worktree root
- **WHEN** the user presses F3 in the worktree root dialog and then Esc in the picker
- **THEN** the box is unchanged

#### Scenario: The picker starts beside the worktree root
- **WHEN** the worktree root is C:\Users\me\source\worktrees and the user presses F3
- **THEN** the picker opens in C:\Users\me\source

### Requirement: Default place is a setting row
The Config tab SHALL show a Default place row after Default base, reading New worktree or Current branch; Enter on it SHALL open a dialog to choose between them, and saving SHALL write `config.json` and apply to the next wizard at once.

#### Scenario: Default
- **WHEN** `config.json` has no default place
- **THEN** the Default place row reads New worktree

#### Scenario: Choose the current branch
- **WHEN** the user presses Enter on Default place, chooses Current branch and presses F5
- **THEN** the row reads Current branch and the next wizard opened with n starts on Current branch
