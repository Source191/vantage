# Tasks

## 1. The launch path

- [ ] 1.1 `HereCommandShould`: `-d x` and `--description x` answer `LaunchProblem.UnknownOption` (red first); `LaunchPlanShould`'s fixtures lose the description. Remove the option from `HereCommand`, `DescriptionText` from `LaunchRequest` and `Description` from `LaunchPlan`. Verify both pass.
- [ ] 1.2 `LaunchServiceShould`: a start writes no description and the service takes no description port (red first). Remove `RecordDescriptionAsync` and the parameter, delete `ISessionDescriptions` from `StatePorts.cs`, and drop it from `InMemoryState`. Verify the Core suite builds and passes.

## 2. The listing

- [ ] 2.1 `SessionRowEnricherShould`: rows carry no description and the enricher takes no description port (red first). Remove `SessionRow.Description`, the enricher's read and parameter, and the DESCRIPTION column from `SessionsScreenDefinition`; update the row fixtures in `SessionTestData`, `SessionsViewShould`, `ScreenLoopShould`, `StateTransitionNotifierShould` and `WordingShould`, and `InMemoryStateShould` for the port. Verify the Core suite passes.

## 3. The wizard

- [ ] 3.1 `WizardViewShould.NextFieldMovesNameToMode` replaces the name-to-description fact; the fixed-directory fact says Enter in the name stays; `Fill` and the `Echoes` theory lose `DescriptionChanged` (red first). Remove `DescriptionChanged`, `WizardField.Description`, `WizardState.Description`, the field and caption from `WizardControls` and the two subscriptions from `LaunchWizardDialog`. Verify the Core suite passes, then `dotnet run --project tools\HeadlessTerminal -- check` passes and the `wizard-as-reducer` drive script, minus its description Enter, replays the same screens as before apart from the missing field.

## 4. The state store

- [ ] 4.1 `JsonFileStoreShould`: the store neither reads `descriptions\` nor imports `sessions.json`, and a folder holding both is left as it is (red first). Remove `DescriptionsFolder`, `LegacySessionsFile`, `ImportLegacyDescriptionsAsync`, `SessionDescription`, the read and write, and the interface from the class; drop the `store` argument from `LaunchService` and `SessionRowEnricher` in `Program.cs`. Verify the solution suite passes and `grep -rni description src` finds nothing.

## 5. Documentation and release

- [ ] 5.1 README: remove the DESCRIPTION column row, the wizard field bullet, the `-d` option, the "with a description" phrase in the scenario, the `descriptions\` table row and the one-file-each sentence; raise `Version` by a minor. Verify `openspec validate remove-session-descriptions` passes, `.github\scripts\Assert-VersionBumped.ps1` passes and `grep -ni description README.md` finds nothing.

## Workflow follow-up

- One pull request, the first of the agreed set; archive the change in it so the specs lose the requirements with the code.
- Manual check: the wizard has no Description field and Enter in the name lands on Where; `vantage here -d x` exits 2 with the usage line; the sessions view has no DESCRIPTION column; a state folder with an old `descriptions\` folder still opens.
