## MODIFIED Requirements

### Requirement: Sessions come from the supported CLI listing
Vantage SHALL obtain the session list by running the Claude Code CLI's JSON listing of active and completed sessions, and SHALL NOT read transcript files or the sessions folder of the Claude configuration directory. A listing that has not returned within the child program time limit SHALL be ended and reported as a failed listing.

#### Scenario: Listing succeeds
- **WHEN** the CLI listing returns three sessions
- **THEN** the sessions view shows three rows, each with the session's name, state and working directory

#### Scenario: Listing fails
- **WHEN** the CLI cannot be run or returns invalid JSON
- **THEN** the view keeps the rows it last showed, displays an error banner naming the failure, and does not exit

#### Scenario: Listing hangs
- **WHEN** the CLI listing has not exited after the time limit
- **THEN** the listing's process tree is ended, the view keeps the rows it last showed, the banner says the listing failed and names claude and the limit, and the next timed refresh runs as usual

### Requirement: The list refreshes without losing the selection
Vantage SHALL refresh the list automatically at least every five seconds and on demand, SHALL keep the selected session selected across a refresh when it still exists, and SHALL stay consistent when one refresh is still running as the next begins: a refresh SHALL never show rows older than the ones already shown, and rows that arrive while a dialog is open SHALL be kept when it closes. The timed refresh SHALL NOT start a listing while one is running; the refresh after an action SHALL always run.

#### Scenario: Selected session survives refresh
- **WHEN** the user has selected a session and a refresh occurs
- **THEN** the same session remains selected even if its row position changed

#### Scenario: Selected session disappears
- **WHEN** the selected session is no longer in the listing after a refresh
- **THEN** the selection moves to the nearest remaining row

#### Scenario: Overlapping refreshes
- **WHEN** a refresh takes longer than the refresh interval and the refresh after an action starts before it finishes
- **THEN** the view shows the complete listing from the refresh that started last, whichever finishes first, and neither refresh fails

#### Scenario: A listing started before a stop lands after it
- **WHEN** the timed listing is running, the user presses x on fix-123, the stop completes and its own listing returns without fix-123, and then the earlier listing returns with fix-123 still running
- **THEN** fix-123 stays gone, no bell rings and no last prompted time is recorded for the reordering

#### Scenario: Rows loaded while a dialog is open
- **WHEN** the user presses m on a session, a timed refresh returns new rows while the memory dialog is open, and the user closes it
- **THEN** the table shows the new rows, not the ones from before the dialog opened

#### Scenario: The timer waits for a running listing
- **WHEN** a listing takes eight seconds
- **THEN** the five-second timer starts no second `claude agents` process until it returns, and the refresh after a stop or delete still starts its own listing

### Requirement: Waiting is visible
The sessions view SHALL show a spinner with the words "Listing sessions" in the middle of the table until the first listing arrives, and SHALL show a spinner beside the status line's working text while an action started from the view runs; neither spinner SHALL appear for the timed refreshes. Typing a filter, Backspace or Escape before the first listing SHALL NOT end the waiting spinner.

#### Scenario: First listing pending
- **WHEN** Vantage starts and the CLI listing has not yet returned
- **THEN** the table area shows a spinner and "Listing sessions", which disappear when the first rows arrive

#### Scenario: A filter typed before the first listing
- **WHEN** the CLI listing has not yet returned and the user presses / and types t
- **THEN** the table area still shows the spinner and "Listing sessions", and the title shows the filter being typed

#### Scenario: Escape before the first listing
- **WHEN** the CLI listing has not yet returned and the user presses Esc
- **THEN** the table area still shows the spinner and "Listing sessions"

#### Scenario: Action running
- **WHEN** the user starts a session from the wizard
- **THEN** the status line shows a spinner before "Starting name" until the start reports its outcome

#### Scenario: First listing fails
- **WHEN** the first CLI listing fails
- **THEN** the spinner is replaced by the failure banner

### Requirement: The status line speaks only when there is something to say
Under the settings row, above the tabs, the sessions view SHALL show a status line only while there is a message or an action is running, the tabs moving back up otherwise. A message SHALL clear when the selection moves or the filter changes, Escape clearing a filter included. The status line SHALL NOT name the view or repeat the counts beside the tabs.

#### Scenario: A message cleared by the filter
- **WHEN** the status line says a folder was copied and the user starts a filter with / and types t
- **THEN** the status line goes and the tabs move back up a row

#### Scenario: A message cleared by Escape
- **WHEN** a filter of "temp" is set, the status line says a folder was copied and the user presses Esc
- **THEN** the filter is cleared, the status line goes and the tabs move back up a row
