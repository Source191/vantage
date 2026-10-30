# Design

## Context

`TableScreen` registers each key's `KeyHelp.Line` as the tooltip of both the key label and the hint label (`ShowToolTips`), and shows a column's meaning by hand with `ShowFor` after replacing its newline with a space. The tooltip view takes the window's plain scheme, so it is white text on the background with no border. The table has `ExpandLastColumn` on and no column styles, so each column is as wide as its longest value, and `CanFocus` is false, so Terminal.Gui's own sideways scrolling never engages. The header draws its values 45 columns wide from column 16, the key hints from `KeyHintColumns.Layout` (62, 90 and 116 for the keys, 72, 98 and 124 for the words) and the logo at `AnchorEnd(12)` on rows 0 and 1. The sessions summary is built in `SessionsScreenDefinition.ContextLines`. Terminal.Gui 2.5 offers `ColumnStyle.MaxWidth` and `TruncationIndicator`, `TableView.ColumnOffset`, and a `ToolTipProvider` that takes a view factory.

## Goals / Non-Goals

**Goals:**
- Every column of both tables can be reached at 120 columns, and nothing is lost: the row keeps full values for copy, editor, solution and the title.
- A tooltip is recognisable as one, and says something the help line does not.
- The header's three hint columns fit 120 columns with the logo still shown.

**Non-Goals:**
- Making all eight sessions columns fit at 120 without a scroll: that is the WHERE column's job (the fixed columns leave about 23 cells for REPOSITORY and FOLDER together).
- A tooltip on a row.

## Decisions

- **A `Tip` beside `Hint` and `Line` on `KeyHelp`.** The help dialog keeps two keys to a line, so its `Line` must stay short; the tooltip has room for a sentence ("Open this session in its own Windows Terminal tab; if that tab is already open, switch to it"). A `WordingShould` fact checks every key of both views has a tip of at most 80 characters, so a new key cannot ship without one.
  - *Rejected: lengthening `Line`.* The help dialog is clipped at about 31 rows on a 40-row terminal and would lose lines.
- **The tooltip is a view of Vantage's own.** `SetToolTip(label, () => view)` and `ShowFor(table, new ToolTipProvider(() => view))` get a `Label` with `Palette.Pair(Palette.Background, Palette.Amber)` (the crumb's colours) and a one-cell margin, built per show so its text is current. Multi-line text is kept as lines; the view's height is its line count.
  - *Rejected: restyling Terminal.Gui's default tooltip.* Its scheme is not exposed in 2.5.0's API.
- **Caps and a scroll, not a fit.** `ColumnStyles` give NAME, REPOSITORY, FOLDER and BRANCH a `MaxWidth` of 28 with "…" as the truncation indicator; the fixed columns (STATE, PR, AGE, TAB; DIRTY, UNPUSHED, LAST COMMIT, HEALTH) keep their natural widths. Left and Right move `table.ColumnOffset` by one column, handled in `TableScreen` before the reducer sees the key, because the offset is presentation and no rule depends on it; `KeyMapping` learns the two keys so the reducer can ignore them. The sessions summary and title are unaffected.
  - *Rejected: computing caps from the terminal width.* At 120 columns the result is about 11 cells per name column, which reads worse than a scroll.
  - *Rejected: hiding REPOSITORY when FOLDER starts with it.* That is the WHERE column, half done.
- **The hint layout.** The value column stays 45 wide (the header's longest spec'd lines, the below-minimum Claude warning and the update notice, need it) and the logo stays on rows 0 and 1. The third column (`c`, `e`, `v`, `p`: four keys) moves to rows 2 to 5 under the logo, and `KeyHintColumns.Layout` gains a first row per column: keys at 62 (width 7, for `<enter>`), 86 and 108 (width 4: no other key is longer than `<?>`), words at 70 (15), 91 (16, so "Close empty tabs" fits whole) and 113 (11, enough for "Open folder" when dialog-fixes renames `e`). Every key ends by column 112 and every word by 123, so the owner's monitor window shows them all; at 120 the third column's words are cut after seven cells and its keys still show. `KeyHintColumnsShould` asserts those bounds, that no two columns overlap on any row, and that the third column starts below the logo.
  - *Rejected: narrowing the value column to fit 120.* It clips two header lines the session-listing spec names.
  - *Rejected: moving the logo to the footer row.* A failed outcome wraps to two footer lines (the Permission denied screenshot), which the logo would overwrite.
  - *Alternative for the owner:* demote `/`, `?` and `q` to help-only and fold the third column into the second, which fits 120; offered in the proposal, not taken here because the key bar replaces the layout next.
- **The summary is `Wording.SessionsSummary(rows)`**, built from the state labels, with the brackets only when the breakdown is non-empty; `SessionsScreenDefinition.ContextLines` calls it. The worktrees summary already never has empty brackets (every row has a health).
- **The help scrolls (issue #89).** A `HelpDialog` in `Tui` replaces `Confirm.Query` for `ShowHelp`: a read-only `TextView` (the project's wrapping box, with the obsolete pragma the wizard already uses) holding the view's help text, `Height = Dim.Fill(2)` inside a dialog of 90 % height, a Close button, and `DialogTabTitle.Apply` with `DialogName.Keys` so the tab still reads "Vantage (Keys)" and the smoke script's marker holds. Focus starts in the text, so the arrow keys and PageUp and PageDown scroll it. At 40 rows the box has room for the 28 lines the sessions help wraps to, so nothing scrolls; at 36 rows the last lines are a PageDown away.
  - *Rejected: shortening the help to fit 36 rows.* Twenty keys two to a line, eleven column lines and the path make 28 lines; fitting about 21 means dropping the path or part of the column glossary the spec promises.
  - CLAUDE.md's Terminal.Gui fact that caps the help at about 22 lines goes, replaced by the fact that the help scrolls.

## Risks / Trade-offs

- [A capped NAME hides what distinguishes two long names] → the table title names the selection in full, and the filter matches the full name.
- [Left and Right were free in both views; a later feature wants them] → the key bar redesign owns the header keys and can reassign them.
- [The hover test needs a real mouse] → the headless terminal sends SGR motion reports (CLAUDE.md), so a capture can show the tooltip's colours; the owner's hover check stays in the tasks.
