## MODIFIED Requirements

### Requirement: The update key updates Vantage in a new tab
`U` in the sessions and worktrees views SHALL ask for confirmation and, when confirmed, open a Windows Terminal tab that waits for this Vantage to exit, runs `dotnet tool update -g Source191.Vantage --source https://api.nuget.org/v3/index.json`, so the package comes from the feed the check consulted and from no other source the machine has configured, and starts Vantage again when the update succeeds; Vantage SHALL then quit. Background sessions SHALL keep running.

#### Scenario: Confirmed update
- **WHEN** an update to 0.10.0 is available and the user presses `U` and confirms
- **THEN** a tab titled for the update opens, Vantage quits, the tab updates the tool from nuget.org and starts Vantage 0.10.0, and every background session is still listed

#### Scenario: Cancelled
- **WHEN** the user presses `U` and cancels the confirmation
- **THEN** nothing is opened and Vantage stays on the same view and selection

#### Scenario: Update fails
- **WHEN** `dotnet tool update` fails in the update tab
- **THEN** the tab stays open showing dotnet's output, and Vantage is not started again

#### Scenario: No update known
- **WHEN** the user presses `U` and no newer release is recorded, or the check is off
- **THEN** no dialog opens and the status line says that Vantage is up to date or that the check is off
