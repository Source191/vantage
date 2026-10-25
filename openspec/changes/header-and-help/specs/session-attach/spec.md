# Spec Delta

## MODIFIED Requirements

### Requirement: Vantage's own tab is recognisable
When the console starts in a Windows Terminal tab, Vantage SHALL title that tab "Vantage" followed by its version in brackets and colour it green when no live Vantage-opened tab is green, otherwise with the first free palette colour; SHALL count its own tab's colour as in use when colouring session tabs; and SHALL clear the title and reset the colour when it quits. Every title Vantage writes for its own tab SHALL be the same text, and a local build SHALL show its prerelease label without the build timestamp.

#### Scenario: Green is free
- **WHEN** Vantage 0.5.0 starts and no live Vantage-opened tab is green
- **THEN** the tab is titled Vantage (0.5.0) and coloured green, and the next session tab opened gets a colour other than green

#### Scenario: Green is in use
- **WHEN** Vantage starts while a live Vantage-opened tab is green
- **THEN** the tab is titled Vantage and its version in brackets, and coloured with the first palette colour no live tab uses

#### Scenario: Title restored after a hand-off
- **WHEN** the user returns from an attached session or the agent view, which set the tab title themselves
- **THEN** the tab is titled Vantage and its version in brackets, exactly as before, and coloured as before again

#### Scenario: Local build
- **WHEN** the installed tool's version is 0.5.0-local.20261025T101500
- **THEN** the tab is titled Vantage (0.5.0-local)

#### Scenario: Quit
- **WHEN** the user quits Vantage
- **THEN** the tab's title is cleared and its colour reset, and its registration goes stale with the process
