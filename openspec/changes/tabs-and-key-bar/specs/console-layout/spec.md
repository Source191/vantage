# Spec Delta

## Purpose
The frame the sessions and worktrees views share: two settings lines under the Vantage icon, two tabs with the active one marked, the counts and the table's title on their row, a key bar that names the keys for the selected row, and one console that keeps each tab's state across switches and hand-offs.

## ADDED Requirements

### Requirement: Sessions and worktrees are two tabs of one screen
The console SHALL show the sessions view and the worktrees view as two tabs of one screen, titled Sessions and Worktrees. The 1 and 2 keys SHALL select a tab, the w key SHALL toggle between them, Esc on the worktrees tab with no filter SHALL return to Sessions, and a click on a tab's title SHALL select it. Each tab SHALL keep its rows, selection, filter and footer while the other is shown.

#### Scenario: Switch and come back
- **WHEN** the user selects templates-sync, types a filter of "temp", presses 2 and then 1
- **THEN** the Sessions tab shows at once with templates-sync still selected and the filter "temp" still set

#### Scenario: Tab titles stay put
- **WHEN** the sessions are listed and the worktrees are scanned, and their counts change
- **THEN** the tabs read Sessions and Worktrees throughout, side by side without overlapping

#### Scenario: Worktrees not yet scanned
- **WHEN** the console starts and the worktrees tab has never been opened
- **THEN** no worktree scan has run, and the scan starts, with its waiting indicator, when the tab is first selected

#### Scenario: The arrows stay with the table
- **WHEN** the Sessions tab is shown and the user presses Down twice
- **THEN** the selection moves down two rows and the Sessions tab stays shown

#### Scenario: Esc on the worktrees tab
- **WHEN** the worktrees tab is shown with no filter and the user presses Esc
- **THEN** the Sessions tab is shown

#### Scenario: Go to session
- **WHEN** the user presses Enter on a worktree row with a session inside it
- **THEN** the Sessions tab is shown with that session selected, without leaving the console

### Requirement: The active tab stands out
The active tab's title and frame, every join of that frame and the table's header box, closed on all four sides with the column names white inside it, SHALL be drawn in cyan, and the other tab's title in grey, so the tab shown is plain at a glance.

#### Scenario: Sessions active
- **WHEN** the Sessions tab is shown
- **THEN** its title, the frame around the table and the lines of the table's header box are cyan and the Worktrees title is grey

#### Scenario: Worktrees active
- **WHEN** the user presses 2
- **THEN** the Worktrees title and frame turn cyan, including the frame's left corner where the Sessions tab meets it, and the Sessions title grey

### Requirement: The settings take two lines
The first two rows SHALL show the settings, labels orange, values white, parts separated by a grey ·: first Vantage with its version and memory, then Claude and its version, with the update notice and the below-minimum warning in their places; second Repository roots with every root, then Worktree root. When the second row is too wide the roots SHALL be elided first, then the worktree root, each with an ellipsis.

#### Scenario: Both rows fit
- **WHEN** Vantage 0.18.0 uses 35 MB with Claude Code 2.1.310, the root C:\Users\me\source\repos and the worktree root C:\Users\me\source\worktrees on a 120-column terminal
- **THEN** the first row reads Vantage 0.18.0 (35 MB) · Claude 2.1.310 and the second Repository roots C:\Users\me\source\repos · Worktree root C:\Users\me\source\worktrees, the labels orange and the values white

#### Scenario: Several roots
- **WHEN** the roots are C:\Users\me\source\repos and D:\work
- **THEN** the second row reads Repository roots C:\Users\me\source\repos; D:\work · Worktree root C:\Users\me\source\worktrees

#### Scenario: Too many roots for the row
- **WHEN** the roots are C:\Users\me\source\repos, D:\work\northwind and E:\archive\northwind-old and the row has 80 cells
- **THEN** the second row reads Repository roots C:\Users\me\sourc… · Worktree root C:\Users\me\source\worktrees

#### Scenario: Room for neither
- **WHEN** the second row has 50 cells
- **THEN** it reads Repository roots … · Worktree root C:\Users\me\so…

#### Scenario: An update is available
- **WHEN** 0.19.0 is the newest release
- **THEN** the first row reads Vantage 0.18.0 (35 MB; 0.19.0 available, U to update) · Claude 2.1.310

### Requirement: The Vantage icon heads the settings
Where the terminal can draw images, the console SHALL draw the Vantage icon two rows high at the left of the two settings rows, which then start after it; where it cannot, the console SHALL draw no icon and the settings rows SHALL start at the left edge.

#### Scenario: A terminal that draws images
- **WHEN** Vantage runs in Windows Terminal, which draws Sixel images
- **THEN** the icon spans the first two rows at the left and both settings rows start to its right

#### Scenario: A terminal that cannot
- **WHEN** the terminal does not report image support
- **THEN** no icon is drawn and both settings rows start at the left edge

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
