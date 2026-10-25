# Proposal

## Why

The session description is an optional free-text label typed in the wizard or given with `vantage here -d`, stored against the session id in its own state folder and shown in the DESCRIPTION column. The product owner does not use it. It carries a wizard field, a command-line option, a state port with a folder and a legacy import, a column, three spec requirements and fourteen test files, for no benefit. Removing it now, before the wizard, the launch request and the state store are reworked by the changes that follow, is the simplest cut.

## What Changes

- **BREAKING: the wizard loses the Description field.** Enter in the name moves straight to the Where choice (and stays in the name when the chooser is hidden for a fixed directory).
- **BREAKING: `vantage here` loses `-d` and `--description`.** Giving either is an unknown option: exit 2 with the usage line, which no longer lists it.
- **The DESCRIPTION column goes** from the sessions view, and rows have no description text.
- **The description state goes:** the `ISessionDescriptions` port, the launch service's write, the row enricher's read, the `descriptions\` folder code and the import of the legacy `sessions.json`. Files already on disc are neither read nor deleted.
- **README:** the column table row, the wizard field bullet, the `-d` option, the "with a description" scenario, the state folder table row and the sentence about one-file-per-description.
- **Version:** a minor bump, because a `vantage here` option and a column are removed while the tool is pre-1.0.

Out of scope: any replacement label, a transitional message for `-d`, deleting old state files for the user, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-launch`: "A description is optional and travels with the session" is removed; "A session can be started from the shell in the current folder" loses the `-d` option; "Vantage records what it created" no longer records descriptions and says old description files are ignored.
- `session-listing`: "Descriptions are shown with their session" is removed.

## Impact

- **Core:** `Application\Launch\LaunchPlan.cs` (`LaunchRequest.DescriptionText`, `LaunchPlan.Description`), `HereCommand.cs` (the option), `LaunchService.cs` (`RecordDescriptionAsync` and its port parameter), `SessionRowEnricher.cs` (the read and its port parameter), `Views\SessionsView.cs` (`SessionRow.Description`), `Views\WizardInput.cs` (`DescriptionChanged`, `WizardField.Description`), `WizardState.cs`, `WizardView.cs` (`NextField`), `Ports\StatePorts.cs` (`ISessionDescriptions`).
- **Infrastructure:** `State\JsonFileStore.cs` (the folder, the read and write, the legacy import, the `SessionDescription` record, the interface list).
- **Tui:** `WizardControls.cs`, `LaunchWizardDialog.cs`, `SessionsScreenDefinition.cs`.
- **Program.cs:** one `store` argument fewer for `LaunchService` and `SessionRowEnricher`.
- **Tests:** fourteen files plus `TestDoubles\InMemoryState`.
- **Docs:** README; the two spec deltas; `Version`.
