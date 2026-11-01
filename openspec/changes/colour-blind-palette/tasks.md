# Tasks

Each task is one red-green-refactor cycle with a commit per step; the test is named before the code. Implement after `colour-blind-status-cues`, which introduces `Palette.FieldBackground`. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 1. The palette's order

- [x] 1.1 `TabPaletteShould.OrderTheColoursSoTheFirstSixStayApartForRedGreenColourBlindness`: the names in order are Red, Blue, Green, Teal, Pink, Brown, Purple, Orange, Lime, Grey, Yellow, with each hex value as today. Red, then `Domain\TabPalette.Default` reordered and its summary reworded (design decision 1) green. Verify the fact passes.
- [x] 1.2 Fix the facts that named a colour by index: `OwnTabShould`'s `Green` becomes the palette's colour named Green, and the `OpenTabHandlerShould`, `RenameSessionHandlerShould`, `SessionTabsShould` and `TabColourSelectionShould` facts that assume a colour at an index are checked against the new order. Add `TabColourSelectionShould.GiveFiveSessionTabsBesideVantagesGreenFiveDistinctColours` for the session-attach scenario (red, blue, teal, pink, brown). Verify the whole Core suite passes.

## 2. Rows the selection steps over

- [x] 2.1 `ListSelectionShould`: with a policy whose `Selectable` refuses one row, `Move(+1)` and `Move(-1)` step over it, both ends stay put, `Load` never selects it (first load, and a reload where the selected row went), clearing a filter never lands on it, `Select` of its key changes nothing, and `Title` counts only selectable rows. Red, then `ListPolicy.Selectable` and the three `ListSelection` changes (design decision 3) green; the three view policies and the test's `Alphabetical` pass `_ => true`. Verify the new facts and every existing `ListSelectionShould`, `SessionsViewShould` and `WorktreesViewShould` fact pass.

## 3. The Setup heading

- [x] 3.1 `ConfigRowsShould.HeadTheSetupRows`: `ConfigRows.For` places one `ConfigRow.Heading` between Check for updates and Settings file, with a key stable across a save. Red, then green. Verify the fact passes.
- [ ] 3.2 `WordingOfConfigShould`: `ConfigSetting` of the heading is Setup and `ConfigValue` empty; `ConfigCounts` with one root and all setup in place reads 6 settings; `ConfigColumns`' SETTING meaning says "under the Setup heading" and no longer names grey. Red, then green. Verify the facts pass.
- [ ] 3.3 `ConfigViewShould`: the config-tab delta's scenarios (Down from Check for updates selects Settings file, Up from Settings file selects Check for updates, filtering by bell shows only Claude bell, the title counts 11 rows) through `ConfigView.Reduce`, the policy's `Selectable` refusing `ConfigRow.Heading`. Red, then green. Verify the facts pass.
- [ ] 3.4 `ConfigScreenDefinition.RowColour` draws the heading in `Palette.Orange` and setup rows grey as now. Rendering only; verify by a `grid` replay of a headless `drive` capture of the Config tab at 120 x 36 showing Setup between Check for updates and Settings file, and the selection stepping over it on Down.

## 4. Field contrast

- [ ] 4.1 Set `Tui\Palette.FieldBackground` to `new(96, 96, 96)`. A constant (design decision 2); verify by build, by the 5.04:1 and 3.11:1 figures in design.md, and by opening the rename dialog in a `tools\install-local.ps1` build, tabbing to Cancel and seeing the name field still drawn as a field.

## 5. Release

- [ ] 5.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next preview. Verify `.github\scripts\Assert-VersionBumped.ps1` passes locally, `dotnet format --verify-no-changes` is clean, `dotnet test -c Debug --no-build` passes at the solution and `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>` exits 0.
- [ ] 5.2 Update the README: the Config tab's section says the setup sits under a Setup heading the selection steps over, rather than "in grey"; where it describes tab colours, say the palette is ordered for red-green colour blindness. Verify by reading it against the specs' scenarios and checking no em dash was introduced.

## Workflow follow-up

- Archive the change in the pull request that completes it, after `openspec validate colour-blind-palette` passes.
