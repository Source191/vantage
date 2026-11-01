# Proposal

## Why

The code review of 1 November 2026 (0.20.0-preview.7, section 3.4 and item 4 of section 4) found `src\Source191.Vantage.Tui\TableBody.cs` doing four jobs in one class (586 lines and twenty-one instance fields at `main` 8dbf4150, unchanged since the review): it runs the reducer loop and routes effects, sizes the columns, shows the header and row tooltips, and owns the detail pane's request lifecycle (V-09); and `IScreenDefinition` asks seventeen members of every view, several of them faked or empty on two of its three implementers (V-08: the Config tab answers a `RefreshEvery` nothing reads (`ConfigScreenDefinition.cs:14-15`), `TabTitle(TState)` ignores its argument everywhere and is re-set on every redraw (`TableBody.cs:246`)). Four smaller findings sit in the same files: `KeyMapping.TryFrom` and a middle-ellipsis written twice can use what .NET and Terminal.Gui already provide (V-13), labels the console removes are never disposed and keep their tooltips registered (C-07), and `KeyMapping` and the two table definitions have no facts though `ConfigScreenDefinitionShould` shows the shape (T-08). Now, because `columns-as-data` and `wording-one-home` are about to reshape the same two files, and the collaborators are best cut once those have landed.

## What Changes

- **`ColumnSizing<TRow>`:** a new Tui class takes the table and the definition and owns the column styles (`StyleColumns`), the elastic width (`ElasticWidth`, `WidestCell`), the column offset and `<`/`>` scrolling (`Scroll`), and the `selected` cache that the per-cell colour getters (`CellColour`, `RowColour`) read, set once per redraw.
- **`TableToolTips<TRow>`:** a new Tui class owns the zero-width anchor view, the `PointerOver` record and the header and row tooltips (`Show`, `Hide`, `PointerAt`), wired to the table's mouse events.
- **`DetailReader<TRow>`:** a new Tui class owns the pane, its `DetailRequests`, the key last asked for (`askAgain` included), the lifetime token and `AskFor`, `Read` and `Show`; `TableBody` keeps `State`, the reducer loop, effect routing and `IEffectContext`.
- **`IScreenDefinition` slimmed (V-08):** a `ConsoleTab Tab` property replaces `TabTitle(TState)` and the tab's title is set once from `Wording.TabTitleFor`; `RefreshEvery` becomes a closed `TabRefresh` (`Never | Every(TimeSpan)`) so the Config tab says what is true, and a new `Frame.Refresh(ITabBody, Action)` in `ConsoleScreen` switches over it, adding a timer through the `Every(TimeSpan, Action)` that PR #179 reshaped for the UI-thread heartbeat, which keeps its own timer; `OnInput`, `RowTip` and `Detail` become default interface members the tabs that need them override (`CellPaint` has already moved onto the column in `columns-as-data`); `WithMessage` stays. `ITabBody.Title`, which nothing reads, goes.
- **Built-ins (V-13):** `KeyMapping.TryFrom` (its `out` already `[MaybeNullWhen(false)] out Key` since PR #175) uses `Key.TryGetPrintableRune` for the character branch, after a spike at a real terminal records its modifier semantics in `.claude\rules\terminal-gui.md`; one public `Domain\TextFit.ElideMiddle` replaces `WhereLabel.ElideMiddle` and `ConfigScreenDefinition.Middle`, and `.claude\rules\locale.md` names it once.
- **Disposal (C-07):** the labels `ConsoleScreen.Fill` and `ShowBar` remove with `RemoveAll` are disposed, the bar's after their tooltips are removed with `RemoveToolTip`.
- **Facts (T-08):** new `KeyMappingShould` in `test\Source191.Vantage.Core.Tests\Tui`, modelled on `ConfigScreenDefinitionShould`; `columns-as-data` creates `SessionsScreenDefinitionShould` and `WorktreesScreenDefinitionShould` with its column facts, and this change extends those files with the cell, paint, colour, notifier, tab and refresh facts rather than duplicating them.
- **Docs:** `.claude\rules\console-screen.md` and `terminal-gui.md` name the new owners where they name `TableBody` today, and `test\Source191.Vantage.Core.Tests\README.md` names the new facts.
- **Version:** raised one preview step above `main` when implemented (0.20.0-preview.14 if nothing lands first); a refactor, so patch-level under semantic versioning. One line under Unreleased in `CHANGELOG.md` (under Internal), which `Assert-ChangelogUpdated.ps1` requires of every pull request that changes the packaged tool (tasks.md drafts it).
- `skip_specs: true`: no key, screen, message, state file or command line behaves differently. The one visible difference is a Config value cell at some widths whose middle ellipsis moves by one character, because the two ellipsis rules differed by which half kept the odd character (design.md).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with tests and documentation, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** `src\Source191.Vantage.Tui\TableBody.cs`, `IScreenDefinition.cs`, `ITabBody.cs`, `ConsoleScreen.cs` (`Frame.Refresh`, `StartRefreshing`, `Fill`, `ShowBar`; `Every` and the heartbeat timer stay as PR #179 left them), `KeyMapping.cs`, the three screen definitions; new `ColumnSizing.cs`, `TableToolTips.cs`, `DetailReader.cs` and `TabRefresh.cs` in Tui; new `src\Source191.Vantage.Core\Domain\TextFit.cs`; `WhereLabel.cs` loses its private copy. No port, adapter, reducer or `Program.cs` change.
- **Tests:** new `TextFitShould` in `Domain`; new `KeyMappingShould` in `Tui`; `SessionsScreenDefinitionShould` and `WorktreesScreenDefinitionShould` (from `columns-as-data`, built over the collaborators `DetailSourcesShould` already assembles) gain the facts above; `ConfigScreenDefinitionShould` gains an ellipsis, `Tab` and `Refresh` fact. `WhereLabelShould` stays as it is.
- **Docs:** `.claude\rules\terminal-gui.md` (the modifier semantics and the `RemoveAll` ownership fact, dated), `locale.md`, `console-screen.md`, `test\Source191.Vantage.Core.Tests\README.md`; `src\Source191.Vantage\Source191.Vantage.csproj` for the version and `CHANGELOG.md` for its Internal line.
- **Parallel changes:** `columns-as-data` (a `Fit` per column, `ElasticColumn` removed) and `wording-one-home` (`WaitingText` and `LoadFailurePrefix` off the definitions) edit `IScreenDefinition` and `TableBody` too; `fix-refresh-ordering` (its proposal covers C-01, C-02, C-03 and C-06) edits `TableBody.TriggerLoad` and `RunInBackground`, which stay in `TableBody`. Whichever lands second rebases. This change is implemented after `columns-as-data`, because `ColumnSizing` then reads each column's `Fit` instead of the `"NAME"` and `"AGE"` literals it would otherwise carry for a week.
- **Users:** none beyond the one-character ellipsis shift above.

## Out of scope

- The `TableBody` concurrency findings C-01, C-02, C-03 and C-06 (`fix-refresh-ordering`), and V-10's reshaping of the view states.
- Fixing `WhereLabel.Inside.Fit` below width 5 (D-04): `TextFit.ElideMiddle` is a move, and `WhereLabel`'s guard stays as it is.
- Unit tests for `ColumnSizing`, `TableToolTips` and `DetailReader` themselves, which take a `TableView`: the smoke check and the Tui facts pin them (design.md).
- A row-level smoke step (T-08's longer-term suggestion).
- Multiple Claude accounts, which is a later change.
