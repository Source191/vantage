# Tasks

## 1. ListSelection in Core

- [ ] 1.1 `ListSelectionShould.SelectTheFirstVisibleRowOnFirstLoad`, `KeepTheSelectedKeyAcrossAReload`, `MoveToTheNearestRowWhenTheSelectionDisappears`, `BeLoadedOnlyAfterTheFirstLoad`: write against a `ListSelection<string>` with identity key and alphabetical order; implement `Load`
- [ ] 1.2 `ListSelectionShould.MoveWithinBounds` and `IgnoreMovesOnAnEmptyList`: implement `Move`
- [ ] 1.3 `ListSelectionShould.FilterCaseInsensitivelyAndReselect`, `KeepTheFilterOnEnter`, `ClearTheFilterOnEscape`, `BackspaceShortensTheFilter`: implement `EditFilter` and `Reselect`
- [ ] 1.4 `ListSelectionShould.TitleNamesTheViewFilterAndCount`: implement `Title`

## 2. Compose it into the states

- [ ] 2.1 `SessionsViewState` holds a `ListSelection<SessionRow>` and forwards `Rows`, `SelectedSessionId`, `Filter`, `FilterEditing`, `VisibleRows`, `Selected`, `Title`; delete `Load`, `EditFilter`, `MoveSelection`, `Reselect`, `IndexOfSelected` from `SessionsView`; verify `SessionsViewShould` passes unchanged
- [ ] 2.2 The same for `WorktreesViewState` and `WorktreesView`, keeping the footer-clearing rule on move; verify `WorktreesViewShould` passes unchanged
- [ ] 2.3 Verify with `dotnet build` that `Unchanged` and `Effect` helpers exist once, in a shared `Reduced` helper or on `ListSelection`

## 3. TableScreen in Tui

- [ ] 3.1 Add `IScreenDefinition<TState, TRow>`, `EffectContext<TState>` and `TableScreen<TState, TRow>` by lifting the shared code from `SessionsScreen` verbatim (lifetime, timer, key mapping, `RunInBackground` with the busy spinner, the waiting indicator, window title, layout, redraw, row colour); verify `dotnet build` passes with both old screens still in place
- [ ] 3.2 Add `TableSource<TRow>` and `Durations.Age`; `DurationsShould.FormatAges` carries the three cases from the `Age` helper; delete `SessionTableSource` and `WorktreeTableSource`
- [ ] 3.3 `SessionsScreenDefinition` reproduces the sessions layout, columns, colours, help text and the `StateTransitionNotifier` hook; `SessionsScreen` becomes a thin factory over `TableScreen`; verify `dotnet run --project tools\HeadlessTerminal -- check` passes and a manual pass through the README's sessions key table shows no difference
- [ ] 3.4 `WorktreesScreenDefinition` the same for worktrees; delete both `Live` classes; verify the hand-off check and a manual pass through the worktrees key table
- [ ] 3.5 Verify `SessionsScreen.cs` and `WorktreesScreen.cs` together are under 300 lines and that `dotnet format --verify-no-changes` passes

## 4. Documentation

- [ ] 4.1 Update CLAUDE.md's description of the Tui layer (definitions, `TableScreen`, where a new view starts) and the Terminal.Gui facts if any moved; verify the text matches the code

## Workflow follow-up

- PR 8: task groups 1 and 2 (`ListSelection` in Core, both reducers composed over it). Proven by the existing reducer tests passing unchanged. `wizard-as-reducer` PR 19a (new Core files only) can run alongside this and every later PR up to 18.
- PR 9: task groups 3 and 4 (`TableScreen`, the two definitions, `Live` deleted). Accepted by the hand-off check and a manual pass through the README key tables. Archive the change in this PR.
