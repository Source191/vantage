## MODIFIED Requirements

### Requirement: The setup command adds Vantage's Windows Terminal profiles
`vantage setup-terminal` SHALL write a Windows Terminal JSON fragment for the current user that adds a "Vantage" profile, which runs the installed Vantage, and a "Vantage session" profile, which runs PowerShell, each with a bell style that sounds, flashes the window and flashes the taskbar, and each with the Vantage icon as its tab icon. The icon file SHALL be copied into Vantage's fragment folder so that removing the folder removes it; when the tool carries no icon file the profiles SHALL be written without an icon and setup SHALL still succeed. It SHALL NOT change the user's Windows Terminal settings file, SHALL NOT give the "Vantage" profile a tab colour, and SHALL tell the user to restart Windows Terminal.

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
