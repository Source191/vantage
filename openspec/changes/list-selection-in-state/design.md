# Design

## Context

See proposal.md for why. At `main` 8dbf4150 (0.20.0-preview.13; the five changes landed since ac7cdae0, PRs #175 and #177 to #180, touched none of the eight files in `Application\Views` this change edits, and only moved the `ConsoleScreen` line it cites) each view state is a positional record of seven members: `Rows`, a `SelectionKey` (named `SelectedSessionId`, `SelectedPath` or `SelectedKey`), `Filter`, `FilterEditing`, `Banner`, `Message` and `Loaded`, with a static `ListPolicy<TRow> Policy` and a computed `List => Policy.List(Rows, Selected..., Filter, FilterEditing, Loaded)` that builds a fresh `ListSelection<TRow>` on every read (`SessionsViewState.cs:29`, `WorktreesViewState.cs:26`, `ConfigViewState.cs:24`). Every reducer arm that moves, filters or loads calls `state.With(state.List.X(...))`, which copies the five fields back (`SessionsViewState.cs:42`, `WorktreesViewState.cs:33`, `ConfigViewState.cs:44`). `SelectionKey` exists only so a state can say "nothing selected" without a null; `ListSelection` itself holds `string? SelectedKey`, allowed by `NullFreeModelsShould.Kept` (`"nothing is selected in an empty list"`), and answers it back as a `SelectionKey` through `Selection`. `IListViewState<TRow>` (`Rows`, `List`, `Banner`, `Shown`, `Loaded`, `Title`) is what `TableBody<TState, TRow>` and `IScreenDefinition` constrain on; `Shown` is `=> Message` in all three states. The view facts build states only through `Empty` and `Reduce`, except `WorktreesViewShould`, which sets `SelectedPath` by hand nine times, and `ConfigViewShould.At` and `WordingOfKeyBarShould`, which call `With(List.Select(key))`.

## Goals / Non-Goals

**Goals:**
- One home for the list facts in each state, so nothing is copied on a key and nothing is rebuilt on a read.
- The same reducer facts pass unchanged except where they name a removed member.
- `IListViewState` says only what a table screen reads and `List` cannot answer.

**Non-Goals:**
- Changing what any key, title, count, filter or message does.
- The generic update record, Sessions' `RowAction`, `SharedKeys`, `IScreenDefinition` or `TableBody`'s shape.
- Any new nullable: the kept entries for `ListSelection.SelectedKey`, `ListSelection.Selected` and the three `ViewState.Selected` stay as they are.

## Decisions

**The state record is `(ListSelection<TRow> List, LoadBanner Banner, StatusMessage Message)`.** `Empty` becomes `new(Policy.EmptyList(), new LoadBanner.None(), StatusMessage.None)`; Sessions keeps `AutomatedRuns` as an init property. The alternative, keeping `Rows` and `Loaded` as positional fields beside `List`, would leave two homes for the same facts, which is the defect being removed. Static field initialisers run in textual order, and today `Policy` is declared below `Empty` in all three files, so `Policy` moves above `Empty`; left where it is, `Empty` would read a null `Policy` and every fact in the file would fail with a `TypeInitializationException`.

**`Selected`, `VisibleRows` and `Title` stay as pass-throughs; `Rows`, `Loaded`, `Filter` and `FilterEditing` do not.** `Selected` is read on nearly every key in the three reducers and in `Wording.SessionsBar`, and its three kept entries in `NullFreeModelsShould` hold while it exists (`KeepNoStaleEntry` would fail if it went). `VisibleRows` and `Title` are read by `ResumeAll` and by a dozen facts. The other four are read in a handful of places each, and `TableBody` already reads `State.List.FilterEditing` and `State.List.Filter.Text`, so callers read `state.List.Rows`, `state.List.Loaded` and so on. The alternative, dropping every pass-through for `state.List.X` everywhere, is consistent but touches about forty more lines and deletes three kept entries for no gain; the opposite, keeping all seven, keeps the interface the review asked to narrow.

**`SelectionKey`, `ListSelection.Selection` and `ListPolicy.List(...)` are deleted.** Their only callers are the three `With` methods and the computed `List` properties this change removes, plus two `ListSelectionShould` facts (`AnswerItsKeyAsASelectionKey`, `BuildASelectionFromASelectionKey`) that test the conversion itself. `ListPolicy.EmptyList()` stays as the one way to start a list. The alternative, keeping `SelectionKey` as the public face of "which row", would keep a type whose one job (a non-null stand-in for the state's key field) no longer exists.

**`IListViewState<TRow>` becomes `List`, `Banner`, `Message`, `Title`.** `Rows` and `Loaded` are `List`'s; `Shown` is replaced by `Message`, as V-12 asked for when V-10 reshaped the states, and its three stale doc comments ("the last message, else...") go with it. `TableBody.Status` (`TableBody.cs:139`) reads `State.Message`, and the one sentence that names the member, `.claude\rules\console-screen.md:41` ("while `State.Shown` has words"), says `State.Message`. The alternative, keeping `Shown` for a future standing message, keeps a member `console-screen.md` says no tab has.

**No cached `Visible` inside `ListSelection`.** `Visible` is read a few times per redraw (`Redraw`, `Selected`, `Title`, `Choosable`) over three to ten rows, which the review measured as a few milliseconds and the owner's 29 October decision not to optimise memory covers. A cache inside a positional record would also be wrong by construction: `with` copies every field, including the cache, and then sets the changed members without running the constructor, so a cached `Visible` would survive `with { Rows = ... }` stale. The one hot path, the per-cell colour getters during draw, already has `TableBody.selected` as its cache, which `table-body-collaborators` moves into `ColumnSizing`.

**One view at a time, Config first.** Config has the fewest reads and the smallest fact file, Worktrees has the hand-built fixtures, Sessions has `AutomatedRuns`. The three states are independent until the interface changes, so each lands green on its own, and the interface and Tui reads change last, when all three states have the new shape.

## Risks / Trade-offs

- [`ListSelection.Select` refuses a key that is not listed or not selectable, where the old `with { SelectedPath = ... }` set it blindly] → every key the `WorktreesViewShould` fixtures set is a listed, selectable row, so the `At(row)` helper selects the same row; a fact that wanted an unlisted key would now need to say so.
- [The Tui edits (`TableBody.cs:139, 253`, `ConsoleScreen.cs:291`, `SessionsScreenDefinition.cs:82-83`, three `Counts` lines) have no unit test under them] → each is a member rename the compiler checks; the smoke check (CI's `check` job) and the hand-off check cover the redraw, the Config tab's first load and a listing's arrival.
- [`sessions-row-action` edits the same three reducers and `table-body-collaborators` the same `TableBody` lines] → the changes are independent; whichever lands second rebases, and this one's diff in the reducers is mechanical (`state.With(x)` to `state with { List = x }`).
- [Deleting `SelectionKey` is a public type going from `Application.Views`] → nothing outside Core and its tests names it (checked by grep over `src`, `test` and `tools` at `main` 8dbf4150); `SealedTypesShould` and `NullFreeModelsShould` stay green because nothing new is added, and the kept entries this change relies on are `NullFreeModelsShould.cs:57-62`.

## Migration Plan

Nothing to migrate: no state file, key, setting or command line changes. Rollback is reverting the pull request.
