# tool-updates Specification

## Purpose
Lets a person who installed Vantage from nuget.org learn that a newer release exists and update to it from inside Vantage, without the running tool blocking its own update.

## Requirements

### Requirement: Vantage checks for a newer release at most once a day
Vantage SHALL ask nuget.org for the published versions of its package at most once in any 24 hours, recording when it last asked and the newest release it found. When the newest release is higher than the running version, the same check SHALL ask nuget.org once for that release's notes and record them with it. The check SHALL start in the background when the console opens and SHALL NOT delay the first screen, a key or a refresh.

#### Scenario: First start of the day
- **WHEN** Vantage starts and its last recorded check is more than 24 hours old
- **THEN** it requests the package's version list once and records the time and the newest release version

#### Scenario: A newer release is found
- **WHEN** the check finds 0.21.0 while Vantage 0.19.0 runs
- **THEN** it requests the 0.21.0 package's release notes once, from nuget.org, and records them with 0.21.0

#### Scenario: Already the newest
- **WHEN** the check finds the running version is the newest release
- **THEN** it requests no release notes

#### Scenario: The notes cannot be read
- **WHEN** the version list names 0.21.0 but the request for its notes fails or takes longer than 3 seconds
- **THEN** 0.21.0 is recorded without notes, the update is still offered, and the next due check asks for the notes again

#### Scenario: Checked recently
- **WHEN** Vantage starts within 24 hours of its last recorded check
- **THEN** it makes no request and uses the recorded newest release and its notes

#### Scenario: A recorded release is shown from the start
- **WHEN** Vantage 0.16.0 using 29 MB starts within 24 hours of a check that recorded 0.99.0
- **THEN** the header reads `Vantage: 0.16.0 (29 MB; 0.99.0 available, U to update)` without a key being pressed, and `U` offers the update

#### Scenario: nuget.org cannot be reached
- **WHEN** the request fails or takes longer than 3 seconds
- **THEN** Vantage shows no update notice, says nothing about the failure, and tries again on the next start

#### Scenario: The check fails unexpectedly
- **WHEN** anything inside the check fails, such as the record that cannot be written
- **THEN** the console keeps running and shows no update notice, the failure is recorded only in the diagnostic log (as an exception summary, also written to the `VANTAGE_LOG` file when that is set), and the next start tries again

### Requirement: Development builds never check
A Vantage whose own version is a prerelease (a local or CI build) SHALL NOT check for updates and SHALL NOT offer one.

#### Scenario: Local build
- **WHEN** Vantage `0.10.0-local` starts
- **THEN** no request is made and the header shows no update notice

### Requirement: The check can be turned off
The user configuration SHALL have a `checkForUpdates` setting, true when absent, editable in the settings dialog. When it is false Vantage SHALL make no update request and show no notice.

#### Scenario: Turned off
- **WHEN** `checkForUpdates` is false
- **THEN** Vantage makes no request to nuget.org and the header shows only its own version and memory

#### Scenario: Configuration from an earlier version
- **WHEN** `config.json` has no `checkForUpdates` property
- **THEN** Vantage checks for updates as if it were true

### Requirement: The header names an available update
When the newest recorded release is higher than the running version by semantic version precedence, the header's Vantage version line SHALL name that release and the update key after Vantage's own memory figure, inside the same brackets, drawn in amber. Prerelease versions on nuget.org SHALL be ignored.

#### Scenario: A newer release exists
- **WHEN** Vantage 0.9.0 runs using 29 MB and the newest recorded release is 0.10.0
- **THEN** the header reads `Vantage: 0.9.0 (29 MB; 0.10.0 available, U to update)`, with `0.10.0 available, U to update` in amber

#### Scenario: Already up to date
- **WHEN** the newest recorded release equals the running version
- **THEN** the header reads `Vantage: 0.9.0 (29 MB)` with no notice

### Requirement: The update key updates Vantage in a new tab
`U` in the sessions and worktrees views SHALL open the What's new dialogue and, when Update now is chosen, open a Windows Terminal tab that waits for this Vantage to exit, runs `dotnet tool update -g Source191.Vantage --source https://api.nuget.org/v3/index.json`, so the package comes from the feed the check consulted and from no other source the machine has configured, and starts Vantage again when the update succeeds; Vantage SHALL then quit. Background sessions SHALL keep running.

#### Scenario: Confirmed update
- **WHEN** an update to 0.10.0 is available and the user presses `U` and chooses Update now
- **THEN** a tab titled for the update opens, Vantage quits, the tab updates the tool from nuget.org and starts Vantage 0.10.0, and every background session is still listed

#### Scenario: Cancelled
- **WHEN** the user presses `U` and chooses Later or presses Esc
- **THEN** nothing is opened and Vantage stays on the same view and selection

#### Scenario: Update fails
- **WHEN** `dotnet tool update` fails in the update tab
- **THEN** the tab stays open showing dotnet's output, and Vantage is not started again

#### Scenario: No update known
- **WHEN** the user presses `U` and no newer release is recorded, or the check is off
- **THEN** no dialog opens and the status line says that Vantage is up to date or that the check is off

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

### Requirement: A newly found release is announced once
When the check finds a newest release higher than both the running version and the release it had recorded before, the status line SHALL say once that the release is available and that `U` shows what changed. A release already recorded SHALL NOT be announced again on later starts.

#### Scenario: Found today
- **WHEN** the record named 0.20.0 and today's check finds 0.21.0 while Vantage 0.19.0 runs
- **THEN** the status line says 0.21.0 is available and to press U to see what changed

#### Scenario: Known since yesterday
- **WHEN** Vantage 0.19.0 starts within 24 hours of a check that recorded 0.21.0
- **THEN** the header names 0.21.0 and the status line says nothing about it

#### Scenario: Found on a first check
- **WHEN** no check has been recorded and the first check finds 0.21.0 while Vantage 0.19.0 runs
- **THEN** the status line says 0.21.0 is available and to press U to see what changed
