## RENAMED Requirements

- FROM: `### Requirement: The Vantage icon heads the settings`
- TO: `### Requirement: The Torni icon heads the settings`

## MODIFIED Requirements

### Requirement: The Torni icon heads the settings
Where the terminal can draw images, the console SHALL draw the Torni icon one row high at the left of the settings row, which then starts after it; where it cannot, the console SHALL draw no icon and the settings row SHALL start at the left edge. The tabs SHALL start on the row below, or below the status line while it shows.

#### Scenario: A terminal that draws images
- **WHEN** Torni runs in Windows Terminal, which draws Sixel images, and there is no status line
- **THEN** the icon sits on the first row at the left, the settings row starts to its right, and the tab strip starts on the second row

#### Scenario: A terminal that cannot
- **WHEN** the terminal does not report image support
- **THEN** no icon is drawn and the settings row starts at the left edge

### Requirement: The settings take one line
The first row SHALL show the versions, labels orange, values white, parts separated by a grey ·: Torni with its version and memory, then Claude and its version, with the update notice, in amber, and the below-minimum warning in their places. The repository roots and the worktree root SHALL be shown on the Config tab, not here.

#### Scenario: The row
- **WHEN** Torni 0.18.0 uses 35 MB with Claude Code 2.1.310
- **THEN** the first row reads Torni 0.18.0 (35 MB) · Claude 2.1.310, the labels orange and the values white, and no row above the tabs names a root

#### Scenario: An update is available
- **WHEN** 0.19.0 is the newest release
- **THEN** the first row reads Torni 0.18.0 (35 MB; 0.19.0 available, U to update) · Claude 2.1.310, with 0.19.0 available, U to update in amber and the rest as before

### Requirement: Only quit leaves the console
The console SHALL run as one terminal application for every tab from start to quit, and no key SHALL leave it for another program in its own tab. Quit SHALL clear Torni's tab as before.

#### Scenario: Opening a session stays in the console
- **WHEN** the user presses Enter on fix-123 on the Sessions tab with a filter of "fix" set
- **THEN** fix-123 opens in its own tab, and the console stays on screen with fix-123 selected and the filter "fix" still set

#### Scenario: Switching never leaves
- **WHEN** the user presses Right and Left ten times each
- **THEN** the tabs change at once and the console never leaves its screen
