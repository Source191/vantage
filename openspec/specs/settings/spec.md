# settings Specification

## Purpose
Lets the user see and change where Vantage looks for repositories, where it puts new worktrees, which editor opens a folder and how new worktrees branch, from inside Vantage or by editing one file.

## Requirements

### Requirement: Settings live in one file with defaults
Vantage SHALL keep its settings in `config.json` in its state folder: the repository roots, the worktree root, the editor and the default worktree base. A missing file, a corrupt file or a missing property SHALL read as the default: roots `<home>\source\repos`, worktree root `worktrees` beside the first repository root, editor VS Code, base the remote default branch.

#### Scenario: No settings file
- **WHEN** Vantage starts and `config.json` does not exist
- **THEN** it scans `C:\Users\me\source\repos`, puts new worktrees under `C:\Users\me\source\worktrees`, opens folders in VS Code and branches new worktrees from origin's default branch

#### Scenario: File from an earlier version
- **WHEN** `config.json` holds only `repositoryRoots` of `D:\work` and `defaultBase` of `Head`
- **THEN** Vantage scans `D:\work`, branches from HEAD, puts new worktrees under `D:\worktrees` and opens folders in VS Code

#### Scenario: Corrupt file
- **WHEN** `config.json` is not valid JSON
- **THEN** Vantage starts with every default and does not exit

### Requirement: The editor is a preset or a custom command
The editor setting SHALL be one of the presets VS Code, Cursor, Rider, Visual Studio and Explorer, or a custom executable with arguments in which `{folder}` stands for the folder to open. A custom editor whose arguments do not contain `{folder}` SHALL be refused when saved from the dialog and SHALL read as VS Code when found in the file.

#### Scenario: Custom editor
- **WHEN** the editor is the executable `C:\Tools\subl.exe` with arguments `-n {folder}`
- **THEN** the setting is kept and names that executable and those arguments

#### Scenario: Custom editor without a placeholder in the file
- **WHEN** `config.json` names a custom editor whose arguments are `-n`
- **THEN** the editor reads as VS Code

### Requirement: Settings can be edited from either view
The settings key SHALL open a settings dialog from the sessions view and from the worktrees view, showing the current repository roots one per line, the worktree root, the editor and the default worktree base, each editable. Esc SHALL close it without changing anything.

#### Scenario: Open settings
- **WHEN** the user presses the settings key in the worktrees view
- **THEN** a dialog shows the current roots, worktree root, editor and default base

#### Scenario: Cancel
- **WHEN** the user changes the worktree root in the dialog and presses Esc
- **THEN** `config.json` is unchanged and the header still shows the old worktree root

### Requirement: Settings are validated before they are saved
The dialog SHALL refuse to save, name the problem and keep the dialog open when a repository root is not a full path or does not exist, when there is no repository root, when the worktree root is not a full path, or when a custom editor has no executable, an executable ending `.cmd` or `.bat` (Windows runs batch files through cmd.exe, which would parse the folder path) or no `{folder}` in its arguments. A worktree root that does not exist yet SHALL be accepted.

#### Scenario: Batch file editor
- **WHEN** the user enters `C:\Tools\edit.cmd` as a custom editor's executable
- **THEN** nothing is saved and the dialog says a batch file cannot be used as the editor

#### Scenario: Missing root
- **WHEN** the user enters `D:\nowhere` as a repository root and that folder does not exist
- **THEN** nothing is saved and the dialog says `D:\nowhere` does not exist

#### Scenario: Relative worktree root
- **WHEN** the user enters `worktrees` as the worktree root
- **THEN** nothing is saved and the dialog says the worktree root must be a full path

#### Scenario: New worktree root
- **WHEN** the user enters `D:\worktrees`, which does not exist yet
- **THEN** the settings are saved

### Requirement: Saved settings apply without a restart
Saving SHALL write `config.json` and SHALL make every later scan, wizard, worktree creation, editor launch and header use the saved values, without restarting Vantage. Saving SHALL keep the file readable by hand, with the property names `repositoryRoots`, `worktreeRoot`, `editor` and `defaultBase`.

#### Scenario: Add a root
- **WHEN** the user adds `D:\work` as a second repository root and saves
- **THEN** the header lists both roots at once, and the next worktree scan and the next wizard include repositories under `D:\work`

#### Scenario: Change the worktree root
- **WHEN** the user sets the worktree root to `D:\worktrees` and saves, then starts a session in a new worktree
- **THEN** the worktree is created under `D:\worktrees`
