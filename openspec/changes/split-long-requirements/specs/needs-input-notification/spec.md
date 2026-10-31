# Spec Delta

## MODIFIED Requirements

### Requirement: Claude Code's own bell covers attached tabs
The README SHALL have one Windows Terminal setup section that tells the user to set Claude Code's `preferredNotifChannel` to `terminal_bell`, which makes Claude ring the bell in the tab a session is attached in when it finishes a task or pauses for a permission prompt while the user appears away.

#### Scenario: Attached session needs permission
- **WHEN** a session attached in a Vantage-opened tab pauses for a permission prompt while the user is in another tab
- **THEN** Claude Code rings the bell in that tab and Windows Terminal flags it

#### Scenario: Choosing a sound by hand
- **WHEN** the user follows the README's manual steps and picks `C:\Windows\Media\Windows Message Nudge.wav` for the Vantage profile
- **THEN** a bell in the Vantage tab plays that sound and bells in other profiles keep theirs

### Requirement: The setup command adds Vantage's Windows Terminal profiles
`vantage setup-terminal` SHALL write a Windows Terminal JSON fragment for the current user that adds a "Vantage" profile, which runs the installed Vantage, and a "Vantage session" profile, which runs PowerShell, each with a bell style that sounds, flashes the window and flashes the taskbar, and each with the Vantage icon as its tab icon.

#### Scenario: First run
- **WHEN** the user runs `vantage setup-terminal --yes` and no fragment exists
- **THEN** the fragment exists with the two profiles, each naming the copied icon file, Windows Terminal's settings file is unchanged, the output says to restart Windows Terminal, and the command exits 0

#### Scenario: Run again
- **WHEN** the fragment already exists and the user runs the command again with other sounds
- **THEN** the fragment is replaced with the new sounds and still holds exactly the two profiles, each with the icon

#### Scenario: No icon file beside the tool
- **WHEN** the user runs `vantage setup-terminal --yes` and the tool's icon file is missing
- **THEN** the fragment holds the two profiles without an icon property, no error is shown, and the command exits 0

#### Scenario: Remove takes the icon too
- **WHEN** the fragment exists with its icon and the user runs `vantage setup-terminal --remove`
- **THEN** the fragment folder, including the icon copy, is gone

#### Scenario: Vantage tab colour still applies
- **WHEN** Vantage starts in a tab opened with the "Vantage" profile
- **THEN** the tab is titled and coloured as the session-attach spec describes for Vantage's own tab

## ADDED Requirements

### Requirement: The README's setup section explains the bell's style and sound
The README's Windows Terminal setup section SHALL suggest a bell style that flags the window and taskbar, SHALL explain that the bell sound is a per-profile setting and list suggested sound files shipped in `C:\Windows\Media`, and SHALL give both the setup command and the equivalent manual steps in the Windows Terminal settings.

#### Scenario: Reading the setup section
- **WHEN** the user reads the README's Windows Terminal setup section
- **THEN** it names `vantage setup-terminal`, gives the manual steps through a profile's Advanced settings (bell notification style and bell sound), and names at least one sound file in `C:\Windows\Media`

### Requirement: The profiles' icon lives in Vantage's fragment folder
The setup command SHALL copy the icon file into Vantage's fragment folder, so that removing the folder removes it; when the tool carries no icon file the profiles SHALL be written without an icon and setup SHALL still succeed.

#### Scenario: The icon beside the fragment
- **WHEN** the user runs `vantage setup-terminal --yes` and the tool carries its icon file
- **THEN** a copy of the icon sits in the fragment's own folder and both profiles name that copy

### Requirement: Setup leaves Windows Terminal's own settings alone
The setup command SHALL NOT change the user's Windows Terminal settings file, SHALL NOT give the "Vantage" profile a tab colour, and SHALL tell the user to restart Windows Terminal.

#### Scenario: Settings file and tab colour untouched
- **WHEN** the user runs `vantage setup-terminal --yes`
- **THEN** Windows Terminal's settings file is unchanged, the "Vantage" profile in the fragment has no tab colour, and the output says to restart Windows Terminal
