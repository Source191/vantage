# Spec Delta

## MODIFIED Requirements

### Requirement: A session or shell can be started in a worktree
From a worktree row whose folder exists and whose health is ok, locked or elsewhere, the user SHALL be able to start a new session in that worktree through the launch wizard with the directory preselected, with n, or open a plain PowerShell tab there, with o.

#### Scenario: Start session here
- **WHEN** the user presses n on a worktree row
- **THEN** the launch wizard opens with that worktree as the chosen directory and no new worktree option

#### Scenario: Shell in an elsewhere worktree
- **WHEN** the user presses the open-shell key on an elsewhere row
- **THEN** a PowerShell tab opens in that folder

### Requirement: Enter on a worktree row goes to its session by identity
Enter on a worktree row with a running session SHALL do what Enter does on that session's row on the Sessions tab, found by its session id so two sessions sharing a name cannot be confused: open the session in its own Windows Terminal tab, or switch to that tab when one is open, or say in the status line why it cannot. The Worktrees tab SHALL stay shown. With no session in the worktree, or one the sessions listing does not hold, the status line SHALL say so.

#### Scenario: Two sessions share a name
- **WHEN** two background sessions are both named fix-tests and one runs in the selected worktree
- **THEN** Enter opens or switches to the tab of the one whose working directory is that worktree

#### Scenario: The session's tab is open
- **WHEN** a Vantage-opened tab for the worktree's background session is alive and the user presses Enter on the worktree row
- **THEN** Windows Terminal switches to that tab, no new tab opens, and the Worktrees tab stays shown in Vantage

#### Scenario: The session has no tab
- **WHEN** the worktree's background session has no live tab and the user presses Enter on the worktree row
- **THEN** a new tab opens attached to that session, as Enter on the Sessions tab opens one

#### Scenario: A terminal session
- **WHEN** the session in the worktree is a plain claude running in its own terminal
- **THEN** nothing opens and the status line says, as on the Sessions tab, that it runs in its own terminal

#### Scenario: No session
- **WHEN** no session runs in the selected worktree and the user presses Enter
- **THEN** nothing opens and the status line says no session runs in that worktree

#### Scenario: Session gone from the listing
- **WHEN** the worktree's row names a session that the latest sessions listing no longer holds
- **THEN** nothing opens and the status line says the session is not listed any more

### Requirement: The worktrees header and help match the sessions view
The worktrees view SHALL be the Worktrees tab of the console, under the same settings line as the Sessions tab, with its own counts beside the tabs and its own key bar grouped Worktree, Folder and view; its status line SHALL show only messages, never a standing hint or the selected row's health, which the detail pane shows.

#### Scenario: Worktrees help
- **WHEN** the user presses the help key on the Worktrees tab
- **THEN** the help names every column of the worktrees table with its meaning, and its Pane section names every line of the pane, including what each health word means

#### Scenario: Worktrees help on a short terminal
- **WHEN** the user presses the help key in the worktrees view on a 120 x 36 terminal
- **THEN** every line of the help is reachable with the arrow keys and PageDown

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on P Prune in the key bar of the Worktrees tab
- **THEN** a tooltip says it prunes stale registrations in the row's repository

#### Scenario: An unhealthy row selected
- **WHEN** an elsewhere worktree is selected and no key has reported anything
- **THEN** the pane's Health line gives the reason and there is no status line

#### Scenario: Hover over a column header
- **WHEN** the mouse pointer rests on the LAST COMMIT header
- **THEN** a tooltip names the column and explains it

#### Scenario: The worktree bar
- **WHEN** an ok worktree with a session inside it is selected
- **THEN** the first bar row reads d Remove, D Force remove, P Prune, n New session, o Open shell and Enter Open session, and the second c Copy path, f Open folder, s Open solution, p Open PR, / Filter, ? Help, ! Report and q Quit
