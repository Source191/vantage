# Tasks

## 1. Domain: settings, editor and placement

- [ ] 1.1 `SettingsShould.ResolveAMissingFileToEveryDefault`, `ResolveAFileFromAnEarlierVersion` (roots `D:\work`, base Head, no worktree root or editor gives `D:\worktrees` and VS Code), `ResolveAnEmptyRootListToTheDefaultRoot`, `PutTheWorktreeRootInsideARootWithNoParent`: implement `Domain\Settings` and `Settings.From(VantageConfig?, FolderPath home)`; verify the tests pass
- [ ] 1.2 `EditorChoiceShould.DefaultToVsCode`, `KeepACustomEditorWithAFolderPlaceholder`, `ReadACustomEditorWithoutAPlaceholderAsVsCode`: implement `Domain\EditorChoice` and `EditorPreset` (every switch names all six members); verify the tests pass
- [ ] 1.3 `CommandArgumentsShould.SplitOnSpaces`, `KeepDoubleQuotedTextTogether`, `KeepAnEmptyLineEmpty`: implement `Domain\CommandArguments.Split`; verify the tests pass
- [ ] 1.4 `SettingsShould.RefuseNoRoots`, `RefuseARelativeRoot`, `RefuseAMissingRoot`, `RefuseARelativeWorktreeRoot`, `AcceptAWorktreeRootThatDoesNotExistYet`, `RefuseACustomEditorWithNoExecutable`, `RefuseABatchFileEditor`, `RefuseACustomEditorWithNoPlaceholder`: implement `Settings.TryParse` returning `Settings` or a `SettingsProblem`; verify the tests pass
- [ ] 1.5 `SettingsShould.RoundTripThroughTheFileRecord`: implement `Settings.ToConfig()` writing every property; verify the test passes
- [ ] 1.6 Rename `SiblingWorktreeShould` to `PlannedWorktreeShould` with `PlaceTheWorktreeUnderTheWorktreeRoot` (`C:\Users\me\source\worktrees\Northwind.Orders-fix-123`, branch `fix-123`); implement `PlannedWorktree.For(worktreeRoot, repository, slug)` replacing `SiblingWorktree`; verify the test passes and `dotnet build -c Debug` is clean

## 2. Ports and the state file

- [ ] 2.1 Make `VantageConfig`'s members nullable and add `WorktreeRoot` and `Editor` (`EditorSetting` record: preset, executable, arguments); `JsonFileStoreShould.ReadAnEarlierConfigWithoutTheNewProperties`, `WriteTheEditorAsAPresetObject`, `WriteACustomEditorWithItsArgumentList`, `ReadACorruptConfigAsAbsent`: verify the tests pass with property names `repositoryRoots`, `worktreeRoot`, `editor`, `defaultBase`
- [ ] 2.2 `GitCliIntegrationShould.AddAWorktreeUnderAFolderThatDoesNotExistYet` against a `TemporaryGitRoot`: verify git creates the missing worktree root; if it does not, add `IFolders.CreateDirectory` (with `FakeFolders` support) in this task and verify the test passes with it

## 3. Application: one holder, read on use

- [ ] 3.1 `CurrentSettingsShould.AnswerTheSettingsItWasBuiltWith`, `AnswerTheSavedSettingsAfterASave`, `WriteTheSavedSettingsThroughThePort` (with `InMemoryState`): implement `Application\CurrentSettings`; verify the tests pass
- [ ] 3.2 `LaunchPlanShould.PlaceANewWorktreeUnderTheWorktreeRoot`, `ReuseAnExistingFolderUnderTheWorktreeRoot`; `LaunchPreviewShould.NameTheFolderUnderTheWorktreeRoot`: pass the worktree root into `LaunchPlanBuilder.Build` and `LaunchPreview.Describe`; verify the tests pass
- [ ] 3.3 `WizardViewShould.TakeTheWorktreeRootAndBaseFromTheSettings`: `WizardState.Open` takes the settings snapshot instead of a default base; verify the test passes and the existing wizard tests still pass
- [ ] 3.4 `RepositoryCatalogueShould.OfferMainCheckoutsOnly`, `UseTheRootsSavedAfterItWasBuilt`: keep only `.git` folders and read roots from `CurrentSettings` per call; verify the tests pass
- [ ] 3.5 `WorktreeInventoryLeftoversShould.FindABrokenWorktreeUnderTheWorktreeRoot`, `NotListAFolderTwiceWhenTheWorktreeRootIsARoot`, `ScanTheRootsSavedAfterItWasBuilt`; `WorktreeInventoryShould.ListWorktreesInBothPlacements`: add the worktree root as a candidate source and read `CurrentSettings` per scan; verify the tests pass
- [ ] 3.6 `LaunchServiceShould.RecordAWorktreeCreatedUnderTheWorktreeRoot`: verify the created-worktree record carries the new path

