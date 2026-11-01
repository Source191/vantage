# Proposal

## Why

An accessibility review (1 November 2026) simulated protanopia, deuteranopia and tritanopia (Machado 2009, full severity) over the console's palette and measured its contrast against WCAG 2.2. The Sessions table is safe because every state has its own glyph and word, but the Worktrees table shows a row's health by colour alone: to a protanope red (broken, orphan) and grey (empty) differ by ΔE 13.5, and locked and elsewhere share amber for everyone. A screen reader reads nothing at all for health. Two smaller faults sit beside it: stopped and unknown sessions share the grey ■, so only the word tells them apart, and `Palette.Grey` (#7A7574) is 4.31:1 on the console's background, under the 4.5:1 WCAG AA asks of text, though it draws stopped rows, empty worktrees, setup rows, hints and the automated count.

## What Changes

- The worktrees table gains a HEALTH column between WORKTREE and LAST COMMIT, reading a glyph and the health's word in the row's colour: ✓ ok, ◆ locked, △ orphan, ✗ broken, □ empty, ↗ elsewhere. The pane's Health line keeps the reason in full; the help and the HEALTH header's tooltip list the glyphs.
- An unknown session state reads ? and its label instead of ■, so stopped and unknown differ by shape; the STATE help line names it.
- `Palette.Grey` becomes #868180 (5.09:1) wherever grey is a foreground: rows, hints, frames, the hollow TAB block and separators. The three places that use it as a background (a dialog list's chosen row after focus moves on, and the name and typed-confirmation fields when unfocused) move to a new `Palette.FieldBackground` at today's #7A7574, so they look as they do now and do not lose contrast; `colour-blind-palette` then darkens it.
- The session-listing requirement that completed, failed and stopped rows are dimmed is corrected to what the code does (each row in its state's colour; nothing dims), and the unused `Palette.IsDimmed` goes.
- The tool's version rises to the next preview in the pull request that implements this.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `worktree-management`: the HEALTH column, health glyphs, and the pane no longer the only place the health is read.
- `session-listing`: the unknown state's glyph, and no dimming.
- `console-layout`: grey text meets 4.5:1 wherever it is drawn.

## Impact

- **Code:** `Application\WordingOfScreens` (`HealthGlyph`, `HealthCell`, `StateGlyph`, `WorktreesColumns`, `SessionsColumns`), `Tui\WorktreesScreenDefinition` (the new column), `Tui\Palette` (`Grey`, `FieldBackground`, `IsDimmed` removed), `Tui\NameDialog` and `Tui\TypedConfirmationDialog` (their fields' background).
- **Tests:** `WordingShould`, `WordingOfKeysAndColumnsShould`, the reducer and screen facts that count the worktrees columns.
- **Tools:** `tools\HeadlessTerminal\SmokeScript` keeps finding the worktrees view by WORKTREE, a column only that view has; nothing to change, but the smoke run is the check.
- **Users:** the WORKTREE column is about 12 cells narrower at the same terminal width (it is elastic, with a floor of 20); no key, state file or command line changes.

## Out of scope

- The tab palette's order, the dialogs' field and list contrast and marking the Config tab's setup rows without colour: the companion change `colour-blind-palette`.
- A user-chosen theme, a light or high-contrast palette, or following the terminal's own theme.
- Screen reader support beyond giving every coloured cue words, and a setting to stop the spinners.
- Glyphs in the worktrees counts beside the tabs, which already read as words (5 ok · 1 locked).
- Multiple Claude accounts, which is a later change.
