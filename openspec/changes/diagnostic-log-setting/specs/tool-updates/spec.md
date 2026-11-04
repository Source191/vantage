# Spec Delta

## MODIFIED Requirements

### Requirement: Torni checks for a newer release at most once a day
Torni SHALL ask nuget.org for the published versions of its package at most once in any 24 hours, recording when it last asked and the newest release it found. When the newest release is higher than the running version, the same check SHALL ask nuget.org once for that release's notes and record them with it. The check SHALL start in the background when the console opens and SHALL NOT delay the first screen, a key or a refresh.

#### Scenario: First start of the day
- **WHEN** Torni starts and its last recorded check is more than 24 hours old
- **THEN** it requests the package's version list once and records the time and the newest release version

#### Scenario: A newer release is found
- **WHEN** the check finds 0.21.0 while Torni 0.19.0 runs
- **THEN** it requests the 0.21.0 package's release notes once, from nuget.org, and records them with 0.21.0

#### Scenario: Already the newest
- **WHEN** the check finds the running version is the newest release
- **THEN** it requests no release notes

#### Scenario: The notes cannot be read
- **WHEN** the version list names 0.21.0 but the request for its notes fails or takes longer than 3 seconds
- **THEN** 0.21.0 is recorded without notes, the update is still offered, and the next due check asks for the notes again

#### Scenario: Checked recently
- **WHEN** Torni starts within 24 hours of its last recorded check
- **THEN** it makes no request and uses the recorded newest release and its notes

#### Scenario: A recorded release is shown from the start
- **WHEN** Torni 0.16.0 using 29 MB starts within 24 hours of a check that recorded 0.99.0
- **THEN** the header reads `Torni: 0.16.0 (29 MB; 0.99.0 available, U to update)` without a key being pressed, and `U` offers the update

#### Scenario: nuget.org cannot be reached
- **WHEN** the request fails or takes longer than 3 seconds
- **THEN** Torni shows no update notice, says nothing about the failure, and tries again on the next start

#### Scenario: The check fails unexpectedly
- **WHEN** anything inside the check fails, such as the record that cannot be written
- **THEN** the console keeps running and shows no update notice, the failure is recorded only in the diagnostic log (as an exception summary, also written to the log file while the Diagnostic log setting is on), and the next start tries again
