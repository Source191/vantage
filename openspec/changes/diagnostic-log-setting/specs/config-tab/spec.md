# Spec Delta

## MODIFIED Requirements

### Requirement: The Config tab lists the settings
The console SHALL have a third tab, titled Config, whose table has the columns SETTING and VALUE and a row per setting in this order: one row per repository root, the first labelled Repository roots and the others with SETTING blank, then Worktree root, Open folder with, Default base, Check for updates and Diagnostic log. Each value SHALL be shown whole where it fits and elided in the middle where it does not.

#### Scenario: Default settings
- **WHEN** `config.json` does not exist and the user shows the Config tab
- **THEN** the rows read Repository roots C:\Users\me\source\repos, Worktree root C:\Users\me\source\worktrees, Open folder with VS Code, Default base origin's default branch, Check for updates on and Diagnostic log off

#### Scenario: Two roots
- **WHEN** the roots are C:\Users\me\source\repos and D:\work
- **THEN** the first row reads Repository roots C:\Users\me\source\repos and the second has SETTING blank and VALUE D:\work

#### Scenario: A custom editor
- **WHEN** the editor is the executable C:\Tools\subl.exe with arguments -n {folder}
- **THEN** the Open folder with row reads C:\Tools\subl.exe -n {folder}

### Requirement: The Config tab shows the setup
After the settings, under the Setup heading row, the table SHALL have a grey row for each piece of setup Torni reads but does not edit: Settings file (the full path of `config.json`), Windows Terminal profiles (installed, with the fragment's path, or not set up), Claude bell (on, the other channel named, or not set) and Last update check (when, and the newest release found, or never, or off).

#### Scenario: Everything set up
- **WHEN** the fragment exists, Claude's settings say terminal_bell and nuget.org was asked at 09:12 today and named 0.18.0
- **THEN** the setup rows read the settings file's path, installed with the fragment's path, on and 09:12 today, newest 0.18.0

#### Scenario: Nothing set up
- **WHEN** the fragment does not exist and Claude's settings file has no preferredNotifChannel
- **THEN** Windows Terminal profiles reads not set up, run torni setup-terminal, and Claude bell reads not set, run torni setup-terminal

#### Scenario: Another channel
- **WHEN** Claude's settings say iterm2
- **THEN** Claude bell reads iterm2, not the terminal bell

### Requirement: A setting is edited a row at a time
Enter on a Worktree root, Open folder with or Default base row SHALL open a dialog for that one value, and Enter on any repository root row a dialog holding every root, one per line, each showing the setting as saved; F5 or Save SHALL check it, name any problem and keep the dialog open, or write `config.json` and close; Esc SHALL close it unchanged. Enter on Check for updates or Diagnostic log SHALL turn it on or off and save at once.

#### Scenario: Change the worktree root
- **WHEN** the user selects Worktree root, presses Enter, types D:\worktrees and presses F5
- **THEN** `config.json` holds D:\worktrees, the row reads it at once, and the status line says it was saved

#### Scenario: Cancel an edit
- **WHEN** the user adds D:\work to the repository roots and presses Esc
- **THEN** `config.json` and the row are unchanged

#### Scenario: Turn the update check off
- **WHEN** Check for updates is on and the user presses Enter on it
- **THEN** it reads off, `config.json` says so, and the Last update check row reads off

#### Scenario: Turn the diagnostic log on
- **WHEN** Diagnostic log is off and the user presses Enter on it
- **THEN** it reads on, `config.json` says so, and the status line says the log is now written to the full path of `torni.log`

### Requirement: A setup row says how to change it
Enter on a setup row SHALL change nothing and the status line SHALL say how that setup is changed: `torni setup-terminal` for the profiles and the bell, Check for updates for the update check, and that the settings file is written by this tab.

#### Scenario: Profiles not set up
- **WHEN** the user presses Enter on Windows Terminal profiles
- **THEN** the status line says to run torni setup-terminal and restart Windows Terminal

### Requirement: The Config tab's counts and keys
Beside the tab titles the Config tab SHALL count its settings and, when any is missing, the setup to do, the latter in amber. Its key bar SHALL read Enter Edit, then c Copy, f Open folder, / Filter, ? Help, ! Report and q Quit. c SHALL copy a path value whole; f SHALL open a folder value in the editor, and the folder holding a file value; on a value that is not a path both SHALL say so.

#### Scenario: Counts
- **WHEN** there are two roots and the Windows Terminal profiles are not set up
- **THEN** the counts read 8 settings · 1 to set up, the second part in amber

#### Scenario: Copy a long root
- **WHEN** a repository root row's value is elided and the user presses c
- **THEN** the clipboard holds the full path

#### Scenario: Open the settings file's folder
- **WHEN** the user presses f on Settings file
- **THEN** the folder holding `config.json` opens with the folder opener

### Requirement: The setup sits under its own heading
Between the settings and the setup rows the Config tab's table SHALL have a heading row reading Setup in SETTING and nothing in VALUE. The selection SHALL never rest on it: moving up or down steps over it, and a load never selects it. While a filter is set it SHALL be hidden. It SHALL count neither as a setting in the counts nor as a row in the table's title.

#### Scenario: The heading between the two parts
- **WHEN** the user shows the Config tab with the default settings
- **THEN** the row after Diagnostic log reads Setup, and the row after that Settings file, drawn grey

#### Scenario: Down steps over the heading
- **WHEN** Diagnostic log is selected and the user presses Down
- **THEN** Settings file is selected

#### Scenario: Up steps over the heading
- **WHEN** Settings file is selected and the user presses Up
- **THEN** Diagnostic log is selected

#### Scenario: A filter hides the heading
- **WHEN** the user filters by bell
- **THEN** the table shows only Claude bell, with no Setup heading

#### Scenario: The heading is not counted
- **WHEN** there is one root and every piece of setup is in place
- **THEN** the counts read 7 settings and the title counts 11 rows

## ADDED Requirements

### Requirement: The Diagnostic log row names its file
The Diagnostic log row's tooltip SHALL say what the setting does and give the full path of the log file, `%LOCALAPPDATA%\Torni\torni.log` expanded for the user, whether the setting is on or off. Its VALUE SHALL read on or off and nothing else.

#### Scenario: Hover on the row
- **WHEN** the user's local application data folder is C:\Users\me\AppData\Local and the mouse rests on the Diagnostic log row
- **THEN** the tooltip names C:\Users\me\AppData\Local\Torni\torni.log and says Enter turns the log on or off

#### Scenario: The value stays short
- **WHEN** the setting is on
- **THEN** the row's VALUE reads on, with no path
