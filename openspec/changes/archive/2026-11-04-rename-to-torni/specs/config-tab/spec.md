## MODIFIED Requirements

### Requirement: The Config tab shows the setup
After the settings, under the Setup heading row, the table SHALL have a grey row for each piece of setup Torni reads but does not edit: Settings file (the full path of `config.json`), Windows Terminal profiles (installed, with the fragment's path, or not set up), Claude bell (on, the other channel named, or not set), Diagnostic log (the `TORNI_LOG` path, or off) and Last update check (when, and the newest release found, or never, or off).

#### Scenario: Everything set up
- **WHEN** the fragment exists, Claude's settings say terminal_bell, `TORNI_LOG` is unset and nuget.org was asked at 09:12 today and named 0.18.0
- **THEN** the setup rows read the settings file's path, installed with the fragment's path, on, off and 09:12 today, newest 0.18.0

#### Scenario: Nothing set up
- **WHEN** the fragment does not exist and Claude's settings file has no preferredNotifChannel
- **THEN** Windows Terminal profiles reads not set up, run torni setup-terminal, and Claude bell reads not set, run torni setup-terminal

#### Scenario: Another channel
- **WHEN** Claude's settings say iterm2
- **THEN** Claude bell reads iterm2, not the terminal bell

### Requirement: A setup row says how to change it
Enter on a setup row SHALL change nothing and the status line SHALL say how that setup is changed: `torni setup-terminal` for the profiles and the bell, the `TORNI_LOG` environment variable for the log, Check for updates for the update check, and that the settings file is written by this tab.

#### Scenario: Profiles not set up
- **WHEN** the user presses Enter on Windows Terminal profiles
- **THEN** the status line says to run torni setup-terminal and restart Windows Terminal

### Requirement: The Config tab reads the setup when it is shown
The setup rows SHALL be read each time the Config tab is shown and after each save, off the UI thread, and the setting rows SHALL never wait for them; a setup row whose read fails SHALL say it could not be read.

#### Scenario: Profiles installed elsewhere meanwhile
- **WHEN** the user runs torni setup-terminal in another tab and then shows the Config tab
- **THEN** Windows Terminal profiles reads installed

#### Scenario: Unreadable Claude settings
- **WHEN** Claude's settings file is not valid JSON
- **THEN** Claude bell reads could not be read, and every other row is shown as usual
