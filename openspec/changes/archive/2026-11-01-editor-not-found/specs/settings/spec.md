## MODIFIED Requirements

### Requirement: An editor Vantage cannot use reads as VS Code
A custom editor whose arguments do not contain `{folder}`, whose executable is not a full path, or whose executable is a `.cmd` or `.bat` file SHALL be refused when saved from its dialog and SHALL read as VS Code when found in the file; a preset name the file holds that Vantage no longer offers SHALL read as VS Code with every other setting kept. A custom editor in the file whose executable does not exist SHALL be kept as it is, so the editor key reports it rather than another editor opening.

#### Scenario: A batch file in the file
- **WHEN** `config.json` names a custom editor whose executable is `C:\Tools\edit.bat` with arguments `{folder}`
- **THEN** the editor reads as VS Code and the other settings are kept

#### Scenario: A custom executable gone from the file
- **WHEN** `config.json` names a custom editor whose executable is `E:\Tools\subl.exe` with arguments `{folder}` and drive E: is not attached
- **THEN** the editor reads as that custom editor and the Open folder with row shows it

### Requirement: A custom editor is checked before it is saved
The Open folder with dialog SHALL refuse to save, name the problem and stay open when a custom editor has no executable, an executable that is not a full path, an executable ending `.cmd` or `.bat` (Windows runs batch files through cmd.exe, which would parse the folder path), an executable that does not exist, or no `{folder}` in its arguments.

#### Scenario: No placeholder
- **WHEN** the user enters `C:\Tools\subl.exe` with the arguments `-n` as a custom editor and presses F5
- **THEN** nothing is saved, the dialog stays open and says the arguments need `{folder}`

#### Scenario: Missing executable
- **WHEN** the user enters `C:\Tools\subl.exe` with the arguments `-n {folder}` as a custom editor, no such file exists, and the user presses F5
- **THEN** nothing is saved, the dialog stays open and says `C:\Tools\subl.exe` does not exist

### Requirement: Rider opens the newest installed Rider
When the editor is Rider, the folder SHALL open in the newest of the installed Riders found under Program Files, in JetBrains Toolbox's apps folder, or in a folder of the user's own Programs folder (`%LOCALAPPDATA%\Programs`, where Toolbox 2.0 and later install), comparing the numbers in their install folders as numbers rather than as text, with the same answer whatever the culture of the machine.

#### Scenario: A two-digit release
- **WHEN** the editor is Rider and both JetBrains Rider 2025.9 and JetBrains Rider 2025.10 are installed under Program Files
- **THEN** pressing f on a worktree row opens it in JetBrains Rider 2025.10

#### Scenario: Toolbox builds
- **WHEN** the editor is Rider and Toolbox holds Rider builds 253.99 and 253.100 in one channel
- **THEN** pressing f on a worktree row opens it in build 253.100

#### Scenario: Toolbox 2 install
- **WHEN** the editor is Rider and the only Rider is `C:\Users\me\AppData\Local\Programs\Rider\bin\rider64.exe`
- **THEN** pressing f on a worktree row opens it in that Rider

## ADDED Requirements

### Requirement: The Open folder with dialog marks presets it cannot find
The Open folder with dialog SHALL look for each preset's editor as it opens, without holding up the dialog, and once it has looked SHALL show `(not found)` after each preset it could not find. Explorer and Custom SHALL never be marked. A preset marked not found SHALL still be chosen and saved without a problem.

#### Scenario: Rider not installed
- **WHEN** the user presses Enter on the Open folder with row and no Rider is installed
- **THEN** the dialog lists Rider (not found) and the other presets as they are

#### Scenario: Saving a missing preset
- **WHEN** the user chooses Rider (not found) and presses F5
- **THEN** the setting is saved and the Open folder with row reads Rider

### Requirement: A Custom executable can be browsed for
While Custom is chosen, the Open folder with dialog SHALL offer Browse (F3), which opens a file picker; the file chosen SHALL replace the executable field, and cancelling the picker SHALL leave it unchanged. The chosen file SHALL be checked by F5 like a typed one. Browse SHALL not be offered while a preset is chosen.

#### Scenario: Browse for an executable
- **WHEN** Custom is chosen, the user presses F3 and chooses `C:\Tools\subl.exe`
- **THEN** the executable field reads `C:\Tools\subl.exe` and nothing is saved until F5

#### Scenario: A preset is chosen
- **WHEN** VS Code is chosen in the Open folder with dialog
- **THEN** no Browse button is shown and F3 does nothing
