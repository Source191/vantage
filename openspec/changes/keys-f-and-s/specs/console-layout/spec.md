# Spec Delta

## MODIFIED Requirements

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
