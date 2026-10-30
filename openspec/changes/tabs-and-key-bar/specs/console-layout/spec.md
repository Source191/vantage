# Spec Delta

## Purpose
The frame the sessions and worktrees views share: one settings line, two tabs with their counts, a key bar that names the keys for the selected row, and one console that keeps each tab's state across switches and hand-offs.

## ADDED Requirements

### Requirement: Sessions and worktrees are two tabs of one screen
The console SHALL show the sessions view and the worktrees view as two tabs of one screen, titled Sessions and Worktrees with the number of rows each lists in brackets once loaded. The 1 and 2 keys SHALL select a tab, the w key SHALL toggle between them, Esc on the worktrees tab with no filter SHALL return to Sessions, and a click on a tab's title SHALL select it. Each tab SHALL keep its rows, selection, filter and footer while the other is shown.

#### Scenario: Switch and come back
- **WHEN** the user selects templates-sync, types a filter of "temp", presses 2 and then 1
- **THEN** the Sessions tab shows at once with templates-sync still selected and the filter "temp" still set

#### Scenario: Tab titles carry the counts
- **WHEN** six sessions are listed and seven worktrees have been scanned
- **THEN** the tabs read Sessions (6) and Worktrees (7)

#### Scenario: Worktrees not yet scanned
- **WHEN** the console starts and the worktrees tab has never been opened
- **THEN** its title reads Worktrees with no count, and the scan starts when it is first selected

#### Scenario: Esc on the worktrees tab
- **WHEN** the worktrees tab is shown with no filter and the user presses Esc
- **THEN** the Sessions tab is shown

#### Scenario: Go to session
- **WHEN** the user presses Enter on a worktree row with a session inside it
- **THEN** the Sessions tab is shown with that session selected, without leaving the console

### Requirement: The settings line is one line
The first row of the screen SHALL read Vantage, its version and memory in brackets, then Claude and its version, then Repository roots and the roots, then Worktree root and the root, separated by · and drawn in grey. The update notice and the below-minimum Claude warning SHALL keep their places inside the Vantage and Claude parts. When the line is wider than the terminal the roots SHALL be elided first, with an ellipsis, and the whole list SHALL stay in Settings.

#### Scenario: Line at 120 columns
- **WHEN** Vantage 0.18.0 uses 35 MB with Claude Code 2.1.310, the root C:\Users\me\source\repos and the worktree root C:\Users\me\source\worktrees on a 120-column terminal
- **THEN** the line reads Vantage 0.18.0 (35 MB) · Claude 2.1.310 · Repository roots C:\Users\me\source\re… · Worktree root C:\Users\me\source\worktrees, the root cut to fit

#### Scenario: Line too wide
- **WHEN** the same values carry an update notice, 0.19.0 available, U to update, on a 120-column terminal
- **THEN** the notice is shown in full and the roots end in an ellipsis so the line fits

#### Scenario: Wide terminal
- **WHEN** the terminal is 160 columns wide
- **THEN** the line with the notice, Vantage 0.18.0 (35 MB; 0.19.0 available, U to update) · Claude 2.1.310 · Repository roots C:\Users\me\source\repos · Worktree root C:\Users\me\source\worktrees, is shown whole

### Requirement: The counts sit beside the tabs
On the tabs' row the console SHALL show, beside the tab titles, the sessions counts as a glyph, a count and a state word for each state present, each in that state's colour, separated by ·, and on the worktrees tab a count per health present followed by how many worktrees have changes and how many have unpushed commits. Needs input SHALL come first when present.

#### Scenario: Mixed session states
- **WHEN** six sessions are listed, two needing input, one working, one idle, one completed and one stopped
- **THEN** the row reads ◐ 2 need input · ● 1 working · ○ 1 idle · ✓ 1 completed · ■ 1 stopped, the first in amber

#### Scenario: Worktree counts
- **WHEN** seven worktrees are listed, five ok, one locked and one broken, two with changes and two with unpushed commits
- **THEN** the row reads 5 ok · 1 locked · 1 broken · 2 with changes · 2 with unpushed commits

### Requirement: The key bar names the keys for the selected row
The last two rows of the screen SHALL be a key bar of items, each a key and a word: the first row the keys that act on the selected row, then New session and, when a release is known, Update; the second row the folder and view keys. The bar SHALL show only the keys that apply to the selected and listed rows, SHALL fit 120 columns for every kind of row, SHALL run a clicked item's key and show its tooltip on hover. The help SHALL list every key.

#### Scenario: A background session is selected
- **WHEN** a running background session is selected and no stopped session or empty terminal session is listed
- **THEN** the first row reads Enter Attach, o Open tab, x Stop, d Delete, r Rename, m Memory and n New session, and the second row c Copy path, e Open folder, v Solution, p Open PR, w Worktrees, / Filter, , Settings, ? Help, ! Report and q Quit

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
- **WHEN** the user clicks o Open tab
- **THEN** the selected session is opened in its own tab, as pressing o does

#### Scenario: The bar fits
- **WHEN** the terminal is 120 columns wide and any kind of row is selected
- **THEN** both bar rows end within the terminal's width

### Requirement: One console keeps its state across hand-offs
The console SHALL run as one terminal application for both tabs. Only an attach SHALL leave it, and on return the console SHALL show the same tab with the same selection and filter. Quit SHALL clear Vantage's tab as before.

#### Scenario: Attach and return
- **WHEN** the user attaches to fix-123 from the Sessions tab with a filter of "fix" set and presses Ctrl+Z
- **THEN** the console returns to the Sessions tab with fix-123 selected and the filter "fix" still set

#### Scenario: Switching never leaves
- **WHEN** the user presses w ten times
- **THEN** the tabs alternate at once and no hand-off is recorded in the diagnostics
