# Proposal

## Why

The code review of 0.20.0-preview.7 (1 November 2026, sections 3.2, 3.3, 3.13 and item 2 of section 4; V-10, V-11 and V-12 in the appendix) found the three tab reducers carrying the same shape three times over: three update records differing only by state type (`ViewUpdate`, the Sessions-specific one with the general name, `WorktreesViewUpdate`, `ConfigViewUpdate`), `Unchanged` and `Effect` declared in each view, the four console keys (`q`, `?`, `U`, `!`) as four arms in each view, and a tuple adaptor in each screen definition's `Reduce`. `SessionsView` predates `RowAction`, so it writes "no row selected" six times (`src\Source191.Vantage.Core\Application\Views\SessionsView.cs:79, 111, 127, 136, 148, 204`) where `WorktreesView` and `ConfigView` funnel every row key through one `RowAction(state, Func<TRow, RowDecision>)`, and its `Rename` borrows `SessionAction` with a lambda that can never run (`SessionsView.cs:168-171`). None of it changes what a key does, so it is the review's largest safe deletion.

## What Changes

- **One update record:** `ViewUpdate<TState>(TState State, IReadOnlyList<ViewEffect> Effects)` with static `Unchanged(state)` and `Effect(state, effect)` replaces `ViewUpdate`, `WorktreesViewUpdate`, `ConfigViewUpdate` and the six private helpers; `IScreenDefinition.Reduce` returns it and the three definitions' tuple adaptors become one-line delegations.
- **`RowAction` on Sessions:** `SessionsView` gets the same `RowAction(state, Func<SessionRow, RowDecision>)` as the other two views, so "no session selected" is said once; Enter (`Opening`, already a `RowDecision`), `x`, `d`, `b`, `r`, `p`, `m`, `c`, `f` and `s` become `Func<SessionRow, RowDecision>` arms, `SessionAction` and `FolderAction` go, and `Rename` answers an interactive row itself, so its dead lambda goes. The two `d` arms become one `Delete(row)` decision, which also settles V-12's ordering nit.
- **`SharedKeys`:** `SharedKeys.TryFor(key, out effect)` beside `TabKeys` answers `Quit`, `ShowHelp`, `UpdateVantage` and `ShowProblemReport` for `q`, `?`, `U` and `!`, one guard arm per view in place of four.
- **Guards:** no `Reduce` guards its `state` (today only `ConfigView.Reduce` does, `ConfigView.cs:12`); two of three already match, and the rule for primary constructors in CLAUDE.md says nothing about reducers. If `convention-nets` (A-08) lands first with the opposite rule, the guard is added to the other two instead.
- **`Shown` goes:** `IListViewState.Shown` and the three states' `Shown => Message` go in favour of `Message`, which `TableBody.Status` reads; the four stale "standing message" comments V-12 listed were already corrected in PR #174 (`ae2297a1`, `d84dd016`), so nothing is left to reword.
- **Readability:** `WorktreesView.InRealWorktree` (`WorktreesView.cs:117`) parenthesises its `is ... or ...` before `&& row.FolderExists`; the double blank line at `SessionsViewState.cs:34-35` and the missing one at `SessionsView.cs:130-131` go.
- **Version:** the next preview above `main` (0.20.0-preview.9 if nothing lands first), because files under `src\` change; a refactor, so patch-level under semantic versioning.
- `skip_specs: true`: every key keeps its behaviour and the existing reducer facts pin it; no screen, message, state file or command line behaves differently.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with tests, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** `src\Source191.Vantage.Core\Application\Views\` (`ViewUpdate.cs` becomes generic; `WorktreesViewUpdate.cs` and `ConfigViewUpdate.cs` deleted; new `SharedKeys.cs`; `SessionsView.cs`, `WorktreesView.cs`, `ConfigView.cs`, `SessionsViewState.cs`, `WorktreesViewState.cs`, `ConfigViewState.cs`, `IListViewState.cs`); `src\Source191.Vantage.Tui\` (`IScreenDefinition.cs`, the three screen definitions, `TableBody.cs`). No port, adapter, effect, handler or `Wording` change.
- **Tests:** `SessionsViewShould`, `WorktreesViewShould` and `ConfigViewShould` change only the declared type of their `update` locals and `.Shown` to `.Message`; new `SharedKeysShould` beside `TabKeysShould`; a few facts pinning what no fact states today (`x` and `r` on an interactive row, the row keys on an empty table) are added before the move.
- **Other open changes:** `list-selection-in-state` (V-10 part 2, holding `ListSelection<TRow>` directly in the states) is a separate change; the two are independent but edit the same three states and reducers, so whichever lands second rebases. `standardise-maybe-null-when` changes `TabKeys.TryFor`'s `out` attribute; `SharedKeys.TryFor` copies whichever shape is on `main` when it is written. `convention-nets` owns the guard rule (A-08) and the remaining guessing discard arm in `WorktreesView.PullRequestOf` (V-03); the Sessions arms this change rewrites follow the two-case rule as they are rewritten, so V-03 has nothing left in `SessionsView`. `table-body-collaborators` (V-08) slims `IScreenDefinition`, of which this change alters one member's return type.
- **Users:** none.

## Out of scope

- Holding `ListSelection<TRow>` directly in the view states and deleting the three `With` methods (V-10 part 2): the change `list-selection-in-state`.
- A key table or polymorphic effects in place of the key switches: V-11 keeps the switches, and so does this change.
- The `KeyHelp`-to-reducer-arm fact (V-06) and the guard rule itself (A-08): `convention-nets`.
- Multiple Claude accounts, which is a later change.
