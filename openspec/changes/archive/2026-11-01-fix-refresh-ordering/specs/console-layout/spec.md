## MODIFIED Requirements

### Requirement: Sessions and worktrees are two tabs of one screen
The console SHALL show the sessions view, the worktrees view and the Config tab as three tabs of one screen, titled Sessions, Worktrees and Config. Left and Right SHALL select the tab before and after the one shown (stopping at the ends), Esc on the Worktrees or Config tab with no filter SHALL return to Sessions, Esc on the Sessions tab with no filter SHALL do nothing, and a click on a tab's title SHALL select it. Each tab SHALL keep its rows, selection, filter and status message while another is shown.

#### Scenario: Switch and come back
- **WHEN** the user selects templates-sync, types a filter of "temp", presses Enter, then Right and then Left
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

#### Scenario: The digit keys
- **WHEN** the Sessions tab is shown and the user presses 1, 2 or 3
- **THEN** no tab changes and nothing else happens

#### Scenario: Esc on the worktrees tab
- **WHEN** the worktrees tab is shown with no filter and the user presses Esc
- **THEN** the Sessions tab is shown

#### Scenario: Esc on the Sessions tab with no filter
- **WHEN** the Sessions tab is shown with no filter, a status message showing, and the user presses Esc
- **THEN** the rows, selection and message are unchanged and the Sessions tab stays shown

#### Scenario: Go to session
- **WHEN** the user presses Enter on a worktree row with a background session inside it
- **THEN** the session opens in its own tab, or its open tab is switched to, and the Worktrees tab stays shown with its selection unchanged

### Requirement: The status line sits under the settings row
The console SHALL show the shown tab's status line directly under the settings row and above the tabs, its mark in the column where the versions start, outside every tab, only while that tab has a message or any action it started is still running; the spinner SHALL turn until every running action has reported. The tabs, their counts and the table's title SHALL move down to make room and back up when it goes. Nothing SHALL be drawn under the detail pane inside a tab's frame, and the tabs' frame SHALL end directly above the key bar.

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

#### Scenario: Two actions overlap
- **WHEN** the user presses d on one worktree and, before it reports, d on another, and the first reports
- **THEN** the spinner keeps turning before the second's working text until the second reports, and only then is the mark drawn
