# Spec Delta

## MODIFIED Requirements

### Requirement: Open a session in its own Windows Terminal tab
The open-in-tab key SHALL open a new tab in the current Windows Terminal window that runs the attach command for the session, titled with the session name, with application title changes suppressed, starting in the session's working directory, coloured from the palette, and in the "Vantage session" profile when Vantage's Windows Terminal fragment is installed, otherwise in the default profile. When a Vantage-opened tab for the session is still alive, the key SHALL instead switch to that tab.

#### Scenario: Tab opened
- **WHEN** the user presses the open-in-tab key on a background session
- **THEN** a new tab appears with the session's name as its title and the attach command running inside it

#### Scenario: Tab opened with the setup profile
- **WHEN** `vantage setup-terminal` has installed the fragment and the user presses the open-in-tab key on a background session
- **THEN** the new tab uses the "Vantage session" profile, so a bell in it plays that profile's sound

#### Scenario: Tab opened without the setup profile
- **WHEN** the fragment is not installed and the user presses the open-in-tab key on a background session
- **THEN** the new tab uses Windows Terminal's default profile, as before

#### Scenario: Tab already live
- **WHEN** a Vantage-opened tab for that session is still alive
- **THEN** no second tab is opened, the Windows Terminal tab titled with the session name is selected in the window that owns it, and the footer says Vantage switched to it

#### Scenario: Live tab cannot be found
- **WHEN** a Vantage-opened tab for that session is recorded as alive but no tab item with the session's name can be found in its Windows Terminal window
- **THEN** no tab is opened and the footer says the tab is open but could not be found
