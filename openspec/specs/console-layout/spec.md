# console-layout Specification

## Purpose
The frame the sessions and worktrees views share: two settings lines under the Vantage icon, two tabs with the active one marked, the counts and the table's title on their row, a key bar that names the keys for the selected row, and one console that keeps each tab's state across switches until it quits.

## Requirements

### Requirement: Sessions and worktrees are two tabs of one screen
The console SHALL show the sessions view, the worktrees view and the Config tab as three tabs of one screen, titled Sessions, Worktrees and Config. The 1, 2 and 3 keys SHALL select a tab, Left and Right SHALL select the tab before and after the one shown (stopping at the ends), Esc on the Worktrees or Config tab with no filter SHALL return to Sessions, and a click on a tab's title SHALL select it. Each tab SHALL keep its rows, selection, filter and status message while another is shown.

#### Scenario: Switch and come back
- **WHEN** the user selects templates-sync, types a filter of "temp", presses 2 and then 1
- **THEN** the Sessions tab shows at once with templates-sync still selected and the filter "temp" still set

#### Scenario: Tab titles stay put
- **WHEN** the sessions are listed and the worktrees are scanned, and their counts change
- **THEN** the tabs read Sessions, Worktrees and Config throughout, side by side without overlapping

#### Scenario: Worktrees not yet scanned
- **WHEN** the console starts and the worktrees tab has never been opened
- **THEN** one worktree scan starts once the first sessions listing has returned, and selecting the tab after it has finished lists the worktrees at once, with no waiting indicator

#### Scenario: The arrows stay with the table
- **WHEN** the Sessions tab is shown and the user presses Down twice
- **THEN** the selection moves down two rows and the Sessions tab stays shown

#### Scenario: Left and Right move between tabs
- **WHEN** the Sessions tab is shown and the user presses Right twice and then Right again
- **THEN** the Worktrees tab is shown, then the Config tab, and the third Right leaves the Config tab shown

#### Scenario: The w key
- **WHEN** the user presses w on any tab
- **THEN** no tab changes and nothing else happens

#### Scenario: Esc on the worktrees tab
- **WHEN** the worktrees tab is shown with no filter and the user presses Esc
- **THEN** the Sessions tab is shown

#### Scenario: Go to session
- **WHEN** the user presses Enter on a worktree row with a background session inside it
- **THEN** the session opens in its own tab, or its open tab is switched to, and the Worktrees tab stays shown with its selection unchanged

### Requirement: The active tab stands out
The active tab's title SHALL be drawn in bold cyan and the other tabs' titles in grey; the tabs' frames and every join between them SHALL stay grey, and the table's header SHALL be a box closed on all four sides, its lines in the frames' grey and its column names white, so the tab shown is plain at a glance.

#### Scenario: Sessions active
- **WHEN** the Sessions tab is shown
- **THEN** its title is bold cyan, the Worktrees and Config titles grey, and the frames and the table's header box stay grey

#### Scenario: Config active
- **WHEN** the user presses 3
- **THEN** the Config title turns bold cyan and the others grey, and no frame line or join changes colour

#### Scenario: Worktrees active
- **WHEN** the user presses 2
- **THEN** the Worktrees title turns bold cyan and the Sessions title grey, and no frame line or join changes colour

### Requirement: The Vantage icon heads the settings
Where the terminal can draw images, the console SHALL draw the Vantage icon one row high at the left of the settings row, which then starts after it; where it cannot, the console SHALL draw no icon and the settings row SHALL start at the left edge. The tabs SHALL start on the row below, or below the status line while it shows.

#### Scenario: A terminal that draws images
- **WHEN** Vantage runs in Windows Terminal, which draws Sixel images, and there is no status line
- **THEN** the icon sits on the first row at the left, the settings row starts to its right, and the tab strip starts on the second row

#### Scenario: A terminal that cannot
- **WHEN** the terminal does not report image support
- **THEN** no icon is drawn and the settings row starts at the left edge

### Requirement: The counts and the table's title sit beside the tabs
On the row of the tab titles the console SHALL show, after them, the active tab's counts separated by ·: per state present a glyph, count and word in the state's colour, needs input first; or per health present a count, then how many worktrees have changes and unpushed commits. At the row's right end it SHALL show the table's title, or a failure banner, cut at its end when too long, a filter being typed before it.

