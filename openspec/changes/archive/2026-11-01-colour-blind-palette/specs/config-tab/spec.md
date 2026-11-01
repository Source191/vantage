# Spec Delta

## ADDED Requirements

### Requirement: The setup sits under its own heading
Between the settings and the setup rows the Config tab's table SHALL have a heading row reading Setup in SETTING and nothing in VALUE. The selection SHALL never rest on it: moving up or down steps over it, and a load never selects it. While a filter is set it SHALL be hidden. It SHALL count neither as a setting in the counts nor as a row in the table's title.

#### Scenario: The heading between the two parts
- **WHEN** the user presses 3 with the default settings
- **THEN** the row after Check for updates reads Setup, and the row after that Settings file, drawn grey

#### Scenario: Down steps over the heading
- **WHEN** Check for updates is selected and the user presses Down
- **THEN** Settings file is selected

#### Scenario: Up steps over the heading
- **WHEN** Settings file is selected and the user presses Up
- **THEN** Check for updates is selected

#### Scenario: A filter hides the heading
- **WHEN** the user filters by bell
- **THEN** the table shows only Claude bell, with no Setup heading

#### Scenario: The heading is not counted
- **WHEN** there is one root and every piece of setup is in place
- **THEN** the counts read 6 settings and the title counts 11 rows

## MODIFIED Requirements

### Requirement: The Config tab shows the setup
After the settings, under the Setup heading row, the table SHALL have a grey row for each piece of setup Vantage reads but does not edit: Settings file (the full path of `config.json`), Windows Terminal profiles (installed, with the fragment's path, or not set up), Claude bell (on, the other channel named, or not set), Diagnostic log (the `VANTAGE_LOG` path, or off) and Last update check (when, and the newest release found, or never, or off).

#### Scenario: Everything set up
- **WHEN** the fragment exists, Claude's settings say terminal_bell, `VANTAGE_LOG` is unset and nuget.org was asked at 09:12 today and named 0.18.0
- **THEN** the setup rows read the settings file's path, installed with the fragment's path, on, off and 09:12 today, newest 0.18.0

#### Scenario: Nothing set up
- **WHEN** the fragment does not exist and Claude's settings file has no preferredNotifChannel
- **THEN** Windows Terminal profiles reads not set up, run vantage setup-terminal, and Claude bell reads not set, run vantage setup-terminal

#### Scenario: Another channel
- **WHEN** Claude's settings say iterm2
- **THEN** Claude bell reads iterm2, not the terminal bell
