# settings Specification

## Purpose
Lets the user see and change where Vantage looks for repositories, where it puts new worktrees, which editor opens a folder and how new worktrees branch, from inside Vantage or by editing one file.

## Requirements

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

#### Scenario: A path holding a NUL character
- **WHEN** `config.json` holds `repositoryRoots` of `C:\Users\me\source\repos` and a second root whose text holds a NUL character, and a `worktreeRoot` whose text holds one too
- **THEN** Vantage starts, scans `C:\Users\me\source\repos` alone, puts new worktrees under `C:\Users\me\source\worktrees`, and does not exit

#### Scenario: The default place by hand
- **WHEN** `config.json` holds `defaultPlace` of `CurrentBranch`
- **THEN** the wizard opened with n starts on Current branch

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

### Requirement: Saved settings apply without a restart
Saving SHALL write `config.json` and SHALL make every later scan, wizard, worktree creation, editor launch and the Config tab use the saved values, without restarting Vantage. Saving SHALL keep the file readable by hand, with the property names `repositoryRoots`, `worktreeRoot`, `editor` and `defaultBase`.

#### Scenario: Add a root
- **WHEN** the user adds `D:\work` as a second repository root and saves
- **THEN** the Config tab lists both roots at once, and the next worktree scan and the next wizard include repositories under `D:\work`

#### Scenario: Change the worktree root
- **WHEN** the user sets the worktree root to `D:\worktrees` and saves, then starts a session in a new worktree
- **THEN** the worktree is created under `D:\worktrees`

### Requirement: An editor Vantage cannot use reads as VS Code
A custom editor whose arguments do not contain `{folder}`, whose executable is not a full path, or whose executable is a `.cmd` or `.bat` file SHALL be refused when saved from its dialog and SHALL read as VS Code when found in the file; a preset name the file holds that Vantage no longer offers SHALL read as VS Code with every other setting kept. A custom editor in the file whose executable does not exist SHALL be kept as it is, so the editor key reports it rather than another editor opening.

#### Scenario: A batch file in the file
- **WHEN** `config.json` names a custom editor whose executable is `C:\Tools\edit.bat` with arguments `{folder}`
- **THEN** the editor reads as VS Code and the other settings are kept

#### Scenario: A custom executable gone from the file
- **WHEN** `config.json` names a custom editor whose executable is `E:\Tools\subl.exe` with arguments `{folder}` and drive E: is not attached
- **THEN** the editor reads as that custom editor and the Open folder with row shows it

### Requirement: The editor's words say it opens a folder
The Config tab's row and its dialog SHALL caption the editor choice as what opens a folder, and the folder key's hint SHALL say it opens the folder.

#### Scenario: The Config tab's row
- **WHEN** the Config tab is shown
- **THEN** the editor's row reads Open folder with

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
