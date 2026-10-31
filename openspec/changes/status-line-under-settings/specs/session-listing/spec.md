## MODIFIED Requirements

### Requirement: The screen is a header, a table and a footer
The sessions view SHALL be the Sessions tab of the console: under the settings rows and the tabs' row, a table framed by the tab, whose title, at the right end of the tabs' row, counts the rows shown, names the filter when one is set and names the selected session; a highlighted bar on the selected row; and the detail pane beneath the table.

#### Scenario: Table title
- **WHEN** seven sessions are listed with no filter and fix-123 is selected
- **THEN** the table title reads 7 sessions, selected: fix-123

#### Scenario: Table title with a filter
- **WHEN** a filter of "templates" matches two of seven rows and templates-sync is selected
- **THEN** the table title reads 2 of 7 sessions match "templates", selected: templates-sync

#### Scenario: One session
- **WHEN** one session is listed with no filter and it is selected
- **THEN** the table title reads 1 session, selected: and the session's name

#### Scenario: Nothing selected
- **WHEN** no sessions are listed
- **THEN** the table title reads 0 sessions and names no selection

#### Scenario: Nothing to say
- **WHEN** two sessions need input and no key has reported anything
- **THEN** the counts beside the tabs read ◐ 2 need input, there is no status line, and the table has its row

#### Scenario: A message
- **WHEN** the user presses c on fix-123
- **THEN** a status line under the settings row, above the tabs, says the folder was copied, and it goes when the user presses Down

### Requirement: The status line speaks only when there is something to say
Under the settings row, above the tabs, the sessions view SHALL show a status line only while there is a message or an action is running, the tabs moving back up otherwise. A message SHALL clear when the selection moves or the filter changes. The status line SHALL NOT name the view or repeat the counts beside the tabs.

#### Scenario: A message cleared by the filter
- **WHEN** the status line says a folder was copied and the user starts a filter with / and types t
- **THEN** the status line goes and the tabs move back up a row
