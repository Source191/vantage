## MODIFIED Requirements

### Requirement: The editor key opens the folder in the chosen editor
In both table views the editor key SHALL open the selected row's folder in the editor chosen in settings, without waiting for the editor to close, without passing Vantage's Claude Code session markers to it, and without the folder path ever being read by a command shell. When the chosen editor, a preset or a Custom executable, cannot be found or will not start, nothing SHALL be started and the status line SHALL say so in Vantage's words and that the Config tab's Open folder with row chooses another.

#### Scenario: Open in VS Code
- **WHEN** the editor setting is VS Code and the user presses the editor key on a row
- **THEN** VS Code opens a window on that row's folder and the status line names the folder and the editor

#### Scenario: Editor not installed
- **WHEN** the chosen editor's executable cannot be found
- **THEN** nothing is started and the status line says which editor could not be found and that Open folder with on the Config tab chooses another

#### Scenario: Custom editor removed
- **WHEN** the editor setting is the Custom executable `C:\Tools\subl.exe` and that file has been deleted since it was saved
- **THEN** nothing is started and the status line says subl could not be found and that Open folder with on the Config tab chooses another, with no .NET error text

#### Scenario: Editor will not start
- **WHEN** the chosen editor's executable exists but Windows refuses to start it
- **THEN** the status line, as a failure, names the editor, gives Windows' reason and says that Open folder with on the Config tab chooses another

#### Scenario: Folder with shell characters
- **WHEN** the folder is C:\Users\me\source\repos\Northwind & Co's tools
- **THEN** the editor opens that folder and no part of its name is run as a command

#### Scenario: Folder gone
- **WHEN** the selected session's working directory no longer exists
- **THEN** nothing is started and the status line says the folder no longer exists

### Requirement: A solution opens in its own application, safely
The solution key SHALL open the solution with Windows' default application for its file type, without waiting for it to close, without passing Vantage's Claude Code session markers to it (the same environment the editor key gives its editor), and without any part of the solution's path being read by a command shell. When Windows has no application for the solution's file type, nothing SHALL open and the status line SHALL say that no application opens that type.

#### Scenario: Vantage does not wait
- **WHEN** the user opens Northwind.Orders.slnx with the solution key and its application stays open
- **THEN** Vantage's keys work at once, without waiting for that application to close

#### Scenario: No application for the type
- **WHEN** the user presses the solution key on a row whose folder holds Northwind.Orders.slnx and Windows has no application for .slnx
- **THEN** nothing opens and the status line, as a failure, says no application opens .slnx files, with no .NET or PowerShell error text
