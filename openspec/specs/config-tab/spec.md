# config-tab Specification

## Purpose
The Config tab: a table of Vantage's settings and of the setup it depends on, where each setting is edited in a dialog of its own, the repository roots together, and each piece of setup says what to run when it is missing.

## Requirements

### Requirement: The Config tab lists the settings
The console SHALL have a third tab, titled Config, whose table has the columns SETTING and VALUE and a row per setting in this order: one row per repository root, the first labelled Repository roots and the others with SETTING blank, then Worktree root, Open folder with, Default base and Check for updates. Each value SHALL be shown whole where it fits and elided in the middle where it does not.

#### Scenario: Default settings
- **WHEN** `config.json` does not exist and the user presses 3
- **THEN** the rows read Repository roots C:\Users\me\source\repos, Worktree root C:\Users\me\source\worktrees, Open folder with VS Code, Default base origin's default branch and Check for updates on

#### Scenario: Two roots
- **WHEN** the roots are C:\Users\me\source\repos and D:\work
- **THEN** the first row reads Repository roots C:\Users\me\source\repos and the second has SETTING blank and VALUE D:\work

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

### Requirement: Repository roots are added and removed in their dialog
The repository roots dialog SHALL list every root, one per line; a line added SHALL add a root after the others and a line deleted SHALL remove that root when the dialog saves. A dialog left with no root, or with a root listed twice, SHALL be refused in the dialog. No other key SHALL add or remove a root.

#### Scenario: Add a root
- **WHEN** the user presses Enter on a repository root row, adds the line D:\work, which exists, and presses F5
- **THEN** D:\work is the last repository root row and the next worktree scan includes it

#### Scenario: Remove a root
- **WHEN** the roots are C:\Users\me\source\repos and D:\work and the user deletes the D:\work line and presses F5
- **THEN** only C:\Users\me\source\repos is left and `config.json` says so

#### Scenario: No root left
- **WHEN** the user deletes every line and presses F5
- **THEN** nothing is saved and the dialog asks for a repository root

#### Scenario: A root twice
- **WHEN** the user adds a second line C:\Users\me\source\repos, already a root
- **THEN** nothing is saved and the dialog says it is already a repository root

### Requirement: A setup row says how to change it
Enter on a setup row SHALL change nothing and the status line SHALL say how that setup is changed: `vantage setup-terminal` for the profiles and the bell, the `VANTAGE_LOG` environment variable for the log, Check for updates for the update check, and that the settings file is written by this tab.

#### Scenario: Profiles not set up
- **WHEN** the user presses Enter on Windows Terminal profiles
- **THEN** the status line says to run vantage setup-terminal and restart Windows Terminal

### Requirement: The Config tab's counts and keys
Beside the tab titles the Config tab SHALL count its settings and, when any is missing, the setup to do, the latter in amber. Its key bar SHALL read Enter Edit, then c Copy, e Open folder, / Filter, ? Help, ! Report and q Quit. c SHALL copy a path value whole; e SHALL open a folder value in the editor, and the folder holding a file value; on a value that is not a path both SHALL say so.

#### Scenario: Counts
- **WHEN** there are two roots and the Windows Terminal profiles are not set up
- **THEN** the counts read 6 settings · 1 to set up, the second part in amber

#### Scenario: Copy a long root
- **WHEN** a repository root row's value is elided and the user presses c
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

### Requirement: Each setting describes itself
Every Config row SHALL have a description of what the setting does or what the setup is for. The status line SHALL show the selected row's description whenever no other message is shown, in place of a fixed hint, and resting the mouse on a row SHALL show the same description as a tooltip.

#### Scenario: Status line for the selected row
- **WHEN** the user selects Worktree root and no message is shown
- **THEN** the status line says where new worktrees go and how their folders are named

#### Scenario: The description follows the selection
- **WHEN** the user moves from Worktree root to Check for updates
- **THEN** the status line changes to Check for updates' description

#### Scenario: Tooltip on hover
- **WHEN** the mouse rests on the Claude bell row
- **THEN** a tooltip says the bell rings when a session finishes or needs permission while you are away

#### Scenario: A message wins
- **WHEN** a setting has just been saved
- **THEN** the status line says it was saved, and goes back to the description when the selection moves

### Requirement: The repository roots dialog browses for folders
The repository roots dialog SHALL offer Browse (F3), which opens a folder picker inside the terminal that can choose several folders; each folder chosen SHALL be added as a new line after the roots already listed, and the dialog SHALL check the list on F5 as it does for typed roots. Cancelling the picker SHALL add nothing.

#### Scenario: Add a root by browsing
- **WHEN** the roots are C:\Users\me\source\repos and the user presses F3, chooses D:\work and confirms
- **THEN** the roots box reads C:\Users\me\source\repos and D:\work on its own line, and nothing is saved until F5

#### Scenario: Two folders at once
- **WHEN** the user chooses D:\work and E:\personal in the picker
- **THEN** both are added, each on a line of its own, in the order chosen

#### Scenario: Cancel the picker
- **WHEN** the user presses F3 and then Esc in the picker
- **THEN** the roots box is unchanged

#### Scenario: A folder already listed
- **WHEN** the user browses to C:\Users\me\source\repos, already a root, and presses F5
- **THEN** nothing is saved and the dialog says it is already a repository root

### Requirement: The worktree root dialog browses for a folder
The worktree root dialog SHALL offer Browse (F3), which opens the same folder picker for one folder, starting in the folder holding the current worktree root; the folder chosen SHALL replace the path in the box, and the dialog SHALL check it on F5 as it does for a typed path. Cancelling the picker SHALL change nothing.

#### Scenario: Choose the worktree root by browsing
- **WHEN** the worktree root is C:\Users\me\source\worktrees and the user presses F3, chooses D:\trees and confirms
- **THEN** the box reads D:\trees, and nothing is saved until F5

#### Scenario: Cancel the picker for the worktree root
- **WHEN** the user presses F3 in the worktree root dialog and then Esc in the picker
- **THEN** the box is unchanged

#### Scenario: The picker starts beside the worktree root
- **WHEN** the worktree root is C:\Users\me\source\worktrees and the user presses F3
- **THEN** the picker opens in C:\Users\me\source

### Requirement: Default place is a setting row
The Config tab SHALL show a Default place row after Default base, reading New worktree or Current branch; Enter on it SHALL open a dialog to choose between them, and saving SHALL write `config.json` and apply to the next wizard at once.

#### Scenario: Default
- **WHEN** `config.json` has no default place
- **THEN** the Default place row reads New worktree

#### Scenario: Choose the current branch
- **WHEN** the user presses Enter on Default place, chooses Current branch and presses F5
- **THEN** the row reads Current branch and the next wizard opened with n starts on Current branch

### Requirement: The last update check's time is local
The Last update check row SHALL show when nuget.org was last asked in the user's local time zone, using the offset that zone had at that moment, and SHALL say today only when that moment falls on the current local date.

#### Scenario: A check this morning in summer
- **WHEN** the local time zone is the UK's, it is 15 July and nuget.org was asked at 08:12 UTC that day and named 0.18.0
- **THEN** the row reads 09:12 today, newest 0.18.0

#### Scenario: A check before the clocks went back
- **WHEN** the local time zone is the UK's, it is 1 November and nuget.org was last asked at 08:12 UTC on 24 October
- **THEN** the row reads 24 October 2026 09:12