#### Scenario: Mixed session states
- **WHEN** six sessions are listed, two needing input, one working, one idle, one completed and one stopped
- **THEN** the counts read ◐ 2 need input · ● 1 working · ○ 1 idle · ✓ 1 completed · ■ 1 stopped, the first in amber

#### Scenario: Worktree counts
- **WHEN** seven worktrees are listed, five ok, one locked and one broken, two with changes and two with unpushed commits
- **THEN** the counts read 5 ok · 1 locked · 1 broken · 2 with changes · 2 with unpushed commits

#### Scenario: The title at the end of the row
- **WHEN** six sessions are listed and fix-123 is selected
- **THEN** 6 sessions, selected: fix-123 ends at the right edge of the tab titles' row, and the frame's line below is a plain border

#### Scenario: A filter being typed
- **WHEN** the user has typed /tem to filter the sessions
- **THEN** the row's right end reads filter: tem_ before the title, so the typed text stays in view when the title is cut

### Requirement: The key bar names the keys for the selected row
The last two rows of the screen SHALL be a key bar of items, each a key and a word: the first row the keys that act on the selected row, then New session and, when a release is known, Update, on the Sessions and Worktrees tabs; the second row the folder and view keys. The bar SHALL show only the keys that apply to the selected and listed rows, SHALL fit 120 columns for every kind of row, SHALL run a clicked item's key and show its tooltip on hover. The help SHALL list every key.

#### Scenario: A background session is selected
- **WHEN** a running background session is selected and no stopped session or empty terminal session is listed
- **THEN** the first row reads Enter Open, x Stop, d Delete, r Rename, m Memory and n New session, and the second row c Copy path, f Open folder, s Open solution, p Open PR, / Filter, ? Help, ! Report and q Quit

#### Scenario: A terminal session is selected
- **WHEN** a plain claude session that has never been sent a message is selected
- **THEN** the first row reads b Adopt, d Close tab and C Close empty before n New session

#### Scenario: A stopped session is listed
- **WHEN** some listed session is stopped or failed
- **THEN** R Resume is in the first row

#### Scenario: An update is known
- **WHEN** a newer release is known
- **THEN** U Update ends the first row, and otherwise the key is listed only in the help

#### Scenario: Click an item
- **WHEN** the user clicks Enter Open
- **THEN** the selected session is opened in its own tab, as pressing Enter does

#### Scenario: The bar fits
- **WHEN** the terminal is 120 columns wide and any kind of row is selected on any tab
- **THEN** both bar rows end within the terminal's width

#### Scenario: The help names the tab keys
- **WHEN** the user presses ? on any tab
- **THEN** the help lists 1, 2, 3, ← and → as choosing a tab and < and > as scrolling the columns, and lists neither w nor ,

#### Scenario: The help names Enter as Open
- **WHEN** the user presses ? on the Sessions tab
- **THEN** the help lists Enter as opening the session in its own tab or switching to it, and lists no o key and no attach in this tab

#### Scenario: The help names the folder keys
- **WHEN** the user presses ? on the Sessions or Worktrees tab
- **THEN** the help lists f as opening the folder and s as opening its solution, and lists neither e nor v

### Requirement: The settings take one line
The first row SHALL show the versions, labels orange, values white, parts separated by a grey ·: Vantage with its version and memory, then Claude and its version, with the update notice and the below-minimum warning in their places. The repository roots and the worktree root SHALL be shown on the Config tab, not here.

#### Scenario: The row
- **WHEN** Vantage 0.18.0 uses 35 MB with Claude Code 2.1.310
- **THEN** the first row reads Vantage 0.18.0 (35 MB) · Claude 2.1.310, the labels orange and the values white, and no row above the tabs names a root

#### Scenario: An update is available
- **WHEN** 0.19.0 is the newest release
- **THEN** the first row reads Vantage 0.18.0 (35 MB; 0.19.0 available, U to update) · Claude 2.1.310

