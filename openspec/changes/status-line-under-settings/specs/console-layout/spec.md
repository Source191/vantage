## ADDED Requirements

### Requirement: The status line sits under the settings row
The console SHALL show the shown tab's status line directly under the settings row and above the tabs, its mark in the column where the versions start, outside every tab, only while that tab has a message or an action it started is running; the tabs, their counts and the table's title SHALL move down to make room and back up when it goes. Nothing SHALL be drawn under the detail pane inside a tab's frame, and the tabs' frame SHALL end directly above the key bar.

#### Scenario: A message under the settings row
- **WHEN** the user presses c on fix-123 in the Sessions tab
- **THEN** the row under the versions says the folder was copied, the tab titles are on the row under it, and the detail pane's last line is the last row inside the frame, directly above its bottom edge and the key bar

#### Scenario: Nothing to say
- **WHEN** no key has reported anything on the Sessions tab and no action is running
- **THEN** the tab strip starts on the row under the versions and the tabs' frame ends on the row just above the key bar

#### Scenario: The line follows the tab shown
- **WHEN** the Sessions tab says a folder was copied, the Worktrees tab has no message, and the user presses 2
- **THEN** there is no status line while the Worktrees tab is shown, and pressing 1 shows the copied message again

#### Scenario: An action running in the Worktrees tab
- **WHEN** the user removes a worktree
- **THEN** the row under the versions shows a spinner before "Removing name" until the removal reports its outcome

### Requirement: A long message wraps
A message wider than the screen SHALL wrap at spaces onto further rows, each starting under the first row's words, up to three rows; a word wider than a row SHALL be broken at the row's end and carried on the next, and a message that needs more than three rows SHALL end its third with an ellipsis.

#### Scenario: Two rows at 120 columns
- **WHEN** a message of 150 characters is shown on a terminal 120 columns wide
- **THEN** the message takes two rows under the versions, broken at a space, and the tabs start on the row after them

#### Scenario: Too long for three rows
- **WHEN** a message would need four rows at the terminal's width
- **THEN** it takes three rows and the third ends with …

### Requirement: Every message has a tone
The status line SHALL start with its message's tone: a green ✓ when something was done, an orange ! when the key was refused or did part of what was asked, a red ✗ when it failed, and a cyan spinner while an action runs, the words in the same colour. The Config tab's description of the selected row SHALL be plain white with no glyph. A message reporting a result and then what followed it SHALL take the tone of what followed.

#### Scenario: Something done
- **WHEN** a session started from the wizard opens in a purple tab
- **THEN** the status line reads ✓ Started and the rest of the message, in green

#### Scenario: A refusal
- **WHEN** the user presses x with no session selected
- **THEN** the status line reads ! No session selected., in orange

#### Scenario: A failure reported by the action
- **WHEN** a start from the wizard fails because the folder is not trusted
- **THEN** the status line starts with a red ✗ and gives the reason in red

#### Scenario: Background work threw
- **WHEN** an action started from a key throws
- **THEN** the status line starts with a red ✗ and gives what failed in red

#### Scenario: Part of what was asked
- **WHEN** a session is deleted but its worktree is kept because it has uncommitted changes
- **THEN** the status line starts with an orange !

#### Scenario: A result and what followed
- **WHEN** a worktree is removed, its branch is not merged, and the user answers Delete branch
- **THEN** the status line starts with a green ✓ and says the worktree was removed and the branch deleted

#### Scenario: Config's description
- **WHEN** the user selects Worktree root on the Config tab and no message is shown
- **THEN** the status line gives the row's description in white with no glyph before it

### Requirement: A message can be closed
Every status message SHALL end with a ✕ that clears it when clicked and shows a tooltip saying so, except while an action runs and for the Config tab's description of the selected row, which are not messages. The ✕ SHALL follow the message's last word, on its last row.

#### Scenario: Closing a refusal
- **WHEN** the status line reads ! No session selected. and the user clicks the ✕ after it
- **THEN** the status line goes and the tabs move back up

#### Scenario: Nothing to close
- **WHEN** an action is running, or the Config tab shows the selected row's description
- **THEN** there is no ✕

### Requirement: A success clears itself
A message with a ✓ SHALL clear by itself ten seconds after it appears, unless something else has replaced or cleared it first; refusals, failures and hints SHALL stay until they are closed, the selection moves or the filter changes.

#### Scenario: A copy confirmed
- **WHEN** the status line reads ✓ Copied the folder path and nothing is pressed for ten seconds
- **THEN** the status line goes and the tabs move back up

#### Scenario: A newer message survives
- **WHEN** a ✓ message appears, and five seconds later another key shows a ✓ message
- **THEN** the second message stays until ten seconds after it appeared

#### Scenario: A failure stays
- **WHEN** the status line reads ✗ and the reason a start failed, and nothing is pressed for a minute
- **THEN** the message is still shown

## MODIFIED Requirements

### Requirement: The Vantage icon heads the settings
Where the terminal can draw images, the console SHALL draw the Vantage icon one row high at the left of the settings row, which then starts after it; where it cannot, the console SHALL draw no icon and the settings row SHALL start at the left edge. The tabs SHALL start on the row below, or below the status line while it shows.

#### Scenario: A terminal that draws images
- **WHEN** Vantage runs in Windows Terminal, which draws Sixel images, and there is no status line
- **THEN** the icon sits on the first row at the left, the settings row starts to its right, and the tab strip starts on the second row

#### Scenario: A terminal that cannot
- **WHEN** the terminal does not report image support
- **THEN** no icon is drawn and the settings row starts at the left edge
