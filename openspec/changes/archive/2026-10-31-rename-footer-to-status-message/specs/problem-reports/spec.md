## MODIFIED Requirements

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

### Requirement: The text can be copied instead
Choosing to copy SHALL put the title and the full diagnostics text, untrimmed, on the clipboard and close the dialog, without opening a browser.

#### Scenario: Copy
- **WHEN** the user chooses to copy
- **THEN** the clipboard holds the title and the whole diagnostics text, the status line says it was copied, and the crash file is left as it was
