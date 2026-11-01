# Spec Delta

## ADDED Requirements

### Requirement: The folder keys are c, f and s
In the sessions and worktrees views the copy key SHALL be c, the editor key f and the solution key s; on the Config tab the copy key SHALL be c and the editor key f. The letters e and v SHALL do nothing on any tab.

#### Scenario: f opens the folder
- **WHEN** the editor setting is VS Code and the user presses f on a session running in C:\Users\me\source\worktrees\Northwind.Orders-fix-123
- **THEN** VS Code opens a window on that folder

#### Scenario: s opens the solution
- **WHEN** the user presses s on a worktree row whose folder holds Northwind.Orders.slnx and no other solution
- **THEN** Northwind.Orders.slnx opens in the application Windows associates with .slnx

#### Scenario: The old letters
- **WHEN** the user presses e or v on the Sessions, Worktrees or Config tab
- **THEN** nothing opens and the status line does not change
