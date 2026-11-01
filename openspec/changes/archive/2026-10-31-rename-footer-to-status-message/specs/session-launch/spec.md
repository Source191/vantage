## MODIFIED Requirements

### Requirement: A session started from the wizard opens in its own tab
A session started from the wizard SHALL be opened in its own Windows Terminal tab as soon as the supervisor lists it, and the wizard SHALL have no way to start one without a tab; `vantage here` attaches in its own tab instead.

#### Scenario: Start and open in a tab
- **WHEN** the user starts a session from the wizard
- **THEN** Vantage starts the session, opens it in a new Windows Terminal tab titled with the session name as the open-in-tab key does, and the sessions view stays on screen with the status line naming the tab's colour

#### Scenario: No start without a tab
- **WHEN** the user presses F6 in the wizard
- **THEN** nothing starts and the wizard stays open

#### Scenario: Started session not listed yet
- **WHEN** the user starts a session from the wizard and the supervisor has not yet listed the new session when the start returns
- **THEN** the status line reports the session as started and says to press Enter on it once it appears, and no tab is opened
