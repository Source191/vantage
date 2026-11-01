# Tasks

Each task is one red-green-refactor cycle with a commit per step; the test is named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 1. Health in words

- [x] 1.1 `WordingOfScreensShould.GiveEveryHealthItsOwnGlyph`: a theory over every `WorktreeHealthKind` (✓ ok, ◆ locked, △ orphan, ✗ broken, □ empty, ↗ elsewhere) and a fact that the six glyphs are distinct. Red, then `Wording.HealthGlyph` green. Verify both pass.
- [x] 1.2 `WordingOfScreensShould.WriteTheHealthCellAsGlyphAndWord`: `HealthCell(Locked)` reads `◆ locked`, and so on for each kind; refactor `WorktreeCounts` and the pane's `HealthLine` onto the same private `HealthWord` (design decision 1). Verify the new facts and the existing counts and pane facts pass.
- [x] 1.3 `WordingOfKeysAndColumnsShould.ExplainEveryColumnOfEachView` expects `["WORKTREE", "HEALTH", "LAST COMMIT", "SESSION"]`, and a new fact `ListEveryHealthGlyphInTheHelp` that HEALTH's meaning names every `HealthCell` and WORKTREE's no longer names a colour. Red, then the `WorktreesColumns` entries green. Verify the facts pass and `WordingShould`'s every-column-has-words fact still passes.

## 2. The HEALTH column on screen

- [x] 2.1 `WorktreesScreenDefinition.Cell` renumbers its columns (0 WORKTREE, 1 HEALTH from `Wording.HealthCell`, 2 LAST COMMIT, the rest SESSION); `ElasticColumn` stays 0. Rendering only, no test of its own (the Tui project has none); verify with `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>` against a `tools\install-local.ps1` build (exit 0, the worktrees view found by WORKTREE) and a `grid` replay of a `drive` capture showing ✓ ok in the HEALTH cell of the seeded Northwind.Smoke worktree at 120 x 36. (Code done; the smoke check runs in CI's check job, and the grid replay is owed: see the pull request.)

## 3. Unknown apart from stopped

- [x] 3.1 `WordingOfScreensShould.GiveEveryStateAGlyph`'s Unknown case expects `?`. Red, then `StateGlyph` green. Verify the theory passes.
- [x] 3.2 `WordingOfKeysAndColumnsShould`: the STATE meaning names "? unknown (grey)". Red, then the `SessionsColumns` line green. Verify the fact passes.

## 4. Legible grey

- [x] 4.1 In `Tui\Palette`, add `FieldBackground` at `new(122, 117, 116)` and use it for `Focusable`'s Active role and the field attributes in `NameDialog` and `TypedConfirmationDialog`; then set `Grey` to `new(134, 129, 128)` and delete the unused `IsDimmed`. Constants only (design decision 3); verify by build, by `grep -n "Palette.Grey" src\Source191.Vantage.Tui` showing no use as a background, and by the 5.09:1 figure for #868180 on #0C0C0C recorded in design.md.

## 5. Release

- [x] 5.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next preview. Verify `.github\scripts\Assert-VersionBumped.ps1` passes locally, `dotnet format --verify-no-changes` is clean and `dotnet test -c Debug --no-build` passes at the solution.
- [x] 5.2 Update the README: the sessions table's STATE row gains `? unknown` (grey); the worktrees table's WORKTREE row drops its colour list, a HEALTH row lists the six glyphs and words, and the two paragraphs after the table say the health is in the table as well as the pane. Verify by reading it against the specs' scenarios and checking no em dash was introduced.

## Workflow follow-up

- Archive the change in the pull request that completes it, after `openspec validate colour-blind-status-cues` passes.
