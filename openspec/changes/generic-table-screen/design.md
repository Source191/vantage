# Design

## Context

See proposal.md. The duplication is in `Screens\Tui\SessionsScreen.cs:230` and `WorktreesScreen.cs:209` (`RunInBackground<T>`), the two `Live` nested classes including their `ShowBusy` and `WaitingIndicator` members, and the private helpers in `SessionsView.cs` and `WorktreesView.cs`. The reducers are pure and fully tested, so they are the safety net for this change.

## Goals / Non-Goals

**Goals:** one implementation of selection, filtering, refresh and background work; a third view costs a reducer, a definition and its handlers; the existing reducer tests pass unchanged.

**Non-Goals:** a general widget library, inheritance between the two reducers, changing how effects are dispatched (next change).

## Decisions

- **Composition over inheritance for state.** `ListSelection<TRow>(IReadOnlyList<TRow> Rows, string? SelectedKey, RowFilter Filter, bool FilterEditing, bool Loaded)` is a record with pure methods `Load(rows)`, `Move(delta)`, `EditFilter(key)`, `Reselect()`, `VisibleRows`, `Selected`, `Title(name)`, taking a `Func<TRow, string> keyOf`, a `Func<TRow, IReadOnlyList<string?>> filterFields` and a `Func<IEnumerable<TRow>, IEnumerable<TRow>> order` supplied by the owning state. `SessionsViewState` and `WorktreesViewState` become `(ListSelection<SessionRow> List, string? Banner, string Footer)` with the same public properties as today forwarded, so the reducer tests do not change. Alternative rejected: a generic base record; records with inheritance lose `with` ergonomics and the two states differ in their footer rules.
- **The reducers keep their shape.** `SessionsView.Reduce` and `WorktreesView.Reduce` stay static and keep their switch on inputs; they delegate the shared cases (`RowsLoaded`, filter keys, Up and Down) to `ListSelection` and keep the view-specific cases. The view-specific footer behaviours (worktrees clears the footer on move, sessions does not) live in the reducer, not the component.
- **`TableScreen<TState, TRow>`** is a class in `Tui` with `Run(TState initial) : ScreenExit<TState>`. It takes `IScreenDefinition<TState, TRow>` and `Func<CancellationToken, Task<ViewInput>> load`. The screen always titles its window "Vantage" (Terminal.Gui writes it as the terminal title) and owns one busy spinner beside the footer and one waiting indicator over the table, shown while `Loaded` is false and no banner is set. The definition supplies: `string Crumb`, `string WaitingText` ("Listing sessions" or "Scanning worktrees"), `TimeSpan RefreshEvery`, `string[] ColumnNames`, `Func<TRow, int, DateTimeOffset, string> Cell`, `Func<TRow, bool, Scheme> RowScheme`, `string HeaderNames`, `string HeaderKeys`, `string HeaderKeyNames`, `string HelpText`, `Func<TState, string> Summary`, `Func<TState, string> ThirdLine`, `Func<TState, string> FooterText`, `Func<TState, string?> Banner`, `Func<TState, string> Title`, `Func<TState, ViewInput, (TState, IReadOnlyList<ViewEffect>)> Reduce`, `Func<TState, IReadOnlyList<TRow>> VisibleRows`, `Func<TRow, string> KeyOf`, `Func<TState, string?> SelectedKey`, `Func<ViewEffect, bool> LeavesScreen`, and `Action<EffectContext<TState>, ViewEffect> Handle` for in-screen effects. The `EffectContext` exposes `RunInBackground`, `Confirm`, `SetFooter`, `Exit` and the `IApplication` for dialogs. Alternative rejected: an abstract `TableScreen` base class with virtuals; the definition-as-object keeps the Tui layer free of per-view subclasses and reads as data.
- **One `TableSource<TRow>`** replaces `SessionTableSource` and `WorktreeTableSource`, taking the column names and cell function from the definition; `Age` moves to a small `Durations` helper in `Tui`.
- **`StateTransitionNotifier`** stays sessions-specific: the definition gets an optional `Action<TState, TState> OnRowsLoaded` hook and only the sessions definition supplies one.
- **Lifetime unchanged.** Each `Run` still creates and disposes its own `IApplication`, because the screen loop alternates two screens and hands the terminal to children between runs.

## Risks / Trade-offs

- [A definition with twenty members is its own kind of sprawl] → it is data, grouped into three small records (`Layout`, `Columns`, `Behaviour`) if it reads badly; the alternative is two 400-line classes.
- [The `Live` layout constants (column 62, 72, 90, 98) become shared and a mistake affects both views] → one place to fix, and the `HeadlessTerminal` `grid` command shows both layouts in CI.
- [Reducer tests are the safety net but cover no Terminal.Gui code] → the hand-off check (`HeadlessTerminal -- check`) and a manual pass through the README's key tables are the acceptance for the Tui half.
