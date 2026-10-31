# Spec Delta

## MODIFIED Requirements

### Requirement: The key bar names the keys for the selected row
The last two rows of the screen SHALL be a key bar of items, each a key and a word: the first row the keys that act on the selected row, then New session and, when a release is known, Update, on the Sessions and Worktrees tabs; the second row the folder and view keys. The bar SHALL show only the keys that apply to the selected and listed rows, SHALL fit 120 columns for every kind of row, SHALL run a clicked item's key and show its tooltip on hover. The help SHALL list every key.

#### Scenario: A background session is selected
- **WHEN** a running background session is selected and no stopped session or empty terminal session is listed
- **THEN** the first row reads Enter Open, x Stop, d Delete, r Rename, m Memory and n New session, and the second row c Copy path, e Open folder, v Solution, p Open PR, / Filter, ? Help, ! Report and q Quit

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

## REMOVED Requirements

### Requirement: One console keeps its state across hand-offs
**Reason**: The console no longer hands its tab to an attach, so nothing leaves it except quit and there is no return to keep the selection and filter for.
**Migration**: None needed: switching tabs never left the console and still does not; quit clears Vantage's tab as before (session-attach spec).

## ADDED Requirements

### Requirement: Only quit leaves the console
The console SHALL run as one terminal application for every tab from start to quit, and no key SHALL leave it for another program in its own tab. Quit SHALL clear Vantage's tab as before.

#### Scenario: Opening a session stays in the console
- **WHEN** the user presses Enter on fix-123 on the Sessions tab with a filter of "fix" set
- **THEN** fix-123 opens in its own tab, and the console stays on screen with fix-123 selected and the filter "fix" still set

#### Scenario: Switching never leaves
- **WHEN** the user presses Right and Left ten times each
- **THEN** the tabs change at once and the console never leaves its screen
