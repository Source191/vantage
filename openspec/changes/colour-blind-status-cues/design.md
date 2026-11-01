# Design

## Context

See proposal.md for why. What shapes the approach:

- **Where colour is decided:** `Tui\Palette` holds every colour value; `Tui\WorktreesScreenDefinition.RowColour` maps `WorktreeHealthKind` to a colour, and its `Cell` switch numbers the columns 0 (WORKTREE), 1 (LAST COMMIT) and the rest (SESSION). The Tui project has no tests: it renders and runs effects, and everything it shows comes from `Wording`, which is tested.
- **Words and glyphs:** `Wording.StateGlyph` is a switch over the closed `DisplayStateKind` (CS8509 fails the build for a missing member), tested in `WordingOfScreensShould`; the column names and their help are `Wording.SessionsColumns` and `WorktreesColumns`, whose names `WordingOfKeysAndColumnsShould` lists and whose meanings become the help and the header tooltips. The health's word is already `kind.ToString().ToLowerInvariant()` in `WorktreeCounts` and the pane's Health line.
- **Grey as a background:** `Palette.Focusable`'s Active role (white on grey) and the unfocused fields in `NameDialog` and `TypedConfirmationDialog` draw white on `Palette.Grey`. At #7A7574 that is 3.64:1; lifting the grey to #868180 would drop it to about 3.1:1.
- **Measured figures** (WCAG 2.2 relative luminance; Machado 2009 at full severity in linear RGB, CIE76 ΔE): grey #7A7574 on #0C0C0C is 4.31:1, #868180 is 5.09:1; protan red against grey ΔE 13.5.

## Goals / Non-Goals

**Goals:**

- Every colour on the Worktrees and Sessions tables has words or a distinct shape beside it, so the table reads the same in monochrome and to a screen reader.
- No grey text under 4.5:1, and no field or list highlight left worse than today.

**Non-Goals:**

- Changing any colour other than the grey, or any colour's meaning.
- A test project for `Tui`: the colour values stay constants checked by the figures above.

## Decisions

### 1. HEALTH is a column, its words from `Wording`

`Wording.HealthGlyph(WorktreeHealthKind)` is a switch over the closed enum (✓ ok, ◆ locked, △ orphan, ✗ broken, □ empty, ↗ elsewhere) and `Wording.HealthCell(kind)` gives `"{glyph} {word}"`, the word the one the counts and pane already use (a shared private `HealthWord`, so the three cannot drift). `WorktreesColumns` gains `HEALTH` second, its meaning listing the glyphs; WORKTREE's meaning drops the colour list it carries today. `WorktreesScreenDefinition.Cell` gains the column (0 WORKTREE, 1 HEALTH, 2 LAST COMMIT, the rest SESSION) and keeps the row colour, so the cell is drawn in the health's colour like the rest of the row.

- **Alternative, a glyph before the WORKTREE cell:** offered and declined by the owner; a separate column reads as a word to a screen reader and lines up for scanning.
- **Alternative, ✓ ok left blank:** rejected with the column, because a blank cell reads as missing data.
- **Glyph choice:** ✗ and ✓ already mean failure and success elsewhere in the console; ◆, △, □ and ↗ come from the Geometric Shapes and Arrows blocks, which Cascadia Mono draws, single-width like the ◐ ○ ● ■ already in use. ↗ was chosen for elsewhere because the worktree points out of the roots, □ for an empty folder, △ for an orphan as a warning shape distinct from broken's ✗.

### 2. Unknown gets ?

`StateGlyph(DisplayStateKind.Unknown)` answers `?`. An unknown state's label is the CLI's own `status/state` text (`ClaudeStates`), so the cell reads `? busy/paused`. The STATE help line adds "? unknown (grey)".

### 3. One grey for text, a named background for fields

`Palette.Grey` becomes `new(134, 129, 128)` (#868180). A new `Palette.FieldBackground` keeps `new(122, 117, 116)` and replaces `Grey` in `Focusable`'s Active role and in the two dialogs' field attributes, so those draw exactly as today. `colour-blind-palette` owns darkening it; splitting the background out here keeps that change to one value.

- **Alternative, lift every use of grey:** rejected, it would make the field and list highlights worse until the second change lands.

### 4. The spec catches up with the code on dimming

The session-listing requirement says completed, failed and stopped rows are dimmed; the code draws each row in its state's colour and `Palette.IsDimmed` has no caller. The requirement is corrected to the code, and `IsDimmed` is deleted, rather than adding dimming: a dimmed failed row would lose the red that marks it.

## Risks / Trade-offs

- [WORKTREE loses about 12 cells at the same width] → it is the elastic column with a floor of 20 and elides the repository's middle first; at 120 columns the longest Northwind fixture still reads whole.
- [A glyph missing from the user's font shows as a box] → all six are in Cascadia Mono and Consolas; the word beside each glyph carries the meaning regardless.
- [The lifted grey is brighter in the tab frames too] → the frames are meant to recede; 5.09:1 is still well below the white text's 15.7:1, so the hierarchy holds.

## Migration Plan

None: nothing stored changes. The next preview's release notes mention the HEALTH column.
