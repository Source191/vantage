## MODIFIED Requirements

### Requirement: A setting is edited a row at a time
Enter on a Worktree root, Open folder with or Default base row SHALL open a dialog for that one value, and Enter on any repository root row a dialog holding every root, one per line, each showing the setting as saved; F5 or Save SHALL check it, name any problem and keep the dialog open, or write `config.json` and close; Esc SHALL close it unchanged. Enter on Check for updates SHALL turn it on or off and save at once.

#### Scenario: Change the worktree root
- **WHEN** the user selects Worktree root, presses Enter, types D:\worktrees and presses F5
- **THEN** `config.json` holds D:\worktrees, the row reads it at once, and the status line says it was saved

#### Scenario: Cancel an edit
- **WHEN** the user adds D:\work to the repository roots and presses Esc
- **THEN** `config.json` and the row are unchanged

#### Scenario: Turn the update check off
- **WHEN** Check for updates is on and the user presses Enter on it
- **THEN** it reads off, `config.json` says so, and the Last update check row reads off

### Requirement: A setup row says how to change it
Enter on a setup row SHALL change nothing and the status line SHALL say how that setup is changed: `vantage setup-terminal` for the profiles and the bell, the `VANTAGE_LOG` environment variable for the log, Check for updates for the update check, and that the settings file is written by this tab.

#### Scenario: Profiles not set up
- **WHEN** the user presses Enter on Windows Terminal profiles
- **THEN** the status line says to run vantage setup-terminal and restart Windows Terminal
