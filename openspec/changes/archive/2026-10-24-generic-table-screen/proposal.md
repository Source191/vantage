# Proposal

## Why

`SessionsScreen` (424 lines) and `WorktreesScreen` (396 lines) duplicate `Run`, `TriggerLoad`, `OnKey`, `RunInBackground<T>`, `Live.Build`, `Redraw`, `ShowBusy`, the waiting indicator, `RowColour` and `Text`, roughly 350 lines between them, and the two reducers duplicate `Load`, `EditFilter`, `MoveSelection`, `Reselect`, `IndexOfSelected`, `Unchanged` and `Effect` while their state records repeat `Filter`, `FilterEditing`, `Banner`, `Footer`, `Loaded` and `Title`. Every fix to selection, filtering or refresh has to be made twice (the spinner change on `main` was), and the roadmap's next view would be a third copy.

## What Changes

- A `ListSelection<TRow>` component in `Application` holds the filter text, filter-editing flag, selected key, the loaded flag and the load, move, reselect and nearest-row logic, parameterised by a key selector and an ordering. Both view states compose it.
- A `TableScreen<TState, TRow>` in `Tui` owns the application lifetime, the refresh timer, key mapping, background work with footer updates and the busy spinner, the waiting indicator until the first load, the "Vantage" window title, and the shared header, table, crumb and footer layout. It is driven by an `IScreenDefinition<TState, TRow>` giving the columns, row colour, header hints, help text, crumb label, refresh interval and which effects leave the screen.
- `SessionsScreen` and `WorktreesScreen` become a definition each plus the handlers for their own effects; the two `Live` classes and the duplicated reducer plumbing are deleted.
- The two reducers keep their public `Reduce` signatures and their tests.

Out of scope: changing any key, column, colour or footer; effect handler registration (`effect-handlers`); the wizard; multiple Claude accounts. Depends on `split-into-projects`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Structure only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `Core`: new `Application\Views\ListSelection.cs`; `SessionsViewState`, `WorktreesViewState`, `SessionsView`, `WorktreesView` shrink.
- `Tui`: new `TableScreen.cs`, `IScreenDefinition.cs`, `SessionsScreenDefinition.cs`, `WorktreesScreenDefinition.cs`; the two screen files lose their duplicated halves.
- Tests: `ListSelectionShould` is new; `SessionsViewShould` and `WorktreesViewShould` keep passing unchanged, which is the proof that behaviour is intact.
