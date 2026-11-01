# Proposal

## Why

The code review of 0.20.0-preview.7 (section 3.3 and V-10 part 2) found that the three view states carry the same five fields (`Rows`, a selected key, `Filter`, `FilterEditing`, `Loaded`) only to rebuild a `ListSelection<TRow>` from them on every access through `Policy.List(...)`, and to copy them back through an identical `With(ListSelection)` on every key. The copy is the one place a reducer can forget a field, the selected key has three names (`SelectedSessionId`, `SelectedPath`, `SelectedKey`), and `IListViewState` exposes `Rows` and `Loaded` that its own `List` already answers. Holding the `ListSelection` directly is the largest deletion the review offered, and it was asked for as its own change.

## What Changes

- **Each view state holds its list:** `SessionsViewState`, `WorktreesViewState` and `ConfigViewState` become `(ListSelection<TRow> List, LoadBanner Banner, StatusMessage Message)` (Sessions keeps its `AutomatedRuns` init property). The five list fields, the three computed `List` properties and the three `With(ListSelection)` methods go; a reducer writes `state with { List = state.List.Load(rows) }` and reads `state.List.FilterEditing` and `state.List.Filter`.
- **Pass-throughs kept:** `Selected`, `VisibleRows` (Sessions and Worktrees) and `Title` stay as one-line reads over `List`, so the reducers, `Wording` and the facts that read them do not change, and `NullFreeModelsShould`'s three kept `ViewState.Selected` entries stay true.
- **`SelectionKey` goes:** with no state field to hold it, `SelectionKey`, `ListSelection.Selection` and `ListPolicy.List(...)` have no caller left and are deleted with their two `ListSelectionShould` facts; `ListSelection.SelectedKey` stays `string?` on `NullFreeModelsShould`'s kept list, as the review said.
- **`IListViewState<TRow>` narrows** to `List`, `Banner`, `Message` and `Title`: `Rows` and `Loaded` are read from `List`, and `Shown`, which is `=> Message` in all three states (V-12), is replaced by `Message`.
- **Tui reads follow:** `TableBody` reads `State.List.Loaded` and `State.Message`, `ConsoleScreen.ReadConfig` reads `config.State.List.Loaded`, the three definitions' `Counts` read `state.List.Rows`, and `SessionsScreenDefinition.OnInput` hands `before.List.Rows` to the notifier and the activity (the `loaded.Rows` beside it is the `RowsLoaded` input's and stays).
- **No cached `Visible`:** `ListSelection.Visible` stays a computed property; design.md says why.
- **Test fixtures:** `WorktreesViewShould`'s nine `with { SelectedPath = new SelectionKey.Row(...) }` sites become an `At(row)` helper over `List.Select`, as `ConfigViewShould.At` already is; `ConfigViewShould.At` and `WordingOfKeyBarShould`'s three `.With(...)` calls become `with { List = ... }`.
- **Version and changelog:** one preview step above `main` (0.20.0-preview.14 if nothing lands first), because files under `src\` change; a refactor, so patch-level under semantic versioning. The same pull request adds one line under Unreleased in `CHANGELOG.md` (under Internal), which `Assert-ChangelogUpdated.ps1` requires since PR #180.
- `skip_specs: true`: no key, screen, title, message or state file behaves differently; the existing reducer facts pin it.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with no behaviour change, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** `src\Source191.Vantage.Core\Application\Views\SessionsViewState.cs`, `WorktreesViewState.cs`, `ConfigViewState.cs`, `IListViewState.cs`, `ListSelection.cs`, `ListPolicy.cs`, `SelectionKey.cs` (deleted); the reducers `SessionsView.cs`, `WorktreesView.cs`, `ConfigView.cs`; `Application\WordingOfConsole.cs:98-99` and `Views\ConsoleView.cs:36` (`.Rows` reads); in Tui `TableBody.cs:139, 253`, `ConsoleScreen.cs:291`, `SessionsScreenDefinition.cs:82-83`, and the `Counts` line of the three screen definitions (`SessionsScreenDefinition.cs:65`, `WorktreesScreenDefinition.cs:58`, `ConfigScreenDefinition.cs:54`). No port, adapter, handler, effect or `Wording` text changes. Line numbers are at `main` 8dbf4150 (0.20.0-preview.13).
- **Tests:** `ListSelectionShould`, `SessionsViewShould`, `WorktreesViewShould`, `ConfigViewShould`, `ConsoleViewShould`, `WordingOfKeyBarShould` and `NullFreeModelsShould` keep every step green; two `ListSelectionShould` facts about `SelectionKey` are removed with the type.
- **Other open changes:** `sessions-row-action` (the generic `ViewUpdate<TState>` and Sessions' `RowAction`) edits the same three reducers and `ViewUpdate.cs`, and `table-body-collaborators` moves the `TableBody` lines this change touches; the three are independent, and whichever lands second rebases.
- **Docs:** `.claude\rules\console-screen.md:41` says the status line has rows "while `State.Shown` has words" and reads `State.Message` once `Shown` goes; `CHANGELOG.md` gains its Internal line.
- **Users:** none.

## Out of scope

- The generic `ViewUpdate<TState>`, Sessions' `RowAction` and `SharedKeys` (V-10 parts 1, 3 and 4): the `sessions-row-action` change.
- Slimming `IScreenDefinition` (V-08) or extracting `TableBody`'s collaborators (V-09).
- Caching `ListSelection.Visible` or moving `TableBody.selected`.
- Multiple Claude accounts, which is a later change.
