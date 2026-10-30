# Spec Delta

## MODIFIED Requirements

### Requirement: The folder is shown relative to its root
Both table views SHALL show the row's folder inside their WHERE or WORKTREE column rather than in a FOLDER column of its own. The folder's label SHALL be its path relative to the deepest configured root that contains it (a repository root or the worktree root); a folder under no root but under the user's home folder SHALL be labelled `~\` followed by its path relative to home; any other folder, and a folder that is itself a root, SHALL be labelled in full or relative to home by the same rules. Inside a repository the label's part after the repository is shown after ›; outside one the label stands alone.

#### Scenario: Worktree under the worktree root
- **WHEN** a session runs in C:\Users\me\source\worktrees\Northwind.Orders-fix-123 on branch fix-123 and the worktree root is C:\Users\me\source\worktrees
- **THEN** its WHERE cell reads Northwind.Orders › fix-123

#### Scenario: Repository under a repository root
- **WHEN** a session runs in C:\Users\me\source\repos\Northwind.Orders\src\Api and the repository root is C:\Users\me\source\repos
- **THEN** its WHERE cell reads Northwind.Orders › src\Api followed by the branch in brackets

#### Scenario: Folder outside every root
- **WHEN** a session runs in C:\Users\me\scratch\notes, which is not a repository
- **THEN** its WHERE cell reads ~\scratch\notes

#### Scenario: Folder outside home
- **WHEN** a session runs in D:\work\Northwind.Tools, which is not a repository, and no root contains it
- **THEN** its WHERE cell reads D:\work\Northwind.Tools

#### Scenario: Session in a root itself
- **WHEN** a session started with vantage here runs in the repository root C:\Users\me\source\repos
- **THEN** its WHERE cell reads ~\source\repos
