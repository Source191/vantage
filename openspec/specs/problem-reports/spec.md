# problem-reports Specification

## Purpose
Lets a user report a problem with Vantage as a GitHub issue that carries their own description and Vantage's privacy-safe diagnostics, with nothing leaving the machine until they choose to open or submit it.

## Requirements

### Requirement: A key opens the problem report
Both table views SHALL offer `!` (Report a problem), listed in the key hints, the help and the tooltips like every other key. It SHALL open a dialog with a title box and a read-only preview of the diagnostics text exactly as it will be filled in, and three choices: open in the browser, copy the text, cancel.

#### Scenario: Open the dialog
- **WHEN** the user presses `!` in the sessions view
- **THEN** a "Report a problem" dialog opens with an empty title box, the preview and the three choices, and the tab reads `Vantage (Report a problem)`

#### Scenario: Cancel
- **WHEN** the user presses Esc in the dialog
- **THEN** the dialog closes, nothing is opened or copied, and the crash file is left as it was

### Requirement: The report holds only diagnostics
The diagnostics text SHALL hold only the environment (the Vantage, Claude Code, Windows and .NET versions, the terminal's size, the editor preset, whether update checks are on and the number of repository roots), the recorded diagnostic events, the child program counts and the last crash when there is one, rendered as the diagnostic log renders them. The title SHALL be the only text the user typed, and it SHALL NOT be recorded in the diagnostic log.

#### Scenario: Preview after some work
- **WHEN** the user has opened "Fix flaky tests" in C:\Users\me\source\worktrees\Northwind.Orders-fix-123 in its tab and a worktree remove has failed, then presses `!`
- **THEN** the preview shows the versions, the open-in-tab effect, the failed remove with its exception summary and the counts, and neither "Fix flaky tests", "Northwind.Orders" nor "C:\Users\me" appears in it

#### Scenario: The title is not logged
- **WHEN** the user types the title "Attach froze after Ctrl+Z" and opens the report
- **THEN** the diagnostic log records that a report was opened and does not contain the title

### Requirement: The dialog says where the text goes
The dialog SHALL state that the text holds no names, paths or code, that opening the page passes the title and the text to GitHub in the page's address, and that the report becomes public only when the user submits it there.

#### Scenario: The explanation is shown
- **WHEN** the dialog opens
- **THEN** its explanation line says that opening the page passes the text to GitHub and that nothing is public until submitted

### Requirement: Opening the report uses the issue form
Choosing to open SHALL open the default browser on the `Source191/vantage` new-issue page for the "Problem report" form, with the title and the diagnostics field filled in, so the user writes the description, can change anything, and submits from their own account. When the title is blank, the title SHALL be `Problem report from Vantage <version>`. Vantage SHALL make no network request of its own.

#### Scenario: Open with a title
- **WHEN** the user types "Attach froze after Ctrl+Z" and chooses to open
- **THEN** the browser opens `https://github.com/Source191/vantage/issues/new?template=problem-report.yml` with that title and the diagnostics filled in, and the status line says the report was opened in the browser

#### Scenario: Open with a blank title
- **WHEN** Vantage 0.18.0 opens a report with a blank title
- **THEN** the page's title reads "Problem report from Vantage 0.18.0"

#### Scenario: The browser cannot be started
- **WHEN** Windows cannot open the address
- **THEN** the status line reports the failure and suggests copying the text instead, and the crash file is left as it was

### Requirement: The report fits GitHub's address limit
The page's address SHALL be at most 7,500 characters. When the full diagnostics would make it longer, the oldest events that are not failures SHALL be left out first, then the counts with exit code 0, then the oldest failures, and the text SHALL end with a line saying how many lines were left out; the environment and the crash summary SHALL always be kept.

#### Scenario: A long session
- **WHEN** the log holds 500 events and the full text would make a 20,000-character address
- **THEN** the address is at most 7,500 characters, the environment, every failure that fits and the crash summary are present, and the last line says how many lines were left out

### Requirement: The text can be copied instead
Choosing to copy SHALL put the title and the full diagnostics text, untrimmed, on the clipboard and close the dialog, without opening a browser.

#### Scenario: Copy
- **WHEN** the user chooses to copy
- **THEN** the clipboard holds the title and the whole diagnostics text, the status line says it was copied, and the crash file is left as it was

### Requirement: A crash is reported once
When `last-crash.log` exists, the report SHALL include it under its own heading, and once the browser has been opened successfully Vantage SHALL remove it.

#### Scenario: Report after a crash
- **WHEN** Vantage crashed earlier, has been started again and the user opens a report
- **THEN** the diagnostics hold the crash under its heading, and afterwards `last-crash.log` no longer exists

#### Scenario: No crash
- **WHEN** no crash file exists
- **THEN** the report has no crash heading
