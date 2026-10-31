## MODIFIED Requirements

### Requirement: The folder is shown relative to its root
Both table views SHALL show where a row's folder is in their REPOSITORY or WORKTREE column, not a FOLDER column. A session's REPOSITORY cell SHALL read its repository's name, or the folder's label outside a repository; a worktree's WORKTREE cell SHALL read the repository › the folder's slug, its inner path, or its label when not named after it. A label SHALL be the path relative to the deepest root holding it, else ~\ and the path under home, else in full. Neither cell SHALL show the branch.

#### Scenario: Worktree under the worktree root
- **WHEN** the worktrees view lists C:\Users\me\source\worktrees\Northwind.Orders-fix-123, a worktree of Northwind.Orders on branch fix-123, and the worktree root is C:\Users\me\source\worktrees
- **THEN** its WORKTREE cell reads Northwind.Orders › fix-123

#### Scenario: Session in a worktree
- **WHEN** a session runs in C:\Users\me\source\worktrees\Northwind.Orders-fix-123 on branch fix-123
- **THEN** its REPOSITORY cell reads Northwind.Orders, with no slug and no branch

#### Scenario: Repository under a repository root
- **WHEN** a session runs in C:\Users\me\source\repos\Northwind.Orders\src\Api and the repository root is C:\Users\me\source\repos
- **THEN** its REPOSITORY cell reads Northwind.Orders, with no inner path and no branch

#### Scenario: Session in the main checkout
- **WHEN** a session runs in the main checkout C:\Users\me\source\repos\Northwind.Orders on branch main
- **THEN** its REPOSITORY cell reads Northwind.Orders, with no branch

#### Scenario: Folder outside every root
- **WHEN** a session runs in C:\Users\me\scratch\notes, which is not a repository
- **THEN** its REPOSITORY cell reads ~\scratch\notes

#### Scenario: Folder outside home
- **WHEN** a session runs in D:\work\Northwind.Tools, which is not a repository, and no root contains it
- **THEN** its REPOSITORY cell reads D:\work\Northwind.Tools

#### Scenario: Session in a root itself
- **WHEN** a session started with vantage here runs in the repository root C:\Users\me\source\repos
- **THEN** its REPOSITORY cell reads ~\source\repos
