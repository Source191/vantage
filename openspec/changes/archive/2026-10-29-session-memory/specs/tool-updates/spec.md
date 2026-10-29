# Spec Delta

## MODIFIED Requirements

### Requirement: Vantage checks for a newer release at most once a day
Vantage SHALL ask nuget.org for the published versions of its package at most once in any 24 hours, recording when it last asked and the newest release it found. The check SHALL start in the background when the console opens and SHALL NOT delay the first screen, a key or a refresh.

#### Scenario: First start of the day
- **WHEN** Vantage starts and its last recorded check is more than 24 hours old
- **THEN** it requests the package's version list once and records the time and the newest release version

#### Scenario: Checked recently
- **WHEN** Vantage starts within 24 hours of its last recorded check
- **THEN** it makes no request and uses the recorded newest release

#### Scenario: A recorded release is shown from the start
- **WHEN** Vantage 0.16.0 using 29 MB starts within 24 hours of a check that recorded 0.99.0
- **THEN** the header reads `Vantage: 0.16.0 (29 MB; 0.99.0 available, U to update)` without a key being pressed, and `U` offers the update

#### Scenario: nuget.org cannot be reached
- **WHEN** the request fails or takes longer than 3 seconds
- **THEN** Vantage shows no update notice, says nothing about the failure, and tries again on the next start

#### Scenario: The check fails unexpectedly
- **WHEN** anything inside the check fails, such as the record that cannot be written
- **THEN** the console keeps running and shows no update notice, the failure is recorded only in the diagnostic log (as an exception summary, also written to the `VANTAGE_LOG` file when that is set), and the next start tries again

### Requirement: The check can be turned off
The user configuration SHALL have a `checkForUpdates` setting, true when absent, editable in the settings dialog. When it is false Vantage SHALL make no update request and show no notice.

#### Scenario: Turned off
- **WHEN** `checkForUpdates` is false
- **THEN** Vantage makes no request to nuget.org and the header shows only its own version and memory

#### Scenario: Configuration from an earlier version
- **WHEN** `config.json` has no `checkForUpdates` property
- **THEN** Vantage checks for updates as if it were true

### Requirement: The header names an available update
When the newest recorded release is higher than the running version by semantic version precedence, the header's Vantage version line SHALL name that release and the update key after Vantage's own memory figure, inside the same brackets. Prerelease versions on nuget.org SHALL be ignored.

#### Scenario: A newer release exists
- **WHEN** Vantage 0.9.0 runs using 29 MB and the newest recorded release is 0.10.0
- **THEN** the header reads `Vantage: 0.9.0 (29 MB; 0.10.0 available, U to update)`

#### Scenario: Already up to date
- **WHEN** the newest recorded release equals the running version
- **THEN** the header reads `Vantage: 0.9.0 (29 MB)` with no notice
