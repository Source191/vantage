## MODIFIED Requirements

### Requirement: Settings live in one file with defaults
Vantage SHALL keep its settings in `config.json` in its state folder: the repository roots, the worktree root, the editor, the default worktree base and the default place for a new session. A missing file, a corrupt file or a missing property SHALL read as the default: roots `<home>\source\repos`, worktree root `worktrees` beside the first repository root, editor VS Code, base the remote default branch, place a new worktree. A file Vantage is not allowed to read, and a number where a base or place name belongs, SHALL read as a corrupt file does.

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

#### Scenario: A number where a name belongs
- **WHEN** `config.json` holds `repositoryRoots` of `D:\work` and `defaultBase` of `7`
- **THEN** Vantage starts with every default, as it does for a corrupt file, the Config tab draws and the wizard opens, and nothing exits

#### Scenario: A file Vantage may not read
- **WHEN** `config.json` exists but the user's account is denied reading its contents
- **THEN** Vantage starts with every default and does not exit

## ADDED Requirements

### Requirement: A saved settings file is replaced whole
Saving SHALL write the new `config.json` beside the old one and replace it in one step, so a save that is interrupted leaves the previous settings in place and no partly written file is ever read. Another Vantage process reading `config.json` SHALL NOT stop a save, and a save SHALL leave no temporary file behind.

#### Scenario: An interrupted save
- **WHEN** Vantage is ended while it is saving the repository roots `C:\Users\me\source\repos` and `D:\work`
- **THEN** the next start reads either the roots as they were before the save or both new roots, never no roots

#### Scenario: Another process is reading the file
- **WHEN** `vantage here` is reading `config.json` as the console saves a new worktree root
- **THEN** the save completes, the new worktree root is read by the next reader, and only `config.json` is left in the state folder
