# Spec Delta

## MODIFIED Requirements

### Requirement: The editor is a preset or a custom command
The editor setting SHALL be one of the presets VS Code, Cursor, Rider and Explorer, or a custom executable, named by its full path, with arguments in which `{folder}` stands for the folder to open. A custom editor whose arguments do not contain `{folder}`, whose executable is not a full path, or whose executable is a `.cmd` or `.bat` file SHALL be refused when saved from the dialog and SHALL read as VS Code when found in the file; a preset name the file holds that Vantage no longer offers, such as Visual Studio, SHALL read as VS Code with every other setting kept. The dialog SHALL caption the choice as what opens a folder, and the folder key's hint SHALL say it opens the folder.

#### Scenario: Custom editor
- **WHEN** the editor is the executable `C:\Tools\subl.exe` with arguments `-n {folder}`
- **THEN** the setting is kept and names that executable and those arguments

#### Scenario: Custom editor without a placeholder in the file
- **WHEN** `config.json` names a custom editor whose arguments are `-n`
- **THEN** the editor reads as VS Code

#### Scenario: Custom editor by bare name in the file
- **WHEN** `config.json` names a custom editor whose executable is `subl` with arguments `{folder}`
- **THEN** the editor reads as VS Code, because a bare name would be searched for in the folder Vantage was started from

#### Scenario: Visual Studio in an old file
- **WHEN** `config.json` names the preset VisualStudio and the repository root `D:\work`
- **THEN** the editor reads as VS Code, the dialog shows VS Code chosen, and `D:\work` is still the repository root

#### Scenario: The dialog's words
- **WHEN** the user opens the settings dialog
- **THEN** the choice is captioned "Open folder with", lists VS Code, Cursor, Rider, Explorer and Custom, and the e key's header hint reads Open folder

### Requirement: Settings are validated before they are saved
The dialog SHALL refuse to save, name the problem and keep the dialog open when a repository root is not a full path or does not exist, when there is no repository root, when the worktree root is not a full path, or when a custom editor has no executable, an executable that is not a full path, an executable ending `.cmd` or `.bat` (Windows runs batch files through cmd.exe, which would parse the folder path) or no `{folder}` in its arguments. A worktree root that does not exist yet SHALL be accepted.

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
