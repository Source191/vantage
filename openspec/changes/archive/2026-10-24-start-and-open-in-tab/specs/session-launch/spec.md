# Spec Delta

## ADDED Requirements

### Requirement: The session starts under the supervisor and opens where the user chose
Vantage SHALL start the session as a named background session in the chosen directory, passing the opening prompt when one was given and starting an idle session when none was, and SHALL then open the session in its own Windows Terminal tab or return to the list as the user chose.

#### Scenario: No opening prompt
- **WHEN** the user leaves the opening prompt blank
- **THEN** the session starts idle and waits for input under the given name

#### Scenario: Start and open in a tab
- **WHEN** the user chooses "start and open in tab"
- **THEN** Vantage starts the session, opens it in a new Windows Terminal tab titled with the session name as the open-in-tab key does, and the sessions view stays on screen with the footer naming the tab's colour

#### Scenario: Started session not listed yet
- **WHEN** the user chooses "start and open in tab" and the supervisor has not yet listed the new session when the start returns
- **THEN** the footer reports the session as started and says to open it with the open-in-tab key once it appears, and no tab is opened

## REMOVED Requirements

### Requirement: The session starts under the supervisor with its name
**Reason**: Its "start and open here" scenario (attach in the same tab straight after starting) is replaced by opening the new session in its own tab, which keeps the overview on screen.
**Migration**: Use "start and open in tab" (F6) to start and work in the session at once, or F5 then Enter on the new row to attach in the same tab.
