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
- **WHEN** Vantage 0.16.0 starts within 24 hours of a check that recorded 0.99.0
- **THEN** the header reads `Vantage: 0.16.0 (0.99.0 available, U to update)` without a key being pressed, and `U` offers the update

#### Scenario: nuget.org cannot be reached
- **WHEN** the request fails or takes longer than 3 seconds
- **THEN** Vantage shows no update notice, says nothing about the failure, and tries again on the next start

#### Scenario: The check fails unexpectedly
- **WHEN** anything inside the check fails, such as the record that cannot be written
- **THEN** the console keeps running and shows no update notice, the failure is recorded only in the diagnostic log (as an exception summary, also written to the `VANTAGE_LOG` file when that is set), and the next start tries again
