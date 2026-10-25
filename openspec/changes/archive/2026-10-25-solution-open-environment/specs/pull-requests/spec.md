## MODIFIED Requirements

### Requirement: The pull request key opens it in the browser
The pull request key SHALL open the selected row's pull request in the default browser, without passing Vantage's Claude Code session markers to it, and the footer SHALL name it, in both views.

#### Scenario: Open from the sessions view
- **WHEN** the user presses the pull request key on a row whose pull request is !4521
- **THEN** that pull request's page opens in the default browser, without Vantage's Claude Code session markers in the browser's environment, and the footer says !4521 was opened

