# Spec Delta

## MODIFIED Requirements

### Requirement: The folder is shown relative to its root
Both table views SHALL show the row's folder inside their WHERE or WORKTREE column rather than in a FOLDER column of its own. The folder's label SHALL be its path relative to the deepest configured root that contains it (a repository root or the worktree root).

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

### Requirement: The solution key opens the folder's solution
In both table views the solution key SHALL look for files ending `.sln` or `.slnx` directly in the selected row's folder and, when none are there, directly in its `src` folder. One solution found SHALL be opened; several SHALL open the one whose name matches the row's repository name, else ask the user which to open from a list of their paths relative to the folder, in a dialog whose tab title names it; none SHALL be reported in the footer.

#### Scenario: One solution
- **WHEN** the folder holds Northwind.Orders.slnx and no other solution
- **THEN** Northwind.Orders.slnx opens in the application Windows associates with .slnx

#### Scenario: Solution under src
- **WHEN** the folder holds no solution and its src folder holds Northwind.Orders.sln
- **THEN** Northwind.Orders.sln opens

#### Scenario: Several solutions, one named after the repository
- **WHEN** a worktree of Northwind.Orders holds Northwind.Orders.slnx and Northwind.Orders.Tools.slnx
- **THEN** Northwind.Orders.slnx opens without a question

#### Scenario: Several solutions, none named after the repository
- **WHEN** the folder holds Api.sln and Worker.sln and the repository is Northwind.Orders
- **THEN** a dialog lists Api.sln and Worker.sln one per line, the arrow keys move between them, Enter opens the chosen one, Esc opens nothing, and the tab reads Vantage (Open solution) while the dialog is open

#### Scenario: Long solution names
- **WHEN** the folder's src folder holds Northwind.Orders.PackageConfiguration - Copy.slnx and Northwind.Orders.PackageConfiguration - temp.slnx
- **THEN** the dialog lists src\Northwind.Orders.PackageConfiguration - Copy.slnx and src\Northwind.Orders.PackageConfiguration - temp.slnx in full, each on its own line

#### Scenario: No solution
- **WHEN** neither the folder nor its src folder holds a solution
- **THEN** nothing opens and the footer says no solution was found in that folder

#### Scenario: Vantage started from a Claude Code session
- **WHEN** Vantage's own environment holds NO_COLOR, CLAUDECODE, CLAUDE_PID and CLAUDE_CODE_SESSION_ID and the user opens a solution
- **THEN** the application that opens the solution has none of those variables, and its other variables, such as PATH, are as Vantage's

#### Scenario: Solution path with shell characters
- **WHEN** the solution is C:\Users\me\source\repos\Northwind & Co's tools\100% ^ (a)\Northwind.Orders.slnx
- **THEN** that solution opens and no part of its path is run as a command

#### Scenario: Solution gone before it opens
- **WHEN** the solution file is deleted after it was found and before it is opened
- **THEN** nothing opens, no dialog appears and the footer says the solution could not be opened and why

## ADDED Requirements

### Requirement: A folder outside the roots is labelled from home
A folder under no configured root but under the user's home folder SHALL be labelled `~\` followed by its path relative to home, and any other folder SHALL be labelled in full. A folder that is itself a root SHALL be labelled by the same two rules.

#### Scenario: The worktree root itself
- **WHEN** a session started with vantage here runs in the worktree root C:\Users\me\source\worktrees, which is not a repository
- **THEN** its WHERE cell reads ~\source\worktrees

### Requirement: The part after the repository follows ›
Inside a repository the folder label's part after the repository SHALL be shown after ›; a folder outside every repository SHALL have its label stand alone.

#### Scenario: A worktree's slug after the repository
- **WHEN** a session runs in C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf, a worktree of Northwind.Billing
- **THEN** its WHERE cell starts Northwind.Billing › invoice-pdf

#### Scenario: Not a repository
- **WHEN** a session runs in C:\Users\me\scratch\notes, which is not a repository
- **THEN** its WHERE cell holds no ›

### Requirement: A solution opens in its own application, safely
The solution key SHALL open the solution with Windows' default application for its file type, without waiting for it to close, without passing Vantage's Claude Code session markers to it (the same environment the editor key gives its editor), and without any part of the solution's path being read by a command shell.

#### Scenario: Vantage does not wait
- **WHEN** the user opens Northwind.Orders.slnx with the solution key and its application stays open
- **THEN** Vantage's keys work at once, without waiting for that application to close
