# Spec Delta

## MODIFIED Requirements

### Requirement: Drill into a session in the same tab
Pressing Enter on a background session SHALL suspend the console screen and run the Claude Code attach command for that session in the same terminal, and SHALL redraw the console when the attach command exits. Before handing over, the console SHALL print one line saying that Ctrl+Z returns to Vantage, and SHALL erase that line when the attach command exits, so the scrollback is left as it was.

#### Scenario: Attach and return
- **WHEN** the user presses Enter on a background session and later presses Ctrl+Z inside it
- **THEN** the attach command exits, the console redraws with a fresh listing, and the session keeps running

#### Scenario: Live tab already attached
- **WHEN** a Vantage-opened tab for the session is still alive
- **THEN** Enter does not attach and the footer says the session is open in another tab

#### Scenario: Footer teaches the return key
- **WHEN** the console starts an attach
- **THEN** the last line printed before handing over is exactly "Ctrl+Z returns to Vantage.", on one line

#### Scenario: The hint leaves no trace
- **WHEN** the user attaches three times, returning with Ctrl+Z each time, and then quits Vantage
- **THEN** the terminal's scrollback holds no copy of the hint

#### Scenario: Empty session
- **WHEN** the user presses Enter on a background session that has never been sent a message
- **THEN** Claude Code shows that session's own prompt, not an agent list, and Ctrl+Z returns to Vantage as for any session

### Requirement: Vantage's own tab is recognisable
When the console starts in a Windows Terminal tab, Vantage SHALL title that tab "Vantage" and colour it green when no live Vantage-opened tab is green, otherwise with the first free palette colour; SHALL count its own tab's colour as in use when colouring session tabs; and SHALL clear the title and reset the colour when it quits, so the tab shows its profile's own colour again. While a dialog is open the tab SHALL read "Vantage" followed by the dialog's short name in brackets, and closing the dialog SHALL put "Vantage" back; no dialog's own title SHALL reach the tab. Outside a dialog, every title Vantage writes for its own tab SHALL be the same text.

#### Scenario: Green is free
- **WHEN** Vantage starts and no live Vantage-opened tab is green
- **THEN** the tab is titled Vantage and coloured green, and the next session tab opened gets a colour other than green

#### Scenario: Green is in use
- **WHEN** Vantage starts while a live Vantage-opened tab is green
- **THEN** the tab is titled Vantage, and coloured with the first palette colour no live tab uses

#### Scenario: Title restored after a hand-off
- **WHEN** the user returns from an attached session, which set the tab title itself
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
- **THEN** the tab's title is cleared, the tab shows its profile's colour again because the palette entry Vantage coloured is reset rather than only re-pointed, and its registration goes stale with the process

## REMOVED Requirements

### Requirement: The native agent view is one key away
**Reason**: Claude Code 2.1.310 attaches a session that has never been sent a message at its own prompt, so the agent list the key was there for no longer appears from Vantage, and the owner found no other use for the key.
**Migration**: Run `claude agents` in any shell for Claude Code's own view; nothing in Vantage depends on it.
