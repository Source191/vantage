# Spec Delta

## ADDED Requirements

### Requirement: Deleting a leftover folder never deletes a root
When Vantage deletes a leftover folder from disc, it SHALL refuse a path that is not fully qualified, or that is a drive root or a network share root, before changing anything. The footer SHALL report the refusal as a failure naming the path.

#### Scenario: A drive root reaches the deletion
- **WHEN** a leftover folder's path is C:\
- **THEN** nothing on the drive is deleted and the footer reports a failure naming C:\

#### Scenario: A share root or a relative path reaches the deletion
- **WHEN** a leftover folder's path is \\server\share\ or Northwind.Orders-fix
- **THEN** nothing is deleted and the footer reports a failure naming that path

#### Scenario: An ordinary leftover folder with a trailing separator
- **WHEN** the user removes an orphan folder whose path is written C:\Users\me\source\worktrees\Northwind.Orders-fix\
- **THEN** the folder is deleted as before
