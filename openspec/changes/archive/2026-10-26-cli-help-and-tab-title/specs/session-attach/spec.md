# Spec Delta

## MODIFIED Requirements

### Requirement: Vantage's own tab is recognisable
When the console starts in a Windows Terminal tab, Vantage SHALL title that tab "Vantage" and colour it green when no live Vantage-opened tab is green, otherwise with the first free palette colour; SHALL count its own tab's colour as in use when colouring session tabs; and SHALL clear the title and reset the colour when it quits. While a dialog is open the tab SHALL read "Vantage" followed by the dialog's short name in brackets, and closing the dialog SHALL put "Vantage" back; no dialog's own title SHALL reach the tab. Outside a dialog, every title Vantage writes for its own tab SHALL be the same text.

#### Scenario: Green is free
- **WHEN** Vantage starts and no live Vantage-opened tab is green
- **THEN** the tab is titled Vantage and coloured green, and the next session tab opened gets a colour other than green

#### Scenario: Green is in use
- **WHEN** Vantage starts while a live Vantage-opened tab is green
- **THEN** the tab is titled Vantage, and coloured with the first palette colour no live tab uses

#### Scenario: Title restored after a hand-off
- **WHEN** the user returns from an attached session or the agent view, which set the tab title themselves
- **THEN** the tab is titled Vantage, exactly as before, and coloured as before again

#### Scenario: Local build
- **WHEN** the installed tool's version is 0.12.0-local.20261026T101500
- **THEN** the tab is titled Vantage, and the version, with its prerelease label, is shown only in the header

#### Scenario: A dialog is open
- **WHEN** the user presses the help key
- **THEN** the tab reads Vantage (Keys) while the help is shown, and Vantage again once it is closed

#### Scenario: Each dialog has its own word
- **WHEN** the new-session wizard, rename, resume, settings, delete session, delete branch, close empty, force remove, open solution or update dialog is open
- **THEN** the tab reads Vantage (New session), Vantage (Rename), Vantage (Resume), Vantage (Settings), Vantage (Delete session), Vantage (Delete branch), Vantage (Close empty), Vantage (Force remove), Vantage (Open solution) or Vantage (Update) respectively

#### Scenario: Quit
- **WHEN** the user quits Vantage
- **THEN** the tab's title is cleared and its colour reset, and its registration goes stale with the process