## 4. The settings key

- [ ] 4.1 `SettingsViewShould.OpenWithTheCurrentSettings`, `FollowEachControlChange`, `RefuseToSaveWithTheProblemAndItsField` (one case per `SettingsProblem`), `SaveYieldsSaveSettings`, `CancelYieldsClose`, `ShowTheCustomEditorFieldsOnlyForCustom`, `BeIdempotentForARepeatedInput`: implement `SettingsState`, `SettingsInput`, `SettingsView.Reduce`; verify the tests pass
- [ ] 4.2 `SessionsViewShould.OpenSettingsOnComma`, `WorktreesViewShould.OpenSettingsOnComma`: add the `OpenSettings` effect (an `IDialogEffect`) and both reducer cases; verify the tests pass
- [ ] 4.3 `SaveSettingsHandlerShould.SaveThroughCurrentSettingsAndReportSettingsSaved`: implement `SaveSettingsHandler` (`IEffectHandler<SaveSettings>`) returning the `SettingsSaved` outcome in its own file, and register it in `Composition.Handlers`; verify the test and `EffectDispatcherShould` pass
- [ ] 4.4 `WordingShould.SaySettingsProblems` (each `SettingsProblem`), `SayTheSettingsKeyInHelp`; `WordingOfOutcomesShould.SaySettingsSaved`: add the dialog's title, captions, problems, the help lines for `,` and the outcome words; verify the reflection facts and these tests pass
- [ ] 4.5 `SettingsControls` (layout) and `SettingsDialog` (binder with the `binding` guard, no decisions) in `Tui`, and the `OpenSettings` flow in `DialogEffects` dispatching `SaveSettings`; `DialogEffectsShould` passes; verify `dotnet build -c Debug` and `dotnet format --verify-no-changes` are clean

## 5. Wiring and the header

- [ ] 5.1 `Program.cs` builds `CurrentSettings` from `config.json` and passes it to `RepositoryCatalogue`, `WorktreeInventory`, `DialogEffects` and both screens in place of copied values; `TableScreen` takes the header context as a function called on each redraw; verify `dotnet test -c Debug --no-build` passes at the solution
- [ ] 5.2 Add `Worktree root:` to both definitions' header names, widen the label column to 14 and shift the value and key-hint columns; verify with `dotnet run --project tools\HeadlessTerminal -- drive` then `grid` at 120 columns that no header text overlaps, and that the header shows the new root right after a save from the dialog
- [ ] 5.3 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal`; verify exit 0 (the dialog must not block the UI thread)

## 6. Documentation and release

- [ ] 6.1 README: the settings key in both key tables, the settings dialog, `config.json`'s four properties and defaults in "Where Vantage keeps things", the worktree root in the wizard section (folder under the worktree root, not beside the repository) and that existing sibling worktrees are left where they are; verify the text matches the dialog
- [ ] 6.2 CLAUDE.md (the settings dialog is a reducer and binder; `CurrentSettings` is the one home of settings) and `openspec\config.yaml`'s worktree line (worktrees go to `<worktree root>\<repository>-<slug>` on branch `<slug>`, never under `.claude\worktrees`); verify `openspec validate --all` passes
- [ ] 6.3 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next minor after `main`'s; verify `.github\scripts\Assert-VersionBumped.ps1` passes locally

## Workflow follow-up

- Merge after `header-and-help` and `remove-session-descriptions`; rebase the header context and `LaunchRequest` edits if either lands later.
- Archive the change in the same pull request (`/opsx:archive settings-and-worktree-root`), so `openspec\specs` gains `settings` and the modified `session-launch` and `worktree-management` requirements.
