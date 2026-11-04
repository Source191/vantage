# Spec Delta

## MODIFIED Requirements

### Requirement: Settings live in one file with defaults
Torni SHALL keep its settings in `config.json` in its state folder: the repository roots, the worktree root, the editor, the default worktree base, the default place for a new session and whether the diagnostic log is written to a file. A missing, corrupt or unreadable file, a number where a base or place name belongs, or a missing property SHALL read as the default: roots `<home>\source\repos`, worktree root `worktrees` beside the first repository root, editor VS Code, base the remote default branch, place a new worktree, diagnostic log off.

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

#### Scenario: A file Torni may not read
- **WHEN** `config.json` exists but the user's account is denied reading its contents
- **THEN** Torni starts with every default and does not exit

#### Scenario: The diagnostic log by hand
- **WHEN** `config.json` holds `diagnosticLog` of `true`
- **THEN** Torni writes its diagnostic log to `%LOCALAPPDATA%\Torni\torni.log` from its first event, and the Config tab's Diagnostic log row reads on

#### Scenario: Not a true or false
- **WHEN** `config.json` holds `diagnosticLog` of `"yes"`
- **THEN** Torni starts with every default, as it does for a corrupt file, and writes no log file

### Requirement: Saved settings apply without a restart
Saving SHALL write `config.json` and SHALL make every later scan, wizard, worktree creation, editor launch, diagnostic event and the Config tab use the saved values, without restarting Torni. Saving SHALL keep the file readable by hand, with the property names `repositoryRoots`, `worktreeRoot`, `editor`, `defaultBase` and `diagnosticLog`.

#### Scenario: Add a root
- **WHEN** the user adds `D:\work` as a second repository root and saves
- **THEN** the Config tab lists both roots at once, and the next worktree scan and the next wizard include repositories under `D:\work`

#### Scenario: Change the worktree root
- **WHEN** the user sets the worktree root to `D:\worktrees` and saves, then starts a session in a new worktree
- **THEN** the worktree is created under `D:\worktrees`

#### Scenario: Turn the diagnostic log on
- **WHEN** the user turns Diagnostic log on and then stops a session
- **THEN** `config.json` holds `"diagnosticLog": true` and `torni.log` gains the line for the stop, without a restart
