# Spec Delta

## MODIFIED Requirements

### Requirement: Vantage checks for a newer release at most once a day
Vantage SHALL ask the winget community repository for the published versions of its package at most once in any 24 hours, recording when it last asked and the newest release it found. When the newest release is higher than the running version, the same check SHALL ask GitHub once for that release's changelog and record its notes with it. The check SHALL start in the background when the console opens and SHALL NOT delay the first screen, a key or a refresh.

#### Scenario: First start of the day
- **WHEN** Vantage starts and its last recorded check is more than 24 hours old
- **THEN** it requests the package's version list once and records the time and the newest release version

#### Scenario: A newer release is found
- **WHEN** the check finds 0.21.0 while Vantage 0.19.0 runs
- **THEN** it requests the changelog at the 0.21.0 tag once, from GitHub, and records its notes with 0.21.0

#### Scenario: Already the newest
- **WHEN** the check finds the running version is the newest release
- **THEN** it requests no release notes

#### Scenario: The notes cannot be read
- **WHEN** the version list names 0.21.0 but the request for its changelog fails or takes longer than 3 seconds
- **THEN** 0.21.0 is recorded without notes, the update is still offered, and the next due check asks for the notes again

#### Scenario: Checked recently
- **WHEN** Vantage starts within 24 hours of its last recorded check
- **THEN** it makes no request and uses the recorded newest release and its notes

#### Scenario: A recorded release is shown from the start
- **WHEN** Vantage 0.16.0 using 29 MB starts within 24 hours of a check that recorded 0.99.0
- **THEN** the header reads `Vantage: 0.16.0 (29 MB; 0.99.0 available, U to update)` without a key being pressed, and `U` offers the update

#### Scenario: nuget.org cannot be reached
- **WHEN** the request to the winget community repository, or the one to GitHub, fails or takes longer than 3 seconds
- **THEN** Vantage shows no update notice, says nothing about the failure, and tries again on the next start

#### Scenario: The check fails unexpectedly
- **WHEN** anything inside the check fails, such as the record that cannot be written
- **THEN** the console keeps running and shows no update notice, the failure is recorded only in the diagnostic log (as an exception summary, also written to the `VANTAGE_LOG` file when that is set), and the next start tries again

#### Scenario: A release not yet merged
- **WHEN** v0.21.0 is a GitHub Release but its manifest is not yet in the winget community repository
- **THEN** the check does not find 0.21.0 and no update to it is offered

### Requirement: The check can be turned off
The user configuration SHALL have a `checkForUpdates` setting, true when absent, editable in the settings dialog. When it is false Vantage SHALL make no update request and show no notice.

#### Scenario: Turned off
- **WHEN** `checkForUpdates` is false
- **THEN** Vantage makes no request to the winget community repository or GitHub and the header shows only its own version and memory

#### Scenario: Configuration from an earlier version
- **WHEN** `config.json` has no `checkForUpdates` property
- **THEN** Vantage checks for updates as if it were true

### Requirement: The header names an available update
When the newest recorded release is higher than the running version by semantic version precedence, the header's Vantage version line SHALL name that release and the update key after Vantage's own memory figure, inside the same brackets, drawn in amber. Versions the winget community repository lists that are prereleases SHALL be ignored.

#### Scenario: A newer release exists
- **WHEN** Vantage 0.9.0 runs using 29 MB and the newest recorded release is 0.10.0
- **THEN** the header reads `Vantage: 0.9.0 (29 MB; 0.10.0 available, U to update)`, with `0.10.0 available, U to update` in amber

#### Scenario: Already up to date
- **WHEN** the newest recorded release equals the running version
- **THEN** the header reads `Vantage: 0.9.0 (29 MB)` with no notice

### Requirement: The update key updates Vantage in a new tab
`U` in the sessions and worktrees views SHALL open the What's new dialogue and, when Update now is chosen, open a Windows Terminal tab that waits for this Vantage to exit, runs `winget upgrade --id Source191.Vantage --exact --source winget`, and starts Vantage again when the upgrade succeeds; Vantage SHALL then quit. Background sessions SHALL keep running.

#### Scenario: Confirmed update
- **WHEN** an update to 0.10.0 is available and the user presses `U` and chooses Update now
- **THEN** a tab titled for the update opens, Vantage quits, the tab upgrades the package with winget and starts Vantage 0.10.0, and every background session is still listed

#### Scenario: Cancelled
- **WHEN** the user presses `U` and chooses Later or presses Esc
- **THEN** nothing is opened and Vantage stays on the same view and selection

#### Scenario: Update fails
- **WHEN** `winget upgrade` fails in the update tab, for example because another Vantage still runs or winget has not yet indexed the release
- **THEN** the tab stays open showing winget's output, and Vantage is not started again

#### Scenario: No update known
- **WHEN** the user presses `U` and no newer release is recorded, or the check is off
- **THEN** no dialog opens and the status line says that Vantage is up to date or that the check is off
