# Spec Delta

## MODIFIED Requirements

### Requirement: Open a session in its own Windows Terminal tab
Enter on a background session is the open-in-tab key. It SHALL open a new tab in the current Windows Terminal window that runs the attach command for the session, titled with the session name, with application title changes suppressed, starting in the session's working directory, coloured from the palette, and in the "Vantage session" profile when Vantage's Windows Terminal fragment is installed, otherwise in the default profile. When a Vantage-opened tab for the session is alive at the moment the key is pressed, the key SHALL instead switch to that tab; a record whose shell has gone SHALL be dropped and a new tab opened. Opening a tab SHALL drop any earlier record for the same session. The console SHALL never attach to a session in its own tab.

#### Scenario: Tab opened
- **WHEN** the user presses Enter on a background session
- **THEN** a new tab appears with the session's name as its title and the attach command running inside it, and the console stays on screen

#### Scenario: Tab opened with the setup profile
- **WHEN** `vantage setup-terminal` has installed the fragment and the user presses Enter on a background session
- **THEN** the new tab uses the "Vantage session" profile, so a bell in it plays that profile's sound

#### Scenario: Tab opened without the setup profile
- **WHEN** the fragment is not installed and the user presses Enter on a background session
- **THEN** the new tab uses Windows Terminal's default profile, as before

#### Scenario: Tab already live
- **WHEN** a Vantage-opened tab for that session is still alive and the user presses Enter on it
- **THEN** no second tab is opened, the Windows Terminal tab titled with the session name is selected in the window that owns it, and the footer says Vantage switched to it

#### Scenario: Live tab cannot be found
- **WHEN** a Vantage-opened tab for that session is recorded as alive but no tab item with the session's name can be found in its Windows Terminal window
- **THEN** no tab is opened and the footer says the tab is open but could not be found

#### Scenario: Tab closed by hand
- **WHEN** the user closes the session's tab with the terminal's close button and, once the tab's shell has gone, presses Enter on the session before the next refresh
- **THEN** the stale record is dropped, a new tab opens, and the footer says a tab was opened, not switched to

#### Scenario: Earlier record dropped
- **WHEN** a record for the session remains from a tab that has gone and the user presses Enter on the session
- **THEN** the new tab's record is the only one for the session and the old record's colour is free again

#### Scenario: Empty session
- **WHEN** the user presses Enter on a running background session that has never been sent a message
- **THEN** its tab shows that session's own prompt, not an agent list, and Ctrl+Z in it closes the tab as for any session

#### Scenario: o is not a key
- **WHEN** the user presses o on a background session
- **THEN** nothing happens and no tab is opened

### Requirement: Live tabs are tracked by process
Vantage SHALL record each tab it opens with the tab shell's process ID and start time, SHALL treat a record as live only while that process exists with the same start time, and SHALL remove stale records whenever it reads them. A tab opened for an attach SHALL close itself when the attach ends because the user detached or the session was stopped or removed, and SHALL stay open, showing the output, when the attach fails.

#### Scenario: Tab closed
- **WHEN** the user closes a Vantage-opened tab
- **THEN** on the next read its record is removed and its colour becomes free

#### Scenario: Process ID reused
- **WHEN** a record's process ID now belongs to a process with a different start time
- **THEN** the record is treated as stale and removed

#### Scenario: Detach closes the tab
- **WHEN** the user presses Ctrl+Z inside a tab Vantage opened for a session
- **THEN** the tab closes, the TAB column clears within one refresh, and Enter on the row opens a new tab

#### Scenario: Attach fails
- **WHEN** the attach command in a Vantage-opened tab exits with a failure, for example because the session was removed before it started
- **THEN** the tab stays open with the attach command's output

### Requirement: Sessions with nothing to resume are refused
Open-in-tab, rename and resume SHALL be refused for a session that has no process and no transcript, because Claude writes a session's transcript only on its first message, and the footer SHALL say that delete removes it.

#### Scenario: Stub session
- **WHEN** the user presses Enter on a failed session whose transcript does not exist
- **THEN** nothing happens, no tab opens, and the footer says it was never sent a message and that delete removes it

### Requirement: Vantage's own tab is recognisable
When the console starts in a Windows Terminal tab, Vantage SHALL title that tab "Vantage" and colour it green when no live Vantage-opened tab is green, otherwise with the first free palette colour; SHALL count its own tab's colour as in use when colouring session tabs; and SHALL clear the title and reset the colour when it quits, so the tab shows its profile's own colour again. While a dialog is open the tab SHALL read "Vantage" followed by the dialog's short name in brackets, and closing the dialog SHALL put "Vantage" back; no dialog's own title SHALL reach the tab. Outside a dialog, every title Vantage writes for its own tab SHALL be the same text.

#### Scenario: Green is free
- **WHEN** Vantage starts and no live Vantage-opened tab is green
- **THEN** the tab is titled Vantage and coloured green, and the next session tab opened gets a colour other than green

#### Scenario: Green is in use
- **WHEN** Vantage starts while a live Vantage-opened tab is green
- **THEN** the tab is titled Vantage, and coloured with the first palette colour no live tab uses

#### Scenario: Local build
- **WHEN** the installed tool's version is 0.12.0-local.20261026T101500
- **THEN** the tab is titled Vantage, and the version, with its prerelease label, is shown only in the header

#### Scenario: A dialog is open
- **WHEN** the user presses the help key
- **THEN** the tab reads Vantage (Keys) while the help is shown, and Vantage again once it is closed

#### Scenario: Each dialog has its own word
- **WHEN** the new-session wizard, rename, resume, settings, delete session, delete branch, close empty, force remove, open solution or update dialog is open
- **THEN** the tab reads Vantage (New session), Vantage (Rename), Vantage (Resume), Vantage (Settings), Vantage (Delete session), Vantage (Delete branch), Vantage (Close empty), Vantage (Force remove), Vantage (Open solution) or Vantage (Update) respectively

#### Scenario: Title restored after a hand-off
- **WHEN** the user opens a session's tab with Enter, works in it while Claude sets that tab's title, and selects the Vantage tab again
- **THEN** the Vantage tab is still titled Vantage and coloured as before, because the attach ran in the other tab

#### Scenario: Quit
- **WHEN** the user quits Vantage
- **THEN** the tab's title is cleared, the tab shows its profile's colour again because the palette entry Vantage coloured is reset rather than only re-pointed, and its registration goes stale with the process

## REMOVED Requirements

### Requirement: Drill into a session in the same tab
**Reason**: Enter now opens the session in its own tab (the owner's decision, 31 October 2026), so the console never hands its own tab to `claude attach`; the "open in another tab" refusal and the "Ctrl+Z returns to Vantage." line go with it.
**Migration**: Press Enter to open the session in its own tab, or switch to it; Ctrl+Z there closes that tab. `vantage here` still attaches in the shell's own tab.
