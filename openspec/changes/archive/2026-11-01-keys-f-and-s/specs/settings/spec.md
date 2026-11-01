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
- **WHEN** the editor is Visual Studio and the user presses f on a worktree row
- **THEN** the newest installed Visual Studio opens that worktree's folder

#### Scenario: The dialog's words
- **WHEN** the user presses Enter on the Open folder with row
- **THEN** the dialog's choice is captioned "Open folder with", lists VS Code, Visual Studio, Rider, Explorer and Custom, and the f key's bar item reads Open folder
