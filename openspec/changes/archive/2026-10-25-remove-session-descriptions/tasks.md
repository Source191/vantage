# Tasks

## 1. The launch path

- [x] 1.1 `HereCommandShould`: `-d x` and `--description x` answer `LaunchProblem.UnknownOption` (red first); `LaunchPlanShould`'s fixtures lose the description. Remove the option from `HereCommand`, `DescriptionText` from `LaunchRequest` and `Description` from `LaunchPlan`. Verify both pass.
- [x] 1.2 `LaunchServiceShould`: a start writes no description and the service takes no description port (red first). Remove `RecordDescriptionAsync` and the parameter, delete `ISessionDescriptions` from `StatePorts.cs`, and drop it from `InMemoryState`. Verify the Core suite builds and passes.

## 2. The listing

- [x] 2.1 `SessionRowEnricherShould`: rows carry no description and the enricher takes no description port (red first). Remove `SessionRow.Description`, the enricher's read and parameter, and the DESCRIPTION column from `SessionsScreenDefinition`; update the row fixtures in `SessionTestData`, `SessionsViewShould`, `ScreenLoopShould`, `StateTransitionNotifierShould` and `WordingShould`, and `InMemoryStateShould` for the port. Verify the Core suite passes.

## 3. The wizard

- [x] 3.1 `WizardViewShould.NextFieldMovesNameToMode` replaces the name-to-description fact; the fixed-directory fact says Enter in the name stays; `Fill` and the `Echoes` theory lose `DescriptionChanged` (red first). Remove `DescriptionChanged`, `WizardField.Description`, `WizardState.Description`, the field and caption from `WizardControls` (moving the Where, base, preview and prompt rows up three lines) and the two subscriptions and the `Start` reread from `LaunchWizardDialog`. Verify the Core suite passes, then `dotnet run --project tools\HeadlessTerminal -- check` passes and the `wizard-as-reducer` drive script, minus its description Enter, replays the same screens as before apart from the missing field, with no blank rows where it was.

## 4. The state store

- [x] 4.1 `JsonFileStoreShould`: move `WriteARecordWhileAnotherProcessHasItOpenForReading` and `TreatAHalfWrittenRecordAsAbsentUntilItIsFinished` onto recent-repository records (a file under `recent\`), and narrow `ImportLegacyRecentAndSessionsFiles` to `ImportTheLegacyRecentFile`, so the sharing rules and the kept import stay covered; verify they pass before anything is removed.
- [x] 4.2 `JsonFileStoreShould.LeaveOldDescriptionFilesUntouched`: the store neither reads `descriptions\` nor imports `sessions.json`, and a folder holding both is left byte for byte as it was after reading recent repositories and created worktrees (red first). Delete `KeepDescriptionsWrittenByTwoWriters`, `RoundTripSessionDescriptions` and `TreatACorruptDescriptionsFileAsEmpty`. Remove `DescriptionsFolder`, `DescriptionFile`, `LegacySessionsFile`, `ImportLegacyDescriptionsAsync`, `SessionDescription`, the read and write, and the interface from the class, keeping `ImportLegacyRecentAsync`; drop the `store` argument from `LaunchService` and `SessionRowEnricher` in `Program.cs`. Verify the solution suite passes and `grep -rni description src` finds nothing.

## 5. Documentation and release

- [x] 5.1 README: remove the DESCRIPTION column row, the wizard field bullet, the `-d` option, the "with a description" phrase in the scenario, the `descriptions\` table row and the one-file-each sentence, and add one sentence to the state folder section saying `descriptions\` and `sessions.json` left by earlier versions are no longer used and can be deleted; raise `Version` by a minor. Verify `openspec validate remove-session-descriptions` passes, `.github\scripts\Assert-VersionBumped.ps1` passes and `grep -ni description README.md` finds only that sentence.

## Workflow follow-up

- P1 in `public-release`'s sequence: the gate, branched from `main` and merged before every other change.
- One pull request, the first of the agreed set; archive the change in it so the specs lose the requirements with the code.
- Manual check: the wizard has no Description field and Enter in the name lands on Where; `vantage here -d x` exits 2 with the usage line; the sessions view has no DESCRIPTION column; a state folder with an old `descriptions\` folder still opens.
