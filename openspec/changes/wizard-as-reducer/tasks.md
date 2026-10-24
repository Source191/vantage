# Tasks

## 1. Reducer

- [ ] 1.1 `WizardViewShould.OpenWithRecentRepositoriesFirstAndTheFirstChoiceSelected`, `NarrowChoicesAsTheFilterChanges`, `MoveAndPageTheListWithinBounds`: implement `WizardState.Open`, `FilterChanged`, `ListMoved`, `ListPaged`
- [ ] 1.2 `WizardViewShould.ConfirmTheRepositoryAndMoveFocusToName`, `RefuseToConfirmWithNoChoice`: implement `ConfirmRepository` with the problem text and focus
- [ ] 1.3 `WizardViewShould.PreviewFollowsNameModeAndSelection`: implement the preview recomputation using `LaunchPreview.Describe`
- [ ] 1.4 `WizardViewShould.NextFieldMovesNameToDescriptionToMode`: implement `NextField`
- [ ] 1.5 `WizardViewShould.StartRefusesNoRepositoryBlankNameAndPlanProblemsWithTheRightFocus`, `StartYieldsALaunchEffect`, `StartAndOpenInTabYieldsALaunchEffectWithOpenInTab`, `CancelYieldsClose`: implement `Start` and `Cancel` through `LaunchPlanBuilder`
- [ ] 1.6 `WizardViewShould.FixedDirectoryHidesTheChooserAndWorktreeControls`, `FixedDirectoryStartsInThatFolder`: implement the fixed-directory variant
- [ ] 1.7 `WizardViewShould.ArrowsInTheFilterDriveTheListAndLettersInTheListReturnToTheFilter`: implement the focus switching inputs
- [ ] 1.8 `WizardViewShould.BeIdempotentForARepeatedInput`: verify applying the same input twice yields the same state

## 2. Binder

- [ ] 2.1 Rewrite `LaunchWizardDialog` as controls plus `Apply(input)` plus `Push(state)` with the `binding` guard, removing every local function that decides anything; verify `dotnet build` and `dotnet format --verify-no-changes` pass and the file is under 120 lines
- [ ] 2.2 Run `dotnet run --project tools\HeadlessTerminal -- check` and a scripted `drive` through the wizard (filter, arrows, Enter, name, F5) capturing to `drive-raw.txt`; verify the capture matches a capture taken before this change

## 3. Documentation

- [ ] 3.1 Update CLAUDE.md (the wizard is a reducer; dialogs are binders) and remove the sentence in `docs\architecture-review.md` marking this as a later item, or delete that document now that every item has an archived change; verify `openspec validate` passes for every open change

## Workflow follow-up

- PR 19a: task group 1 (`WizardState`, `WizardInput`, `WizardView.Reduce` and `WizardViewShould`). New files in Core only, depending on `LaunchPlanBuilder`, `LaunchPreview` and `IFolders`, so it can be developed alongside any PR from 8 to 18 and merged as soon as it is green.
- PR 19b: task groups 2 and 3 (the binder and documentation). Needs `consolidate-helpers` PR 18 (the dialog's dependencies) and `outcomes-and-wording` PR 13 (its sentences). Archive the change in this PR. Bump `Version` to 0.3.2 if a release is wanted for the README's manual wizard checks.
