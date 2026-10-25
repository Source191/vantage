## ADDED Requirements

### Requirement: An environment variable turns the check off
When the environment variable `VANTAGE_NO_UPDATE_CHECK` is set to any value with a non-whitespace character, Vantage SHALL make no update request, SHALL NOT write `update-check.json`, and SHALL show no update notice, whatever the `checkForUpdates` setting says. The hand-off harness SHALL set it on every Vantage process it starts.

#### Scenario: Variable set
- **WHEN** `VANTAGE_NO_UPDATE_CHECK` is `1` and the last recorded check is more than 24 hours old
- **THEN** Vantage makes no request to nuget.org, writes nothing to `update-check.json` and the header shows only its own version

#### Scenario: Variable empty or absent
- **WHEN** `VANTAGE_NO_UPDATE_CHECK` is unset, empty or blank
- **THEN** the check follows the `checkForUpdates` setting as before

#### Scenario: Harness run
- **WHEN** the hand-off check starts Vantage
- **THEN** the Vantage process has `VANTAGE_NO_UPDATE_CHECK` set, whatever the harness inherited
