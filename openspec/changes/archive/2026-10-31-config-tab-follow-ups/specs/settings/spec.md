# Spec Delta

## MODIFIED Requirements

### Requirement: Settings live in one file with defaults
Vantage SHALL keep its settings in `config.json` in its state folder: the repository roots, the worktree root, the editor, the default worktree base and the default place for a new session. A missing file, a corrupt file or a missing property SHALL read as the default: roots `<home>\source\repos`, worktree root `worktrees` beside the first repository root, editor VS Code, base the remote default branch, place a new worktree.

#### Scenario: No settings file
- **WHEN** Vantage starts and `config.json` does not exist
- **THEN** it scans `C:\Users\me\source\repos`, puts new worktrees under `C:\Users\me\source\worktrees`, opens folders in VS Code, branches new worktrees from origin's default branch and opens the wizard on New worktree

#### Scenario: File from an earlier version
- **WHEN** `config.json` holds only `repositoryRoots` of `D:\work` and `defaultBase` of `Head`
- **THEN** Vantage scans `D:\work`, branches from HEAD, puts new worktrees under `D:\worktrees`, opens folders in VS Code and opens the wizard on New worktree

#### Scenario: Corrupt file
- **WHEN** `config.json` is not valid JSON
- **THEN** Vantage starts with every default and does not exit

#### Scenario: The default place by hand
- **WHEN** `config.json` holds `defaultPlace` of `CurrentBranch`
- **THEN** the wizard opened with n starts on Current branch

### Requirement: The editor is a preset or a custom command
The editor setting SHALL be one of the presets VS Code, Visual Studio, Rider and Explorer, or a custom executable, named by its full path, with arguments in which `{folder}` stands for the folder to open. Visual Studio SHALL open the folder in the newest installed Visual Studio. A custom editor whose arguments do not contain `{folder}`, whose executable is not a full path, or whose executable is a `.cmd` or `.bat` file SHALL be refused when saved from its dialog and SHALL read as VS Code when found in the file; a preset name the file holds that Vantage no longer offers, such as Cursor, SHALL read as VS Code with every other setting kept. The Config tab's row and its dialog SHALL caption the choice as what opens a folder, and the folder key's hint SHALL say it opens the folder.

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