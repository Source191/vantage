# Design

## Context

`SessionRow` carries the repository name, the branch and the working directory; the FOLDER cell is `FolderLabel.For(folder, roots, home)` through `FolderLabels`, read per redraw. `SessionRowEnricher` knows each row's checkout (`Location.Checkout`, null outside a repository) and reads the tab registrations for `HasLiveTab`, but hands neither the checkout nor the tab's colour to the row. `TableScreen.CapLongColumns` gives NAME, REPOSITORY, FOLDER and BRANCH a `MaxWidth` of 28 with "…" as the end indicator, and Left and Right move `ColumnOffset`. Terminal.Gui 2.5.0's `ColumnStyle` has `MaxWidth`, `MinWidth`, `Alignment`, `ColorGetter` (a scheme per cell) and `TruncationIndicator`, which cuts the end of a cell only. `IScreenDefinition.Cell(row, column, now)` answers a cell's text without knowing its width. The 27 October mockup lays the sessions table out at 120 columns as NAME 14, TAB 3, STATE 13, WHERE 64, PR 11, AGE 5; MEM (six cells and a separator) stays in this change, so WHERE has about 57.

## Goals / Non-Goals

**Goals:**
- Every column of both tables fits a 120-column terminal with the fixtures' longest names, and the slug and branch of a long WHERE are never cut.
- The place a session runs is one value with one rule, in `Domain`, tested without a terminal.
- The TAB column says what it said in one cell, with the tab's colour.

**Non-Goals:**
- Hiding columns by priority below 120 columns: the scroll from #98 stays for that.
- Changing the filter fields, the title, the keys or the footer.
- Removing MEM (detail-pane does that) or changing the header (tabs-and-key-bar).

## Decisions

- **`Domain\WhereLabel` holds the rule.** A record of `Repository` (string, or null outside a repository), `Place` (the folder's part after the repository: the slug, the inner path, or the whole label when the folder is neither under the repository nor named after it) and `Branch` (null when it equals the place, or when there is no branch), built by `WhereLabel.For(repositoryName, folderLabel, branch, inRepository)`. `Text` renders `Repository › Place (Branch)` with each part omitted when absent; a main checkout has an empty place and renders `Repository (Branch)`. `Fit(int width)` elides the repository's middle first (keeping at least its first eight and last eight characters around "…"), then the place's end, and never touches the branch; the place and branch are what distinguish rows that share a repository.
  - *Rejected: a ColumnStyle representation getter in Tui.* The rule would live in a project without unit tests, and the worktrees view needs the same rule.
  - *Rejected: reading the slug from the branch.* A hand-made worktree's folder need not match its branch; the folder is the fact, the branch is the annotation.
- **`SessionRow` gains `Checkout` (FolderPath?) and `TabColour` (TabColour?).** The enricher already computes the first and reads the registrations for the second (`ITabRegistry` entries carry `Colour`); the `HasLiveTab` flag stays for the reducer's rules. `WorktreeRow` needs nothing: it has `Repository`, `Path` and `Branch`.
- **The elastic column is the definition's to name.** `IScreenDefinition` gains `int? ElasticColumn` and `Cell` takes a `width` (int.MaxValue for the others). `TableScreen` computes the elastic width on each redraw as the table's content width minus the other columns' widths (each column's longest cell among the visible rows, capped as before) and the separators, with a floor of 20, and `TableSource` passes it to `Cell`. The definition answers `WhereLabel.Fit(width)` for WHERE and WORKTREE.
  - *Rejected: `ColumnStyle.MaxWidth` from the screen width alone.* Terminal.Gui cuts the end; the middle elision needs the text rebuilt.
- **The swatch is a `ColumnStyle.ColorGetter` on TAB.** The cell text is "▮" (U+25AE) for an open tab, "▯" (U+25AF) for a terminal session, "" otherwise; the getter answers `Palette.Pair(tabColour, Background)` for the solid block and grey for the hollow one, keeping the row's highlight when selected. The `TabColour` hex is parsed once per redraw into a Terminal.Gui `Color`.
- **STATE's glyph is Wording's.** `Wording.StateGlyph(DisplayStateKind)` is a switch over the closed enum (no discard arm, so a new kind fails the build), used by the sessions cell and, in tabs-and-key-bar, by the counts line. AGE gets `ColumnStyle.Alignment = Right`.
- **Caps.** `CapLongColumns` keeps NAME (sessions) and BRANCH (worktrees) at 28; REPOSITORY and FOLDER go with their columns. The README's "cut at 28 characters" sentences name only those two.
- **Worktrees keep BRANCH.** The 27 October mockup, which the owner approved, shows WORKTREE and BRANCH side by side; the view's DIRTY, UNPUSHED and merge facts are about the branch, and BRANCH's cap keeps it to 28. Folding it into WORKTREE as the sessions view does is one line in the definition if the owner prefers it later.
- **The smoke test is untouched.** Its view markers are the first column each table has that the other lacks: NAME for sessions today and after this change, and for worktrees DIRTY today and WORKTREE after it, resolved from `Wording`'s lists at run time; the match is ordinal, so WORKTREE never matches the header's "Worktree root". Its key lookups are by hint.

## Risks / Trade-offs

- [A branch whose name holds "›" or brackets reads oddly] → the parts are joined, never parsed back; the filter matches the branch's own text.
- [Computing the elastic width from the visible rows' longest cells costs a pass per redraw] → the rows are at most tens, and the pass is the same `Cell` call the table makes anyway.
- [A terminal narrower than about 95 columns leaves WHERE under its floor] → the scroll from #98 still reaches the right-hand columns, and the floor keeps the slug readable.
- [MEM keeps WHERE at 57 on the laptop until detail-pane lands] → the middle elision keeps both ends of the name readable, which the 28-cell end cut did not.

## Migration Plan

One pull request; no state or settings change. The README's two column tables and CLAUDE.md's FOLDER fact change with the code.
