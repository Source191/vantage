## ADDED Requirements

### Requirement: Rider opens the newest installed Rider
When the editor is Rider, the folder SHALL open in the newest of the installed Riders found, comparing the numbers in their install folders as numbers rather than as text, with the same answer whatever the culture of the machine.

#### Scenario: A two-digit release
- **WHEN** the editor is Rider and both JetBrains Rider 2025.9 and JetBrains Rider 2025.10 are installed under Program Files
- **THEN** pressing e on a worktree row opens it in JetBrains Rider 2025.10

#### Scenario: Toolbox builds
- **WHEN** the editor is Rider and Toolbox holds Rider builds 253.99 and 253.100 in one channel
- **THEN** pressing e on a worktree row opens it in build 253.100
