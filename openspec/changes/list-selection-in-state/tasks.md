# Tasks

Every task is a refactor kept green by the facts it names, with a commit per step; no behaviour changes, so no new fact is written except where a fixture helper replaces a removed member. Verify each task with `dotnet build -c Debug` (warnings are errors) and `dotnet test --project test\Source191.Vantage.Core.Tests -c Debug --no-build` unless it says otherwise.

## 1. The Config state

- [ ] 1.1 `ConfigViewState` becomes `(ListSelection<ConfigRow> List, LoadBanner Banner, StatusMessage Message)`: move `Policy` above `Empty`, make `Empty` `new(Policy.EmptyList(), new LoadBanner.None(), StatusMessage.None)`, delete the five list fields, the computed `List` and `With`, keep `Selected` and `Title` as reads over `List`; `ConfigView` writes `state with { List = state.List.X(...) }` and reads `state.List.FilterEditing` and `state.List.Filter.Text`; `ConfigViewShould.At` becomes `Loaded with { List = Loaded.List.Select(key) }` and its `.Rows`, `.Filter.Text` reads go through `List`; `WordingOfKeyBarShould` lines 94 and 129 likewise; `ConfigScreenDefinition.Counts` reads `state.List.Rows`. Verify `ConfigViewShould`, `WordingOfKeyBarShould` and `NullFreeModelsShould` pass.

## 2. The Worktrees state

- [ ] 2.1 `WorktreesViewState` the same way, keeping `Selected`, `VisibleRows` and `Title`; `WorktreesView` reads and writes through `List`; `WorktreesScreenDefinition.Counts` reads `state.List.Rows`. Add `private static WorktreesViewState At(WorktreeRow row) => Loaded with { List = Loaded.List.Select(row.Path.Key) }` to `WorktreesViewShould` and use it at the nine `SelectedPath` sites (lines 124, 125, 129, 285, 303, 304, 314, and the two `Elsewhere` fixtures at 208 and 217, which select on their own reduced state); its `.Loaded`, `.FilterEditing` reads go through `List`. Verify `WorktreesViewShould`, `WordingOfKeyBarShould` and `NullFreeModelsShould` pass.

## 3. The Sessions state

- [ ] 3.1 `SessionsViewState` the same way, keeping `AutomatedRuns` as its init property and `Selected`, `VisibleRows` and `Title`; `SessionsView` reads and writes through `List` (`CloseEmpty` reads `state.List.Rows`); `WordingOfConsole.SessionsBar` reads `state.List.Rows`; `ConsoleView.OpenSession` reads `state.Sessions.List.Rows`; `SessionsScreenDefinition.Counts` reads `state.List.Rows`; `SessionsViewShould`'s `.Loaded`, `.FilterEditing`, `.Filter.Text` reads and `WordingOfKeyBarShould` line 140 go through `List`. Verify `SessionsViewShould`, `ConsoleViewShould`, `WordingOfKeyBarShould` and `NullFreeModelsShould` pass.

## 4. The list component and the interface

- [ ] 4.1 Delete `SelectionKey.cs`, `ListSelection.Selection` and `ListPolicy.List(...)`, and the two `ListSelectionShould` facts that test them (`AnswerItsKeyAsASelectionKey`, `BuildASelectionFromASelectionKey`); `ListPolicy.EmptyList()` stays. Verify `ListSelectionShould`, `NullFreeModelsShould` (`KeepNoStaleEntry` still finds `ListSelection.SelectedKey` and `Selected`), `SealedTypesShould` and `OneTypePerFileShould` pass.
- [ ] 4.2 `IListViewState<TRow>` becomes `List`, `Banner`, `Message`, `Title`, its summary saying so; the three states drop `Shown` and its doc comments; `TableBody.Status` reads `State.Message`, `TableBody.Redraw` reads `State.List.Loaded`, `ConsoleScreen.ReadConfig` reads `config.State.List.Loaded`; the `.Shown` reads in `SessionsViewShould`, `WorktreesViewShould` and `ConfigViewShould` read `.Message`. Verify the three view fact files pass and `dotnet build -c Debug` is clean at the solution (Tui included).

## 5. Version and docs

- [ ] 5.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one preview step above `main` when implemented (0.20.0-preview.9 if nothing lands first); patch-level under semantic versioning. Verify `.github\scripts\Assert-VersionBumped.ps1` passes locally against `main`.
- [ ] 5.2 `.claude\rules\console-screen.md` and `CLAUDE.md`'s reducer paragraph still read true (both say "composed over one `ListSelection<TRow>`", which is now literal); amend only if a sentence names a removed member. Verify by reading both against design.md.

## 6. Whole-solution check

- [ ] 6.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate list-selection-in-state --strict` all pass. Record the totals here.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
- If `sessions-row-action` or `table-body-collaborators` lands first, rebase this change's reducer and `TableBody` edits onto it before opening the pull request.
- Push and open the implementation pull request only when the owner asks.
