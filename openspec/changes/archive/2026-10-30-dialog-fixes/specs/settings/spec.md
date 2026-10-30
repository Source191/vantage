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
