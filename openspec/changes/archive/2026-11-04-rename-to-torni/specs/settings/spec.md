## RENAMED Requirements

- FROM: `### Requirement: An editor Vantage cannot use reads as VS Code`
- TO: `### Requirement: An editor Torni cannot use reads as VS Code`

## MODIFIED Requirements

### Requirement: Settings live in one file with defaults
Torni SHALL keep its settings in `config.json` in its state folder: the repository roots, the worktree root, the editor, the default worktree base and the default place for a new session. A missing, corrupt or unreadable file, a number where a base or place name belongs, or a missing property SHALL read as the default: roots `<home>\source\repos`, worktree root `worktrees` beside the first repository root, editor VS Code, base the remote default branch, place a new worktree.

#### Scenario: No settings file
- **WHEN** Torni starts and `config.json` does not exist
- **THEN** it scans `C:\Users\me\source\repos`, puts new worktrees under `C:\Users\me\source\worktrees`, opens folders in VS Code, branches new worktrees from origin's default branch and opens the wizard on New worktree

#### Scenario: File from an earlier version
- **WHEN** `config.json` holds only `repositoryRoots` of `D:\work` and `defaultBase` of `Head`
- **THEN** Torni scans `D:\work`, branches from HEAD, puts new worktrees under `D:\worktrees`, opens folders in VS Code and opens the wizard on New worktree

#### Scenario: Corrupt file
- **WHEN** `config.json` is not valid JSON
- **THEN** Torni starts with every default and does not exit

#### Scenario: A path holding a NUL character
- **WHEN** `config.json` holds `repositoryRoots` of `C:\Users\me\source\repos` and a second root whose text holds a NUL character, and a `worktreeRoot` whose text holds one too
- **THEN** Torni starts, scans `C:\Users\me\source\repos` alone, puts new worktrees under `C:\Users\me\source\worktrees`, and does not exit

#### Scenario: The default place by hand
- **WHEN** `config.json` holds `defaultPlace` of `CurrentBranch`
- **THEN** the wizard opened with n starts on Current branch

#### Scenario: A number where a name belongs
- **WHEN** `config.json` holds `repositoryRoots` of `D:\work` and `defaultBase` of `7`
- **THEN** Torni starts with every default, as it does for a corrupt file, the Config tab draws and the wizard opens, and nothing exits

#### Scenario: A file Vantage may not read
- **WHEN** `config.json` exists but the user's account is denied reading its contents
- **THEN** Torni starts with every default and does not exit

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
- **THEN** the editor reads as VS Code, because a bare name would be searched for in the folder Torni was started from

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

### Requirement: Saved settings apply without a restart
Saving SHALL write `config.json` and SHALL make every later scan, wizard, worktree creation, editor launch and the Config tab use the saved values, without restarting Torni. Saving SHALL keep the file readable by hand, with the property names `repositoryRoots`, `worktreeRoot`, `editor` and `defaultBase`.

#### Scenario: Add a root
- **WHEN** the user adds `D:\work` as a second repository root and saves
- **THEN** the Config tab lists both roots at once, and the next worktree scan and the next wizard include repositories under `D:\work`

#### Scenario: Change the worktree root
- **WHEN** the user sets the worktree root to `D:\worktrees` and saves, then starts a session in a new worktree
- **THEN** the worktree is created under `D:\worktrees`

### Requirement: An editor Torni cannot use reads as VS Code
A custom editor whose arguments do not contain `{folder}`, whose executable is not a full path, or whose executable is a `.cmd` or `.bat` file SHALL be refused when saved from its dialog and SHALL read as VS Code when found in the file; a preset name the file holds that Torni no longer offers SHALL read as VS Code with every other setting kept. A custom editor in the file whose executable does not exist SHALL be kept as it is, so the editor key reports it rather than another editor opening.

#### Scenario: A batch file in the file
- **WHEN** `config.json` names a custom editor whose executable is `C:\Tools\edit.bat` with arguments `{folder}`
- **THEN** the editor reads as VS Code and the other settings are kept

#### Scenario: A custom executable gone from the file
- **WHEN** `config.json` names a custom editor whose executable is `E:\Tools\subl.exe` with arguments `{folder}` and drive E: is not attached
- **THEN** the editor reads as that custom editor and the Open folder with row shows it

### Requirement: A saved settings file is replaced whole
Saving SHALL write the new `config.json` beside the old one and replace it in one step, so a save that is interrupted leaves the previous settings in place and no partly written file is ever read. Another Torni process reading `config.json` SHALL NOT stop a save, and a save SHALL leave no temporary file behind.

#### Scenario: An interrupted save
- **WHEN** Torni is ended while it is saving the repository roots `C:\Users\me\source\repos` and `D:\work`
- **THEN** the next start reads either the roots as they were before the save or both new roots, never no roots

#### Scenario: Another process is reading the file
- **WHEN** `torni here` is reading `config.json` as the console saves a new worktree root
- **THEN** the save completes, the new worktree root is read by the next reader, and only `config.json` is left in the state folder
