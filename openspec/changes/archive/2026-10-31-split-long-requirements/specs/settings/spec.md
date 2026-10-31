# Spec Delta

## MODIFIED Requirements

### Requirement: The editor is a preset or a custom command
The editor setting SHALL be one of the presets VS Code, Visual Studio, Rider and Explorer, or a custom executable, named by its full path, with arguments in which `{folder}` stands for the folder to open. Visual Studio SHALL open the folder in the newest installed Visual Studio.

#### Scenario: Custom editor
- **WHEN** the editor is the executable `C:\Tools\subl.exe` with arguments `-n {folder}`
- **THEN** the setting is kept and names that executable and those arguments

#### Scenario: Custom editor without a placeholder in the file
- **WHEN** `config.json` names a custom editor whose arguments are `-n`
- **THEN** the editor reads as VS Code

#### Scenario: Custom editor by bare name in the file
- **WHEN** `config.json` names a custom editor whose executable is `subl` with arguments `{folder}`
- **THEN** the editor reads as VS Code, because a bare name would be searched for in the folder Vantage was started from

#### Scenario: Cursor in an old file
- **WHEN** `config.json` names the preset Cursor and the repository root `D:\work`
- **THEN** the editor reads as VS Code, the Open folder with row and its dialog show VS Code, and `D:\work` is still the repository root

#### Scenario: Visual Studio in an old file
- **WHEN** `config.json` names the preset VisualStudio and the repository root `D:\work`
- **THEN** the editor reads as Visual Studio, the Open folder with row shows Visual Studio, and `D:\work` is still the repository root

#### Scenario: Visual Studio opens the folder
- **WHEN** the editor is Visual Studio and the user presses e on a worktree row
- **THEN** the newest installed Visual Studio opens that worktree's folder

#### Scenario: The dialog's words
- **WHEN** the user presses Enter on the Open folder with row
- **THEN** the dialog's choice is captioned "Open folder with", lists VS Code, Visual Studio, Rider, Explorer and Custom, and the e key's bar item reads Open folder

### Requirement: Settings are validated before they are saved
A setting's dialog SHALL refuse to save, name the problem and keep the dialog open when a repository root is not a full path, does not exist or is already a root, or when the worktree root is not a full path. A worktree root that does not exist yet SHALL be accepted. Saving the repository roots dialog with no root left SHALL be refused.

#### Scenario: Batch file editor
- **WHEN** the user enters `C:\Tools\edit.cmd` as a custom editor's executable
- **THEN** nothing is saved and the dialog says a batch file cannot be used as the editor

#### Scenario: Bare-name editor
- **WHEN** the user enters `subl` as a custom editor's executable
- **THEN** nothing is saved and the dialog says the executable must be a full path

#### Scenario: Missing root
- **WHEN** the user enters `D:\nowhere` as a repository root and that folder does not exist
- **THEN** nothing is saved and the dialog says `D:\nowhere` does not exist

#### Scenario: Relative worktree root
- **WHEN** the user enters `worktrees` as the worktree root
- **THEN** nothing is saved and the dialog says the worktree root must be a full path

#### Scenario: New worktree root
- **WHEN** the user enters `D:\worktrees`, which does not exist yet
- **THEN** the settings are saved

#### Scenario: No root left
- **WHEN** the user deletes every line of the repository roots dialog and presses F5
- **THEN** nothing is saved and the dialog asks for a repository root

## ADDED Requirements

### Requirement: An editor Vantage cannot use reads as VS Code
A custom editor whose arguments do not contain `{folder}`, whose executable is not a full path, or whose executable is a `.cmd` or `.bat` file SHALL be refused when saved from its dialog and SHALL read as VS Code when found in the file; a preset name the file holds that Vantage no longer offers, such as Cursor, SHALL read as VS Code with every other setting kept.

#### Scenario: A batch file in the file
- **WHEN** `config.json` names a custom editor whose executable is `C:\Tools\edit.bat` with arguments `{folder}`
- **THEN** the editor reads as VS Code and the other settings are kept

### Requirement: The editor's words say it opens a folder
The Config tab's row and its dialog SHALL caption the editor choice as what opens a folder, and the folder key's hint SHALL say it opens the folder.

#### Scenario: The Config tab's row
- **WHEN** the Config tab is shown
- **THEN** the editor's row reads Open folder with

### Requirement: A custom editor is checked before it is saved
The Open folder with dialog SHALL refuse to save, name the problem and stay open when a custom editor has no executable, an executable that is not a full path, an executable ending `.cmd` or `.bat` (Windows runs batch files through cmd.exe, which would parse the folder path) or no `{folder}` in its arguments.

#### Scenario: No placeholder
- **WHEN** the user enters `C:\Tools\subl.exe` with the arguments `-n` as a custom editor and presses F5
- **THEN** nothing is saved, the dialog stays open and says the arguments need `{folder}`
