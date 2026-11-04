## MODIFIED Requirements

### Requirement: A key opens the problem report
Both table views SHALL offer `!` (Report a problem), listed in the key hints, the help and the tooltips like every other key. It SHALL open a dialog with a title box and a read-only preview of the diagnostics text exactly as it will be filled in, and three choices: open in the browser, copy the text, cancel.

#### Scenario: Open the dialog
- **WHEN** the user presses `!` in the sessions view
- **THEN** a "Report a problem" dialog opens with an empty title box, the preview and the three choices, and the tab reads `Torni (Report a problem)`

#### Scenario: Cancel
- **WHEN** the user presses Esc in the dialog
- **THEN** the dialog closes, nothing is opened or copied, and the crash file is left as it was

### Requirement: The report holds only diagnostics
The diagnostics text SHALL hold only the environment (the Torni, Claude Code, Windows and .NET versions, the terminal's size, the editor preset, whether update checks are on and the number of repository roots), the recorded diagnostic events, the child program counts and the last crash when there is one, rendered as the diagnostic log renders them. The title SHALL be the only text the user typed, and it SHALL NOT be recorded in the diagnostic log.

#### Scenario: Preview after some work
- **WHEN** the user has opened "Fix flaky tests" in C:\Users\me\source\worktrees\Northwind.Orders-fix-123 in its tab and a worktree remove has failed, then presses `!`
- **THEN** the preview shows the versions, the open-in-tab effect, the failed remove with its exception summary and the counts, and neither "Fix flaky tests", "Northwind.Orders" nor "C:\Users\me" appears in it

#### Scenario: The title is not logged
- **WHEN** the user types the title "Attach froze after Ctrl+Z" and opens the report
- **THEN** the diagnostic log records that a report was opened and does not contain the title

### Requirement: Opening the report uses the issue form
Choosing to open SHALL open the default browser on the `Source191/torni` new-issue page for the "Problem report" form, with the title and the diagnostics field filled in, so the user writes the description, can change anything, and submits from their own account. When the title is blank, the title SHALL be `Problem report from Torni <version>`. Torni SHALL make no network request of its own.

#### Scenario: Open with a title
- **WHEN** the user types "Attach froze after Ctrl+Z" and chooses to open
- **THEN** the browser opens `https://github.com/Source191/torni/issues/new?template=problem-report.yml` with that title and the diagnostics filled in, and the status line says the report was opened in the browser

#### Scenario: Open with a blank title
- **WHEN** Torni 0.18.0 opens a report with a blank title
- **THEN** the page's title reads "Problem report from Torni 0.18.0"

#### Scenario: The browser cannot be started
- **WHEN** Windows cannot open the address
- **THEN** the status line reports the failure and suggests copying the text instead, and the crash file is left as it was

### Requirement: A crash is reported once
When `last-crash.log` exists, the report SHALL include it under its own heading, and once the browser has been opened successfully Torni SHALL remove it.

#### Scenario: Report after a crash
- **WHEN** Torni crashed earlier, has been started again and the user opens a report
- **THEN** the diagnostics hold the crash under its heading, and afterwards `last-crash.log` no longer exists

#### Scenario: No crash
- **WHEN** no crash file exists
- **THEN** the report has no crash heading
