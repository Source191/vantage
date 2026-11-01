# Proposal

## Why

`TableBody.StyleColumns` (`src\Source191.Vantage.Tui\TableBody.cs:480-489`) gives a column its 28-cell cap and its right-alignment by comparing the header text with the literals `"NAME"` and `"AGE"`, while the headers are data in `Wording.SessionsColumns` (`src\Source191.Vantage.Core\Application\WordingOfScreens.cs:184-233`): rewording either in `Wording` silently drops the cap or the alignment, and nothing on the Tui side would notice. The visible result today is that the Worktrees tab's LAST COMMIT cell shows the same kind of value as AGE (`Durations.Age`) but left-aligned, because its name differs. Every `Cell(row, column)` switch and `ElasticColumn => 3`, `0`, `1` in the three definitions is coupled to the order of the `Wording` list by hand, with no test (code review of 0.20.0-preview.7, V-05 and section 4 item 1).

## What Changes

- The Worktrees tab's LAST COMMIT column is right-aligned, as AGE is on the Sessions tab, so durations line up on both tabs.
- NAME's 28-cell cap with its ellipsis, AGE's and LAST COMMIT's right-alignment, and which column is elastic (REPOSITORY, WORKTREE, VALUE) become a property of each column, declared once in the view's definition, never decided from the header's words.
- Each screen definition describes its table as a list of columns, each holding its `ColumnHelp` from `Wording`, how its cell is written and how it fits; the positional `Cell(row, column)` switches, `ElasticColumn` and the `TabColumn` and `RepositoryColumn` constants go.
- `Wording.SessionsColumns`, `WorktreesColumns` and `ConfigColumns` stay the one home for the column words; each entry gains a name in `Wording` (`Wording.AgeColumn`, `Wording.TabColumn` and so on, help entries, not the Tui's deleted index constants of the same name) so a definition refers to a column by that name, never by an index into the list.
- A fact per definition (`SessionsScreenDefinitionShould`, `WorktreesScreenDefinitionShould`, and one added to `ConfigScreenDefinitionShould`) checks that its columns' help equals the `Wording` list in order and that exactly one column is elastic, which is the test the Tui side has lacked.

Out of scope: any other change to what the cells say or to the 120-column fit; slimming `IScreenDefinition`'s other members and extracting column sizing from `TableBody` (the parallel change `table-body-collaborators`, V-08 and V-09); moving the waiting words and failure prefixes off the definitions (the parallel change `wording-one-home`, V-07); the `KeyHelp`-to-reducer fact (V-06); and multiple Claude accounts, which is a later change. The three parallel changes touch `TableBody.cs`, `IScreenDefinition.cs` and the three definitions: whichever lands second rebases onto the first.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-listing`: a new requirement states each sessions column's fit (NAME cut at 28 cells with an ellipsis, AGE right-aligned, REPOSITORY elastic) as the column's own property, so it survives a reworded header.
- `worktree-management`: "One WORKTREE column names the worktree" gains LAST COMMIT right-aligned as AGE is, and the scenario that shows it.

## Impact

- `src\Source191.Vantage.Tui\IScreenDefinition.cs` (`Columns` becomes a list of columns; `ElasticColumn`, `Cell` and `CellPaint` go), two new Tui types beside it (`TableColumn<TRow>`, `ColumnFit`), `TableBody.cs` (`StyleColumns`, `ElasticWidth`, `WidestCell`, `Redraw`'s cell callback, `CellColour`, the header tooltip), `SessionsScreenDefinition.cs`, `WorktreesScreenDefinition.cs`, `ConfigScreenDefinition.cs`.
- `src\Source191.Vantage.Core\Application\WordingOfScreens.cs` (a named `ColumnHelp` per column, the three lists built from them) with a fact in `WordingOfKeysAndColumnsShould`.
- `test\Source191.Vantage.Core.Tests\Tui\ConfigScreenDefinitionShould.cs` (its `Cell` call) and two new test files beside it; `tools\HeadlessTerminal\SmokeScript.cs` is untouched, since it reads the `Wording` lists.
- `CLAUDE.md`'s Reducers line, which describes an `IScreenDefinition` as "columns, colours, cell paint, ...": a column is now its help, its cell and its fit.
- `Version` raised in the implementing pull request: patch, an inconsistency fixed with no new key.
