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

#### Scenario: A path holding a NUL character
- **WHEN** `config.json` holds `repositoryRoots` of `C:\Users\me\source\repos` and a second root whose text holds a NUL character, and a `worktreeRoot` whose text holds one too
- **THEN** Vantage starts, scans `C:\Users\me\source\repos` alone, puts new worktrees under `C:\Users\me\source\worktrees`, and does not exit

#### Scenario: The default place by hand
- **WHEN** `config.json` holds `defaultPlace` of `CurrentBranch`
- **THEN** the wizard opened with n starts on Current branch
