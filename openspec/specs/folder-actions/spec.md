# folder-actions Specification

## Purpose
Lets the user take a row's folder, in the sessions view or the worktrees view, straight to other tools: see where it is, copy its path, open it in an editor, open its solution.

## Requirements

### Requirement: The folder is shown relative to its root
Both table views SHALL show a FOLDER column holding the row's folder relative to the deepest configured root that contains it (a repository root or the worktree root); a folder under no root but under the user's home folder SHALL be shown as `~\` followed by its path relative to home; any other folder, and a folder that is itself a root, SHALL be shown in full or relative to home by the same rules.

#### Scenario: Worktree under the worktree root
- **WHEN** a session runs in C:\Users\me\source\worktrees\Northwind.Orders-fix-123 and the worktree root is C:\Users\me\source\worktrees
- **THEN** its FOLDER cell reads Northwind.Orders-fix-123

#### Scenario: Repository under a repository root
- **WHEN** a session runs in C:\Users\me\source\repos\Northwind.Orders\src\Api and the repository root is C:\Users\me\source\repos
- **THEN** its FOLDER cell reads Northwind.Orders\src\Api

#### Scenario: Folder outside every root
- **WHEN** a session runs in C:\Users\me\scratch\notes
- **THEN** its FOLDER cell reads ~\scratch\notes

#### Scenario: Folder outside home
- **WHEN** a session runs in D:\work\Northwind.Tools and no root contains it
- **THEN** its FOLDER cell reads D:\work\Northwind.Tools

#### Scenario: Session in a root itself
- **WHEN** a "no repository" session runs in the repository root C:\Users\me\source\repos
- **THEN** its FOLDER cell reads ~\source\repos

### Requirement: The copy key puts the full folder path on the clipboard
In both table views the copy key SHALL place the selected row's full folder path on the Windows clipboard as plain text and SHALL say in the footer what it copied.

#### Scenario: Copy a session's folder
- **WHEN** the user presses the copy key on a session running in C:\Users\me\source\worktrees\Northwind.Orders-fix-123
- **THEN** the clipboard holds C:\Users\me\source\worktrees\Northwind.Orders-fix-123 and the footer says the path was copied

#### Scenario: Clipboard held by another program
- **WHEN** another program keeps the clipboard open while the user presses the copy key
- **THEN** nothing is copied and the footer says the clipboard was busy and to try again

### Requirement: The editor key opens the folder in the chosen editor
In both table views the editor key SHALL open the selected row's folder in the editor chosen in settings, without waiting for the editor to close, without passing Vantage's Claude Code session markers to it, and without the folder path ever being read by a command shell.

#### Scenario: Open in VS Code
- **WHEN** the editor setting is VS Code and the user presses the editor key on a row
- **THEN** VS Code opens a window on that row's folder and the footer names the folder and the editor

#### Scenario: Editor not installed
- **WHEN** the chosen editor's executable cannot be found
- **THEN** nothing is started and the footer says which editor could not be found and that settings choose another

#### Scenario: Folder with shell characters
- **WHEN** the folder is C:\Users\me\source\repos\Northwind & Co's tools
- **THEN** the editor opens that folder and no part of its name is run as a command

#### Scenario: Folder gone
- **WHEN** the selected session's working directory no longer exists
- **THEN** nothing is started and the footer says the folder no longer exists

### Requirement: The solution key opens the folder's solution
In both table views the solution key SHALL look for files ending `.sln` or `.slnx` directly in the selected row's folder and, when none are there, directly in its `src` folder, and SHALL open the solution with Windows' default application for its file type, without waiting for it to close, without passing Vantage's Claude Code session markers to it (the same environment the editor key gives its editor), and without any part of the solution's path being read by a command shell. One solution found SHALL be opened; several SHALL open the one whose name matches the row's repository name, else ask the user which to open; none SHALL be reported in the footer.

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
- **THEN** a dialog lists both, the chosen one opens, and Esc opens nothing

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

### Requirement: Folder keys need a folder
The copy, editor and solution keys SHALL act on every row that has a folder, including terminal and stopped sessions, SHALL say so in the footer when no row is selected, and in the worktrees view SHALL refuse a leftover row whose folder no longer exists, as the start-session and shell keys do.

#### Scenario: Terminal session
- **WHEN** the user presses the editor key on a session marked terminal
- **THEN** its folder opens in the editor

#### Scenario: Nothing selected
- **WHEN** the table is empty and the user presses the copy key
- **THEN** the footer says no row is selected

#### Scenario: Leftover folder gone
- **WHEN** the user presses the solution key on a worktrees row whose folder no longer exists
- **THEN** nothing happens and the footer names the folder as a leftover
