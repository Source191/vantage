# Spec Delta

## MODIFIED Requirements

### Requirement: The Config tab shows the setup
After the settings, under the Setup heading row, the table SHALL have a grey row for each piece of setup Vantage reads but does not edit: Settings file (the full path of `config.json`), Windows Terminal profiles (installed, with the fragment's path, or not set up), Claude bell (on, the other channel named, or not set), Diagnostic log (the `VANTAGE_LOG` path, or off) and Last update check (when, and the newest release found, or never, or off).

#### Scenario: Everything set up
- **WHEN** the fragment exists, Claude's settings say terminal_bell, `VANTAGE_LOG` is unset and the update source was asked at 09:12 today and named 0.18.0
- **THEN** the setup rows read the settings file's path, installed with the fragment's path, on, off and 09:12 today, newest 0.18.0

#### Scenario: Nothing set up
- **WHEN** the fragment does not exist and Claude's settings file has no preferredNotifChannel
- **THEN** Windows Terminal profiles reads not set up, run vantage setup-terminal, and Claude bell reads not set, run vantage setup-terminal

#### Scenario: Another channel
- **WHEN** Claude's settings say iterm2
- **THEN** Claude bell reads iterm2, not the terminal bell

### Requirement: The last update check's time is local
The Last update check row SHALL show when the update source was last asked in the user's local time zone, using the offset that zone had at that moment, and SHALL say today only when that moment falls on the current local date.

#### Scenario: A check this morning in summer
- **WHEN** the local time zone is the UK's, it is 15 July and the update source was asked at 08:12 UTC that day and named 0.18.0
- **THEN** the row reads 09:12 today, newest 0.18.0

#### Scenario: A check before the clocks went back
- **WHEN** the local time zone is the UK's, it is 1 November and the update source was last asked at 08:12 UTC on 24 October
- **THEN** the row reads 24 October 2026 09:12
