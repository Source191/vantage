# Spec Delta

## MODIFIED Requirements

### Requirement: The editor is a preset or a custom command
The editor setting SHALL be one of the presets VS Code, Cursor, Rider and Explorer, or a custom executable, named by its full path, with arguments in which `{folder}` stands for the folder to open. A custom editor whose arguments do not contain `{folder}`, whose executable is not a full path, or whose executable is a `.cmd` or `.bat` file SHALL be refused when saved from its dialog and SHALL read as VS Code when found in the file; a preset name the file holds that Vantage no longer offers, such as Visual Studio, SHALL read as VS Code with every other setting kept. The Config tab's row and its dialog SHALL caption the choice as what opens a folder, and the folder key's hint SHALL say it opens the folder.

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
- **THEN** the editor reads as VS Code, the Open folder with row and its dialog show VS Code, and `D:\work` is still the repository root

#### Scenario: The dialog's words
- **WHEN** the user presses Enter on the Open folder with row
- **THEN** the dialog's choice is captioned "Open folder with", lists VS Code, Cursor, Rider, Explorer and Custom, and the e key's bar item reads Open folder

### Requirement: Settings can be edited from either view
The settings SHALL be shown and edited on the Config tab, a row at a time: the repository roots together, one per line, and the worktree root, the editor and the default worktree base each in a dialog for that one value, the update check by Enter on its row. Esc SHALL close a dialog without changing anything. No other key SHALL open a settings dialog.

#### Scenario: Open settings
- **WHEN** the user presses 3, or Right from the Worktrees tab
- **THEN** the Config tab shows the current roots, worktree root, editor, default base and update check

#### Scenario: Cancel
- **WHEN** the user changes the worktree root in its dialog and presses Esc
- **THEN** `config.json` is unchanged and the Worktree root row still shows the old worktree root

#### Scenario: The comma key
- **WHEN** the user presses , on any tab
- **THEN** nothing opens and nothing changes

### Requirement: Settings are validated before they are saved
A setting's dialog SHALL refuse to save, name the problem and keep the dialog open when a repository root is not a full path, does not exist or is already a root, when the worktree root is not a full path, or when a custom editor has no executable, an executable that is not a full path, an executable ending `.cmd` or `.bat` (Windows runs batch files through cmd.exe, which would parse the folder path) or no `{folder}` in its arguments. A worktree root that does not exist yet SHALL be accepted. Saving the repository roots dialog with no root left SHALL be refused.

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

### Requirement: Saved settings apply without a restart
Saving SHALL write `config.json` and SHALL make every later scan, wizard, worktree creation, editor launch and the Config tab use the saved values, without restarting Vantage. Saving SHALL keep the file readable by hand, with the property names `repositoryRoots`, `worktreeRoot`, `editor` and `defaultBase`.

#### Scenario: Add a root
- **WHEN** the user adds `D:\work` as a second repository root and saves
- **THEN** the Config tab lists both roots at once, and the next worktree scan and the next wizard include repositories under `D:\work`

#### Scenario: Change the worktree root
- **WHEN** the user sets the worktree root to `D:\worktrees` and saves, then starts a session in a new worktree
- **THEN** the worktree is created under `D:\worktrees`
