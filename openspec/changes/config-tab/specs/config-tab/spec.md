# Spec Delta

## Purpose
The Config tab: a table of Vantage's settings and of the setup it depends on, where each setting is edited, added or removed a row at a time and each piece of setup says what to run when it is missing.

## ADDED Requirements

### Requirement: The Config tab lists the settings
The console SHALL have a third tab, titled Config, whose table has the columns SETTING and VALUE and a row per setting in this order: one Repository root row per root, Worktree root, Open folder with, Default base and Check for updates. Each value SHALL be shown whole where it fits and elided in the middle where it does not.

#### Scenario: Default settings
- **WHEN** `config.json` does not exist and the user presses 3
- **THEN** the rows read Repository root C:\Users\me\source\repos, Worktree root C:\Users\me\source\worktrees, Open folder with VS Code, Default base origin's default branch and Check for updates on

#### Scenario: Two roots
- **WHEN** the roots are C:\Users\me\source\repos and D:\work
- **THEN** there are two Repository root rows, C:\Users\me\source\repos first

#### Scenario: A custom editor
- **WHEN** the editor is the executable C:\Tools\subl.exe with arguments -n {folder}
- **THEN** the Open folder with row reads C:\Tools\subl.exe -n {folder}

### Requirement: The Config tab shows the setup
After the settings the table SHALL have a grey row for each piece of setup Vantage reads but does not edit: Settings file (the full path of `config.json`), Windows Terminal profiles (installed, with the fragment's path, or not set up), Claude bell (on, the other channel named, or not set), Diagnostic log (the `VANTAGE_LOG` path, or off) and Last update check (when, and the newest release found, or never, or off).

#### Scenario: Everything set up
- **WHEN** the fragment exists, Claude's settings say terminal_bell, `VANTAGE_LOG` is unset and nuget.org was asked at 09:12 today and named 0.18.0
- **THEN** the setup rows read the settings file's path, installed with the fragment's path, on, off and 09:12 today, newest 0.18.0

#### Scenario: Nothing set up
- **WHEN** the fragment does not exist and Claude's settings file has no preferredNotifChannel
- **THEN** Windows Terminal profiles reads not set up, run vantage setup-terminal, and Claude bell reads not set, run vantage setup-terminal

#### Scenario: Another channel
- **WHEN** Claude's settings say iterm2
- **THEN** Claude bell reads iterm2, not the terminal bell

### Requirement: A setting is edited a row at a time
Enter on a Repository root, Worktree root, Open folder with or Default base row SHALL open a dialog for that one value, showing it as saved; F5 or Save SHALL check it, name any problem and keep the dialog open, or write `config.json` and close; Esc SHALL close it unchanged. Enter on Check for updates SHALL turn it on or off and save at once.

#### Scenario: Change the worktree root
- **WHEN** the user selects Worktree root, presses Enter, types D:\worktrees and presses F5
- **THEN** `config.json` holds D:\worktrees, the row reads it at once, and the footer says it was saved

#### Scenario: Cancel an edit
- **WHEN** the user edits a Repository root to D:\work and presses Esc
- **THEN** `config.json` and the row are unchanged

#### Scenario: Turn the update check off
- **WHEN** Check for updates is on and the user presses Enter on it
- **THEN** it reads off, `config.json` says so, and the Last update check row reads off

### Requirement: Repository roots are added and removed
The a key SHALL open the root dialog empty and add the root it saves after the others; the d key on a Repository root row SHALL remove that root and save. Removing the only root SHALL be refused with the reason in the footer, and adding a root already listed SHALL be refused in the dialog.

#### Scenario: Add a root
- **WHEN** the user presses a, types D:\work, which exists, and presses F5
- **THEN** D:\work is the last Repository root row and the next worktree scan includes it

#### Scenario: Remove a root
- **WHEN** the roots are C:\Users\me\source\repos and D:\work and the user presses d on D:\work
- **THEN** only C:\Users\me\source\repos is left and `config.json` says so

#### Scenario: The last root
- **WHEN** there is one root and the user presses d on it
- **THEN** nothing changes and the footer says Vantage needs at least one repository root

#### Scenario: A root twice
- **WHEN** the user presses a and enters C:\Users\me\source\repos, already a root
- **THEN** nothing is saved and the dialog says it is already a root

### Requirement: A setup row says how to change it
Enter on a setup row SHALL change nothing and the footer SHALL say how that setup is changed: `vantage setup-terminal` for the profiles and the bell, the `VANTAGE_LOG` environment variable for the log, Check for updates for the update check, and that the settings file is written by this tab. The d key on any row but a Repository root SHALL do nothing but say so.

#### Scenario: Profiles not set up
- **WHEN** the user presses Enter on Windows Terminal profiles
- **THEN** the footer says to run vantage setup-terminal and restart Windows Terminal

#### Scenario: Delete on a setup row
- **WHEN** the user presses d on Diagnostic log
- **THEN** nothing changes and the footer says only a repository root can be removed

### Requirement: The Config tab's counts and keys
Beside the tab titles the Config tab SHALL count its settings and, when any is missing, the setup to do, the latter in amber. Its key bar SHALL read Enter Edit, a Add root and, on a root row, d Remove root, then c Copy, e Open folder, / Filter, ? Help, ! Report and q Quit. c SHALL copy a path value whole; e SHALL open a folder value in the editor, and the folder holding a file value; on a value that is not a path both SHALL say so.

#### Scenario: Counts
- **WHEN** there are two roots and the Windows Terminal profiles are not set up
- **THEN** the counts read 6 settings · 1 to set up, the second part in amber

#### Scenario: Copy a long root
- **WHEN** a Repository root row's value is elided and the user presses c
- **THEN** the clipboard holds the full path

#### Scenario: Open the settings file's folder
- **WHEN** the user presses e on Settings file
- **THEN** the folder holding `config.json` opens with the folder opener

### Requirement: The Config tab reads the setup when it is shown
The setup rows SHALL be read each time the Config tab is shown and after each save, off the UI thread, and the setting rows SHALL never wait for them; a setup row whose read fails SHALL say it could not be read.

#### Scenario: Profiles installed elsewhere meanwhile
- **WHEN** the user runs vantage setup-terminal in another tab and then shows the Config tab
- **THEN** Windows Terminal profiles reads installed

#### Scenario: Unreadable Claude settings
- **WHEN** Claude's settings file is not valid JSON
- **THEN** Claude bell reads could not be read, and every other row is shown as usual
