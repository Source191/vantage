# needs-input-notification Specification

## Purpose
Tells the user, without blocking the session or the terminal, that a session needs them.

## Requirements

### Requirement: The console rings the terminal bell when a session starts waiting
Because the agent_needs_input and agent_completed hook types fire only while the native agent view is open, Vantage SHALL write the terminal bell character to its own terminal whenever a refresh shows a background session in needs input that was in another state at the previous refresh, and SHALL NOT ring for a session that was already waiting, for completed or failed transitions, for interactive sessions, or on the first load.

#### Scenario: Session starts waiting
- **WHEN** a refresh shows a background session in needs input that was working at the previous refresh
- **THEN** the bell rings once in the Vantage tab, so Windows Terminal flags it according to the profile's bell style

#### Scenario: No duplicate bell
- **WHEN** a session was already in needs input at the previous refresh
- **THEN** no bell rings for it until its state changes again

#### Scenario: Interactive sessions ring their own tab
- **WHEN** an interactive session starts waiting
- **THEN** Vantage stays silent, because Claude Code rings the bell in that session's own tab

### Requirement: Claude Code's own bell covers attached tabs
The README SHALL have one Windows Terminal setup section that tells the user to set Claude Code's `preferredNotifChannel` to `terminal_bell`, which makes Claude ring the bell in the tab a session is attached in when it finishes a task or pauses for a permission prompt while the user appears away; SHALL suggest a Windows Terminal bell style that flags the window and taskbar; SHALL explain that the bell sound is a per-profile setting and list suggested sound files shipped in `C:\Windows\Media`; and SHALL give both the setup command and the equivalent manual steps in the Windows Terminal settings.

#### Scenario: Attached session needs permission
- **WHEN** a session attached in a Vantage-opened tab pauses for a permission prompt while the user is in another tab
- **THEN** Claude Code rings the bell in that tab and Windows Terminal flags it

#### Scenario: Choosing a sound by hand
- **WHEN** the user follows the README's manual steps and picks `C:\Windows\Media\Windows Message Nudge.wav` for the Vantage profile
- **THEN** a bell in the Vantage tab plays that sound and bells in other profiles keep theirs

### Requirement: No toasts and no hooks
Vantage SHALL NOT raise Windows toast notifications and SHALL NOT install Claude Code hooks; attention is the terminal bell, the header's needs-input count and the amber rows.

#### Scenario: Install
- **WHEN** Vantage is installed
- **THEN** nothing is written to the user's Claude Code hooks

#### Scenario: Terminal setup
- **WHEN** the user runs `vantage setup-terminal --yes`
- **THEN** nothing is written to the user's Claude Code hooks

### Requirement: The setup command adds Vantage's Windows Terminal profiles
`vantage setup-terminal` SHALL write a Windows Terminal JSON fragment for the current user that adds a "Vantage" profile, which runs the installed Vantage, and a "Vantage session" profile, which runs PowerShell, each with a bell style that sounds, flashes the window and flashes the taskbar. It SHALL NOT change the user's Windows Terminal settings file, SHALL NOT give the "Vantage" profile a tab colour, and SHALL tell the user to restart Windows Terminal.

#### Scenario: First run
- **WHEN** the user runs `vantage setup-terminal --yes` and no fragment exists
- **THEN** the fragment exists with the two profiles, Windows Terminal's settings file is unchanged, the output says to restart Windows Terminal, and the command exits 0

#### Scenario: Run again
- **WHEN** the fragment already exists and the user runs the command again with other sounds
- **THEN** the fragment is replaced with the new sounds and still holds exactly the two profiles

#### Scenario: Vantage tab colour still applies
- **WHEN** Vantage starts in a tab opened with the "Vantage" profile
- **THEN** the tab is titled and coloured as the session-attach spec describes for Vantage's own tab

### Requirement: Setup asks before writing
Unless `--yes` is given, the setup command SHALL show every file it will write or change and what it will put there, and SHALL write nothing until the user answers yes; any other answer, or the end of input, SHALL leave every file as it was and exit 0.

#### Scenario: User declines
- **WHEN** the user runs `vantage setup-terminal` and answers n
- **THEN** no file is written or changed and the output says nothing was written

#### Scenario: Input is not interactive
- **WHEN** the command runs without `--yes` and its input ends before an answer
- **THEN** no file is written or changed

### Requirement: Setup chooses the bell sounds
The "Vantage" profile's bell sound SHALL default to `C:\Windows\Media\Windows Notify Messaging.wav` and the "Vantage session" profile's to `C:\Windows\Media\Windows Proximity Notification.wav`; `--sound` and `--session-sound` SHALL replace them. A default that does not exist on the machine SHALL be left out, so that profile uses Windows Terminal's own bell sound, and the output SHALL say so.

#### Scenario: Custom sound
- **WHEN** the user runs `vantage setup-terminal --yes --sound "C:\Windows\Media\chimes.wav"`
- **THEN** the "Vantage" profile's bell sound is chimes.wav and the "Vantage session" profile keeps its default

#### Scenario: Default sound absent
- **WHEN** `Windows Notify Messaging.wav` does not exist on the machine
- **THEN** the "Vantage" profile has no bell sound and the output says Windows Terminal's default sound will play

### Requirement: A chosen sound must be a wav file that exists
A sound given with `--sound` or `--session-sound` that is not an existing `.wav` file SHALL be a usage error with exit code 2, and nothing SHALL be written.

#### Scenario: Missing sound file
- **WHEN** the user runs `vantage setup-terminal --sound C:\Users\me\missing.wav`
- **THEN** the command prints the usage, names the missing file, writes nothing and exits 2

#### Scenario: Not a wav file
- **WHEN** the user runs `vantage setup-terminal --session-sound C:\Windows\Media\notes.txt` and that file exists
- **THEN** the command prints the usage, writes nothing and exits 2

### Requirement: Setup can turn on Claude Code's terminal bell
The setup command SHALL offer to set `preferredNotifChannel` to `terminal_bell` in `~\.claude\settings.json`, creating the file when it is missing, keeping every other setting and its order, and writing bare LF line endings. When the setting already has that value the command SHALL say so and leave the file untouched; when the file is not valid JSON it SHALL leave it untouched and say why.

#### Scenario: Other settings kept
- **WHEN** settings.json holds a model and a permissions block and the user accepts
- **THEN** settings.json holds the same model and permissions block in the same order, plus `"preferredNotifChannel": "terminal_bell"`, with LF line endings only

#### Scenario: Already set
- **WHEN** settings.json already sets `preferredNotifChannel` to `terminal_bell`
- **THEN** the file is not rewritten and the output says the terminal bell is already on

#### Scenario: Unreadable settings
- **WHEN** settings.json is not valid JSON
- **THEN** the file is unchanged, the output says it could not be read, and the profiles are still written

### Requirement: Setup can be undone
`vantage setup-terminal --remove` SHALL delete Vantage's fragment folder and nothing else, and SHALL report when there was nothing to remove.

#### Scenario: Remove
- **WHEN** the fragment exists and the user runs `vantage setup-terminal --remove`
- **THEN** the fragment folder is gone, `~\.claude\settings.json` is unchanged, and the output says to restart Windows Terminal

#### Scenario: Nothing installed
- **WHEN** no fragment exists and the user runs `vantage setup-terminal --remove`
- **THEN** nothing changes, the output says there was nothing to remove, and the command exits 0
