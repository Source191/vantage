# external-tools Specification

## Purpose
How Vantage finds and starts the programs it depends on, Claude Code, git, the GitHub and Azure CLIs, Windows Terminal and PowerShell, so that nothing is ever run from the folder Vantage was started in and no batch file is ever handed text it could read as commands.

## Requirements

### Requirement: Programs are found on the PATH, never in the current directory
Vantage SHALL find git, gh, wt.exe and pwsh.exe once as it starts, by searching the folders of the PATH in order for the program's `.exe`, and SHALL start each by that full path. The folder Vantage was started from SHALL NOT be searched, and a `.cmd` or `.bat` on the PATH SHALL NOT be taken for any of them.

#### Scenario: A planted git.exe in the start folder
- **WHEN** the folder vantage is started from holds a file named git.exe and git is installed in a folder on the PATH
- **THEN** every git command Vantage runs is the installed git, and the planted file is never started

#### Scenario: A program on the PATH
- **WHEN** gh.exe is in a folder listed on the PATH
- **THEN** pull request lookups run that file by its full path

#### Scenario: A batch file of the same name
- **WHEN** the only gh on the PATH is a gh.cmd
- **THEN** gh is treated as not installed

### Requirement: A missing program is reported where it is needed
When no folder on the PATH holds git.exe, the console and `vantage here` SHALL print one sentence saying so and exit 1 without starting anything. When none holds gh.exe, GitHub lookups SHALL report the CLI as missing without starting a process. When none holds wt.exe or pwsh.exe, the key that needs it SHALL fail with a sentence naming the program, and nothing else SHALL change.

#### Scenario: git missing
- **WHEN** no folder on the PATH holds git.exe and the user runs vantage
- **THEN** standard error says git was not found on the PATH and the exit code is 1

#### Scenario: wt.exe missing
- **WHEN** no folder on the PATH holds wt.exe and the user presses the open-in-tab key
- **THEN** the footer reports that Windows Terminal (wt.exe) was not found on the PATH, and no tab opens

#### Scenario: pwsh.exe missing
- **WHEN** no folder on the PATH holds pwsh.exe and the user presses the open-solution key on a row with one solution
- **THEN** the footer reports that PowerShell 7 (pwsh.exe) was not found on the PATH, and nothing opens

### Requirement: Claude Code is run only as an executable
Vantage SHALL run Claude Code from `~\.local\bin\claude.exe` when that file exists, else from the first `claude.exe` in a folder on the PATH. It SHALL NOT run a `claude.cmd` or `claude.bat`: when only one of those is found, the console and `vantage here` SHALL print a sentence naming that file and the native installer and exit 1, and when nothing is found they SHALL say Claude Code was not found and exit 1.

#### Scenario: Native install
- **WHEN** ~\.local\bin\claude.exe exists
- **THEN** every Claude Code command Vantage runs starts that file, whatever else is on the PATH

#### Scenario: Only the npm shim
- **WHEN** no claude.exe exists and C:\Users\me\AppData\Roaming\npm\claude.cmd is on the PATH
- **THEN** standard error names that file, says Vantage does not run Claude Code through a batch file, names the native installer, and the exit code is 1

#### Scenario: Not installed
- **WHEN** neither claude.exe nor claude.cmd can be found
- **THEN** standard error says Claude Code was not found and the exit code is 1

### Requirement: A batch file is given only plain text
The Azure CLI, which Windows runs as a batch file through cmd.exe, SHALL be given only values made of letters, digits, spaces, dots, underscores, hyphens and slashes; a value holding any other character SHALL stop that command from being run at all. The pull-requests capability says what the remote names and the branch name do when they fail this rule.

#### Scenario: Ampersand in a value
- **WHEN** a value that would reach az holds an ampersand
- **THEN** az is not started for it

#### Scenario: Plain value
- **WHEN** a value that would reach az is feature/refund-retry
- **THEN** az is started with it