### Requirement: Only quit leaves the console
The console SHALL run as one terminal application for every tab from start to quit, and no key SHALL leave it for another program in its own tab. Quit SHALL clear Vantage's tab as before.

#### Scenario: Opening a session stays in the console
- **WHEN** the user presses Enter on fix-123 on the Sessions tab with a filter of "fix" set
- **THEN** fix-123 opens in its own tab, and the console stays on screen with fix-123 selected and the filter "fix" still set

#### Scenario: Switching never leaves
- **WHEN** the user presses Right and Left ten times each
- **THEN** the tabs change at once and the console never leaves its screen

### Requirement: The status line sits under the settings row
The console SHALL show the shown tab's status line directly under the settings row and above the tabs, its mark in the column where the versions start, outside every tab, only while that tab has a message or an action it started is running; the tabs, their counts and the table's title SHALL move down to make room and back up when it goes. Nothing SHALL be drawn under the detail pane inside a tab's frame, and the tabs' frame SHALL end directly above the key bar.

#### Scenario: A message under the settings row
- **WHEN** the user presses c on fix-123 in the Sessions tab
- **THEN** the row under the versions says the folder was copied, the tab titles are on the row under it, and the detail pane's last line is the last row inside the frame, directly above its bottom edge and the key bar

#### Scenario: Nothing to say
- **WHEN** no key has reported anything on the Sessions tab and no action is running
- **THEN** the tab strip starts on the row under the versions and the tabs' frame ends on the row just above the key bar

#### Scenario: Nothing to say on Config
- **WHEN** the Config tab is shown with Worktree root selected and no key has reported anything
- **THEN** there is no status line and the tab strip starts on the row under the versions

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
The status line SHALL start with its message's tone: a green ✓ when something was done, an orange ! when the key was refused or did part of what was asked, a red ✗ when it failed, and a cyan spinner while an action runs, the words in the same colour. A message saying how a setup is changed SHALL be plain white with no glyph. A message reporting a result and then what followed it SHALL take the tone of what followed.

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
- **WHEN** the user presses Enter on Windows Terminal profiles on the Config tab
- **THEN** the status line says how to set them up in white with no glyph before it, and selecting a row alone shows no status line

### Requirement: A message can be closed
Every status message SHALL end with a ✕ that clears it when clicked and shows a tooltip saying so, except while an action runs. The ✕ SHALL follow the message's last word, on its last row.

#### Scenario: Closing a refusal
- **WHEN** the status line reads ! No session selected. and the user clicks the ✕ after it
- **THEN** the status line goes and the tabs move back up

#### Scenario: Nothing to close
- **WHEN** an action is running
- **THEN** there is no ✕

#### Scenario: Closing setup advice
- **WHEN** the Config tab's status line says how to set up the Windows Terminal profiles and the user clicks the ✕ after it
- **THEN** the status line goes and the tabs move back up

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

### Requirement: Grey text is legible
Text the console draws in grey (stopped and unknown sessions, empty worktrees, the Config tab's setup rows, dialog hints, the automated count, the tab frames and the separators between parts of a line) SHALL have a contrast ratio of at least 4.5:1 against the console's background.

#### Scenario: A stopped row
- **WHEN** a stopped session's row is drawn on the console's background
- **THEN** its text has a contrast ratio of at least 4.5:1 against that background

#### Scenario: A dialog hint
- **WHEN** the rename dialog shows its hint, Esc keeps the current name
- **THEN** the hint has a contrast ratio of at least 4.5:1 against the console's background

### Requirement: Dialog fields and highlights are legible
In every dialog, the text of an unfocused input field and of a list's chosen row once focus has moved on SHALL have a contrast ratio of at least 4.5:1 against its background, and that background SHALL have a contrast ratio of at least 3:1 against the console's own, so the field or row still stands out.

#### Scenario: An unfocused name field
- **WHEN** the rename dialog is open and focus has moved from the name field to Cancel
- **THEN** the typed name is drawn white on a background at least 4.5:1 from it and at least 3:1 from the console's background

#### Scenario: A list's chosen row after focus moves on
- **WHEN** a repository is chosen in the new-session wizard's list and focus moves to the next field
- **THEN** the chosen row stays marked, white on a background that meets the same two ratios
