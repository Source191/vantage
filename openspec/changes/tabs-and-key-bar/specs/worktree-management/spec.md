# Spec Delta

## MODIFIED Requirements

### Requirement: The worktrees header and help match the sessions view
The worktrees view SHALL be the Worktrees tab of the console, under the same settings line as the Sessions tab, with its own counts beside the tabs and its own key bar grouped Worktree, Folder and view; its help SHALL explain each of its columns; each item of its key bar SHALL show the key's tooltip while the mouse pointer rests on it, and each of its column headers SHALL show what the column shows.

#### Scenario: Worktrees help
- **WHEN** the user presses the help key on the Worktrees tab
- **THEN** the help names every column of the worktrees table with its meaning, including what each health word means

#### Scenario: Hover over a key hint
- **WHEN** the mouse pointer rests on P Prune in the key bar of the Worktrees tab
- **THEN** a tooltip says it prunes stale registrations in the row's repository

#### Scenario: Hover over a column header
- **WHEN** the mouse pointer rests on the HEALTH header
- **THEN** a tooltip names the column and explains what each health word means

#### Scenario: The worktree bar
- **WHEN** an ok worktree with a session inside it is selected
- **THEN** the first bar row reads d Remove, D Force remove, P Prune, s Start session, o Open shell and Enter Go to session, and the second the folder keys, w Sessions, / Filter, , Settings, ? Help, ! Report and q Quit
