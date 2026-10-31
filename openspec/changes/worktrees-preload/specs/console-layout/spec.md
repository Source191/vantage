## MODIFIED Requirements

### Requirement: Sessions and worktrees are two tabs of one screen
The console SHALL show the sessions view, the worktrees view and the Config tab as three tabs of one screen, titled Sessions, Worktrees and Config. The 1, 2 and 3 keys SHALL select a tab, Left and Right SHALL select the tab before and after the one shown (stopping at the ends), Esc on the Worktrees or Config tab with no filter SHALL return to Sessions, and a click on a tab's title SHALL select it. Each tab SHALL keep its rows, selection, filter and footer while another is shown.

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
- **WHEN** the user presses Enter on a worktree row with a session inside it
- **THEN** the Sessions tab is shown with that session selected, without leaving the console
