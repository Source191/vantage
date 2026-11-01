# Tasks

Each task is one red-green-refactor cycle or smaller, with a commit per step; the fact is named before the code. Start only after `columns-as-data` has merged (design.md); rebase on whichever of `wording-one-home` and `fix-refresh-ordering` lands first. Verify with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` unless a task says otherwise. "The smoke check" means: `tools\install-local.ps1` from a shell with NO_COLOR, CLAUDECODE, CLAUDE_PID and CLAUDE_CODE_* cleared, then `dotnet run -- smoke "$env:USERPROFILE\.dotnet\tools\vantage.exe" Northwind.Smoke-smoke` from `tools\HeadlessTerminal` exits 0, with `~\source\repos\Northwind.Smoke` and its linked worktree `Northwind.Smoke-smoke` seeded as `.github\workflows\check.yml` seeds them and removed after (as the archived `config-tab-follow-ups` recorded); exit 4 names the step whose marker never appeared. New facts use Northwind fixtures and `C:\Users\me` paths.

## 1. One middle ellipsis in Domain

- [ ] 1.1 `TextFitShould.KeepTheOddCharacterAtTheHead` and `ReturnTextThatFits`: `TextFit.ElideMiddle("Northwind.Orders", 9)` is `"North…ers"` and a text no longer than the width is returned whole. Red (the type does not exist), then green with `src\Source191.Vantage.Core\Domain\TextFit.cs` (static, `Ellipsis` constant, `ElideMiddle`). Verify the facts pass.
- [ ] 1.2 `TextFitShould.CutWithoutAnEllipsisBelowThree` (widths 0, 1 and 2 answer the first `width` characters) and `RefuseANegativeWidth` (`ArgumentOutOfRangeException`). Red, then green. Verify the facts pass.
- [ ] 1.3 `WhereLabel.Inside.Fit` calls `TextFit.ElideMiddle` and `WhereLabel` reads `TextFit.Ellipsis`; its private `ElideMiddle` goes. Verify `WhereLabelShould` is unchanged and green.
- [ ] 1.4 `ConfigScreenDefinition.Cell` calls `TextFit.ElideMiddle` and its private `Middle` and `Ellipsis` go; `ConfigScreenDefinitionShould.ElideALongValueInItsMiddle` pins one value at a width that elides. Red (the fact, written for `TextFit`'s split), then green. Verify the fact passes and `locale.md` is updated in the same commit: the UTF-16 sentence names `TextFit.ElideMiddle` instead of `ConfigScreenDefinition.Middle`, and `**/TextFit*.cs` joins its `paths`.

## 2. `KeyMapping`: facts, spike, built-in

- [ ] 2.1 `test\Source191.Vantage.Core.Tests\Tui\KeyMappingShould.cs`: `MapTheNamedKeys` (a theory over the seven named keys), `MapAPrintableCharacter` (`d`, `D` as `GuiKey.D.WithShift`, space, `<`, `/`, `?`), `MapNothingForAControlOrModifiedKey` (`GuiKey.D.WithCtrl`, `WithAlt`, F1, Tab) and `MapNothingForARuneOutsideTheBasicPlane` (an emoji). Verify the facts pass against today's code (green at once is expected; say so in the commit) and `test\Source191.Vantage.Core.Tests\README.md` names the file beside `ConfigScreenDefinitionShould`.
- [ ] 2.2 Spike at a real Windows Terminal (120 x 36) and headless (`dotnet run -- drive`): press Shift+letter, Ctrl+letter, Alt+letter, AltGr `€` on the UK layout, `<`, `>`, `/` and `?`, and log for each whether `TryFrom` is reached and what `TryGetPrintableRune` and `AsRune` answer. Verify by recording the answers in `.claude\rules\terminal-gui.md`, dated and with the Terminal.Gui version, beside the existing key facts; the spike code is not committed.
- [ ] 2.3 If the spike shows the same answers for every key: `KeyMapping.TryFrom`'s character branch becomes `key.TryGetPrintableRune(out Rune rune) && rune.IsBmp ? Key.Of((char)rune.Value) : null`. Verify `KeyMappingShould` stays green and the smoke check passes (it presses `?`, `n`, `!`, `s`, F3 and the tab keys). If any answer differs, keep `AsRune` and verify the rules file says which key differed and why the swap was not made.

## 3. Facts for the two table definitions

- [ ] 3.1 Add to `columns-as-data`'s `SessionsScreenDefinitionShould` (built as `DetailSourcesShould.cs:84` builds its loader: a `StringWriter` notifier, `SessionActivity` over `FakeTimeProvider`, `C:\Users\me\claude.exe`, `FolderLabels`, `SessionDetails`): `ShowEveryColumnOfARow` (name, swatch, state glyph and label, repository fitted, pull request, age, each through its column's `Cell`), `PaintTheTabSwatchAndTheTerminalBlock` (every `Paint` arm), `ColourARowByItsState` and `TellTheNotifierAboutLoadedRows`. Verify the facts pass.
- [ ] 3.2 Add to `WorktreesScreenDefinitionShould`: `ShowEveryColumnOfARow` (worktree label, health, last commit and `-` for none, session name or nothing) and `ColourARowByItsHealth` (all six kinds). Verify the facts pass.

## 4. `IScreenDefinition` slimmed

- [ ] 4.1 `ConfigScreenDefinitionShould.NameItsTab`, and the same fact in the two new files: `Tab` is `ConsoleTab.Config`, `Sessions`, `Worktrees`. Red (no member), then green: `ConsoleTab Tab { get; }` replaces `TabTitle(TState)`, `TableBody` sets `View.Title` once from `Wording.TabTitleFor(definition.Tab)` and `Redraw` no longer sets it, `ITabBody.Title` goes. Verify the facts pass and the smoke check passes (the strip's span is read from `View.Title`).
- [ ] 4.2 `ConfigScreenDefinitionShould.NeverRefreshOnATimer` and `SessionsScreenDefinitionShould.RefreshEveryFiveSeconds`, `WorktreesScreenDefinitionShould.RefreshEveryFifteenSeconds`. Red, then green with `src\Source191.Vantage.Tui\TabRefresh.cs` (`Never`, `Every(TimeSpan)`), `IScreenDefinition.Refresh` and `ITabBody.Refresh`, and `ConsoleScreen.Every` switching over it (both cases named, discard `UnreachableException`), called for all three tabs from `StartRefreshing`. Verify the facts pass, `SealedTypesShould` stays green and the smoke check passes.
- [ ] 4.3 Default interface members for `OnInput`, `RowTip` and `Detail`; `ConfigScreenDefinition` drops `OnInput`, `WorktreesScreenDefinition` drops `RowTip` and `OnInput`, `SessionsScreenDefinition` drops `RowTip`. Verify the build (warnings are errors) and every Tui fact pass, and `IScreenDefinition`'s summary and the stale "such as Sessions (6)" line are rewritten.

## 5. `ColumnSizing<TRow>`

- [ ] 5.1 Move `StyleColumns`, `ElasticWidth`, `WidestCell`, `Scroll`, `columnOffset`, `selected`, `CellColour` and `RowColour` verbatim (as `columns-as-data` left them, reading each column's `Fit`, `Cell` and `Paint`) into `src\Source191.Vantage.Tui\ColumnSizing.cs` with design.md's constructor (the table, the columns, `rowColour`, the application; it wires the getters; `Selected`, `ElasticWidth`, `ResetOffset`, `Scroll`); `TableBody.Redraw` calls them in today's order. Verify the build, the fast suite and the smoke check (which shows every tab).
- [ ] 5.2 Tidy: `TableBody` loses `ElasticFloor`, `columnNames` and whatever else only the moved code read; `ColumnSizing`'s summary says what it owns. Verify as 5.1 and that `TableBody` holds no column arithmetic.

## 6. `TableToolTips<TRow>`

- [ ] 6.1 Move `toolTipAnchor`, `pointer`, `PointerOver`, `ShowColumnToolTips`, `HideColumnToolTip`, `ShowColumnToolTip`, `PointerAt` and `ShowToolTip` verbatim into `src\Source191.Vantage.Tui\TableToolTips.cs`; `TableBody` adds `toolTips.Anchor` to its view and nothing else. Verify the build, the fast suite and the smoke check, then by hand at the terminal: hover a column header on each tab and a Config row, and move off the table; the tooltip shows on the rows under the header and hides on leaving, as today. Record the result here.
- [ ] 6.2 `console-screen.md` and `terminal-gui.md` name `TableToolTips` where they say `TableBody` targets the anchor. Verify by reading both against the code.

## 7. `DetailReader<TRow>`

- [ ] 7.1 Move `DetailPart`, `detailKey`, `askAgain`, `lifetime`, `AddPane`, `AskForDetail`, `ReadDetail`, `ReadAsync` and `ShowDetail` into `src\Source191.Vantage.Tui\DetailReader.cs` as design.md shapes it (`For`, nested `None` and `Pane`, `Views`, `AskFor`, `AskAgain`, `Current`, `Dispose`); `TableBody` adds `detail.Views` to its view, `TableBody.TriggerLoad` compares `detail.Current` as it compared `detailKey`, `Shown` calls `AskAgain`, `Dispose` disposes the reader before the view. Verify the build, the fast suite and the smoke check, then by hand: move the selection on Sessions and Worktrees, switch tabs and back, and press `d` to refresh; the pane follows the selection and empties on an empty table, as today. Record the result here.
- [ ] 7.2 `console-screen.md`'s detail pane section names `DetailReader` for the request lifecycle, and `TableBody`'s summary says what it keeps. Verify by reading against the code.

## 8. Disposal of removed labels

- [ ] 8.1 `ConsoleScreen.Fill` disposes each view `RemoveAll` returns, and `ShowBar` removes each returned label's tooltip (`app.ToolTips?.RemoveToolTip`) before disposing it. Verify the build and the smoke check (the bar is rebuilt on every tab change and filter), and record the `RemoveAll` ownership fact and `RemoveToolTip` in `terminal-gui.md`, dated, Terminal.Gui 2.5.0.

## 9. Version

- [ ] 9.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one preview step above `main` when implemented (patch-level: a refactor). Verify `.github\scripts\Assert-VersionBumped.ps1` passes locally against `main`.

## 10. Whole-solution check

- [ ] 10.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate table-body-collaborators --strict` all pass, and the smoke check passes once more against the final pack. Record the totals here.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
- Push and open the implementation pull request only when the owner asks; the hover and selection checks of 6.1 and 7.1 are the pull request's manual checks.
