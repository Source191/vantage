# Spec Delta

## ADDED Requirements

### Requirement: A tab's PowerShell is started by its full path
Every Windows Terminal tab Vantage opens SHALL name PowerShell by the full path of the pwsh.exe found on the PATH as Vantage started, never by its bare name, so a pwsh.exe in the tab's starting folder is never run.

#### Scenario: A planted pwsh.exe in a worktree
- **WHEN** a worktree holds a file named pwsh.exe, PowerShell 7 is installed in C:\Program Files\PowerShell\7 on the PATH, and the user opens a shell tab in that worktree
- **THEN** the tab runs C:\Program Files\PowerShell\7\pwsh.exe and the planted file is never started

## MODIFIED Requirements

### Requirement: Programs are found on the PATH, never in the current directory
Vantage SHALL find git, gh, wt.exe and pwsh.exe once as it starts, by searching the folders of the PATH in order for the program's `.exe`, and SHALL start each by that full path. The folder Vantage was started from SHALL NOT be searched, a PATH entry that is not a full path (such as `.`) SHALL be skipped, here and wherever else Vantage searches the PATH, and a `.cmd` or `.bat` on the PATH SHALL NOT be taken for any of them.

#### Scenario: A planted git.exe in the start folder
- **WHEN** the folder vantage is started from holds a file named git.exe and git is installed in a folder on the PATH
- **THEN** every git command Vantage runs is the installed git, and the planted file is never started

#### Scenario: A relative entry on the PATH
- **WHEN** the PATH starts with `.`, the folder vantage is started from holds a file named git.exe, and git is installed in a later folder on the PATH
- **THEN** every git command Vantage runs is the installed git, and the planted file is never started

#### Scenario: A relative entry before the Azure CLI
- **WHEN** the PATH starts with `.` and the folder vantage is started from holds a file named az.cmd
- **THEN** that file is never started for a pull request lookup

#### Scenario: A program on the PATH
- **WHEN** gh.exe is in a folder listed on the PATH
- **THEN** pull request lookups run that file by its full path

#### Scenario: A batch file of the same name
- **WHEN** the only gh on the PATH is a gh.cmd
- **THEN** gh is treated as not installed
