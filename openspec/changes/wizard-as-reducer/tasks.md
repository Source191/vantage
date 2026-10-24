# Tasks

## 1. Reducer

- [x] 1.1 `WizardViewShould.OpenWithRecentRepositoriesFirstAndTheFirstChoiceSelected`, `NarrowChoicesAsTheFilterChanges`, `MoveAndPageTheListWithinBounds`: implement `WizardState.Open`, `FilterChanged`, `ListMoved`, `ListPaged`
- [x] 1.2 `WizardViewShould.ConfirmTheRepositoryAndMoveFocusToName`, `RefuseToConfirmWithNoChoice`: implement `ConfirmRepository` with the problem text and focus
- [x] 1.3 `WizardViewShould.PreviewFollowsNameModeAndSelection`: implement the preview recomputation using `LaunchPreview.Describe`
- [x] 1.4 `WizardViewShould.NextFieldMovesNameToDescriptionToMode`: implement `NextField`
- [x] 1.5 `WizardViewShould.StartRefusesNoRepositoryBlankNameAndPlanProblemsWithTheRightFocus`, `StartYieldsALaunchEffect`, `StartAndOpenInTabYieldsALaunchEffectWithOpenInTab`, `CancelYieldsClose`: implement `Start` and `Cancel` through `LaunchPlanBuilder`
- [x] 1.6 `WizardViewShould.FixedDirectoryHidesTheChooserAndWorktreeControls`, `FixedDirectoryStartsInThatFolder`: implement the fixed-directory variant
- [x] 1.7 `WizardViewShould.ArrowsInTheFilterDriveTheListAndLettersInTheListReturnToTheFilter`: implement the focus switching inputs
- [x] 1.8 `WizardViewShould.BeIdempotentForARepeatedInput`: verify applying the same input twice yields the same state

## 2. Binder

- [ ] 2.1 Rewrite `LaunchWizardDialog` as controls plus `Apply(input)` plus `Push(state)` with the `binding` guard, removing every local function that decides anything; verify `dotnet build` and `dotnet format --verify-no-changes` pass and the file is under 120 lines
- [ ] 2.2 Run `dotnet run --project tools\HeadlessTerminal -- check` and a scripted `drive` through the wizard (filter, arrows, Enter, name, F5) capturing to `drive-raw.txt`; verify the capture matches a capture taken before this change

## 3. Documentation

- [ ] 3.1 Update CLAUDE.md (the wizard is a reducer; dialogs are binders) and remove the sentence in `docs\architecture-review.md` marking this as a later item, or delete that document now that every item has an archived change; verify `openspec validate` passes for every open change

## Workflow follow-up

- PR 19a: task group 1 (`WizardState`, `WizardInput`, `WizardView.Reduce` and `WizardViewShould`). New files in Core only, depending on `LaunchPlanBuilder`, `LaunchPreview` and `IFolders`, so it can be developed alongside any PR from 8 to 18 and merged as soon as it is green.
- PR 19b: task groups 2 and 3 (the binder and documentation). Needs `consolidate-helpers` PR 18 (the dialog's dependencies) and `outcomes-and-wording` PR 13 (its sentences). Archive the change in this PR. Bump `Version` to the next patch after `main`'s.
- PR 19a notes (version 0.4.8): the inputs follow the design with three adjustments the binder needs. `ListMoved(delta)` covers paging too, because the dialog knows the page size and passes it as the delta, so there is no separate `ListPaged`. `ChoiceSelected(index)` carries the list's own selection changes (its own arrow keys, a click), and `FieldFocused(field)` follows focus moved by Tab or a click, so pushing `state.Focus` never pulls focus back. The problem is a `WizardProblem` fact (`NoRepository`, `NoName`, `Refused(reason)`) that `Wording.Describe` says, as footers are, rather than a string; the title, the chosen caption and name and the hint are `Wording` members too. The repositories are read once when the wizard opens and filtered by `RepositoryChoice.Matches`, the rule `RepositoryCatalogue` now uses as well, so `Choices`, `Chosen` and `Preview` are computed properties and the same input twice gives an equal state. A repeated `FilterChanged` with the same text keeps the selection, so an echo from the binder cannot reset it. In the fixed-directory variant Enter in the description stays there: the Where choice it would move to is hidden, so there is nowhere to go. PR 19b's drive confirms that the dialog on `main` behaves the same.
