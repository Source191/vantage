# Proposal

## Why

Eight header rows hold versions, roots and counts that rarely change and nineteen key hints laid out for a 134-column terminal, so at 120 columns the third hint column's words are cut, `b`, `m` and `U` have no hint at all ("the hint grid is full"), and with six sessions twenty table rows sit empty. Switching views rebuilds the header and runs a new Terminal.Gui application each time (the `w` and Esc dance). The UI review's revised recommendation (26 and 27 October, confirmed 30 October 2026) puts both views in tabs of one screen with a two-line settings-and-counts header and the keys in a bar at the bottom that explains itself.

## What Changes

- **One grey settings line** replaces the six header rows: `Vantage 0.18.0-preview.3 (35 MB) · Claude 2.1.310 · Repository roots C:\Users\me\source\repos · Worktree root C:\Users\me\source\worktrees`. The update notice and the below-minimum Claude warning keep their places in it; the roots elide first when the line is wider than the terminal (it is 128 characters with the fixture paths, 159 with an update notice; the worktree root elides after the roots when the notice needs the room) and show whole from about 160 columns. The logo goes: the tab title and this line say Vantage.
- **Sessions and Worktrees are two tabs of one screen.** The tab titles carry the counts (`Sessions (6)`, `Worktrees (7)`), and the state and health counts sit beside them with the state glyphs in their colours (`◐ 2 need input · ● 1 working · ○ 1 idle · ✓ 1 completed · ■ 1 stopped`; `5 ok · 1 locked · 1 broken · 2 with changes · 2 with unpushed commits`). `1` and `2` select a tab, `w` still toggles, Esc on the worktrees tab still returns, and a click on a tab header selects it. Ctrl+Tab is not offered: Windows Terminal takes it for its own tabs. Each tab keeps its rows, selection, filter and footer while the other is shown, so switching is instant; the worktrees scan starts when its tab is first opened and refreshes only while it is shown.
- **The keys move to a two-line bar at the bottom**, with the crumb and the footer message above it: the first row holds the keys that act on the selected session or worktree, with `n New session` and, when a release is known, `U Update`; the second row the folder keys and the view keys. The bar follows the selected row: a terminal row's first row reads `b Adopt`, `d Close tab`; `R Resume` shows only while a stopped or failed row is listed, `C Close empty` only while an empty terminal session is, `m Memory` only on a running row. Every key keeps its meaning, the help lists every key, the bar's items are clickable and show the key's tooltip on hover; `b`, `m` and `U` get their words back, and both rows fit 120 columns for every kind of row. The header's hint grid, its tooltips and `KeyHintColumns` go.
- **One Terminal.Gui application for the whole console.** A view switch no longer leaves the application; only an attach (and quit) does, and the console comes back to the same tab, row and filter.
- **The table gains three rows** at 36 rows (the header shrinks from eight rows to five, the bar takes two at the bottom).
- **Version:** one preview step above `main`'s.

Out of scope: the detail pane (detail-pane), the wizard (wizard-one-form), warming the worktrees scan in the background, sorting by clicking a column header, a context menu or command palette, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

- `console-layout`: the frame both views share: the settings line, the tabs with their counts, the key bar that follows the selected row, and the one application that keeps each tab's state across switches and hand-offs.

### Modified Capabilities

- `session-listing`: "The screen is a header, a table and a footer" becomes the tab's body (title, table) inside the frame; "The header shows the versions and the roots" becomes the settings line; "The header summarises sessions by state" and "The header counts sessions needing input" move the counts beside the tabs; "Key hints explain themselves on hover" moves the tooltips to the bar; "Long names never hide columns" (as where-column leaves it) drops the header-hint scenarios' header for the bar; "A key shows what the memory is made of" lists `m` in the bar.
- `worktree-management`: "The worktrees header and help match the sessions view" follows the same frame.

## Impact

- **Application:** `Views\ConsoleState` and `ConsoleView` (which tab is active, the two view states, the tab-switch rule for `SwitchToSessions`, `SwitchToWorktrees` and `JumpToSession`, which stop leaving the screen), `ScreenLoop` (one screen, hand-offs only), `Screens.cs` (`IConsoleScreen` in place of the two screen interfaces), `KeyHelp` (a group), `WordingOfScreens` (the settings line, the counts, the tab titles, the bar's words per row kind, `1` and `2` in the help), `Effects` (an `ISwitchesTab` marker).
- **Tui:** `ConsoleScreen` replaces `SessionsScreen`, `WorktreesScreen` and most of `TableScreen` (the body per definition stays as the table, title, busy spinner and waiting indicator), a `Tabs` view hosting the two bodies, a `StatusBar` of `Shortcut`s for the bar, `IScreenDefinition` (the row kind of the selected row, the counts, the tab title); `KeyHintColumns` deleted.
- **Program.cs:** builds one `ConsoleScreen` with both definitions.
- **Tests:** `ConsoleViewShould` (new), `ScreenLoopShould` (reshaped), `WordingOfScreensShould` and `WordingShould` (the line, the counts, the bar fits 120 for every row kind), `EffectRoutingShould` (every effect that leaves the screen is Quit or a hand-off; every tab switch is handled), `SessionsViewShould` and `WorktreesViewShould` (`1`, `2`); `KeyHintColumnsShould` deleted; headless captures at 120 x 36 and 160 x 36; the hand-off check.
- **Docs:** README (the header, the keys tables gain `1` and `2`, the worktrees view's screen text, the sentences on tooltips and the logo), CLAUDE.md (the header facts, the Terminal.Gui facts for `Tabs`, `StatusBar` and `Shortcut`, the screen architecture paragraph), the three specs.
- **Release:** a preview raise; the hand-off check and the smoke check; the owner's terminal check of the bar and the tabs at both sizes.

## Order

The second of four changes from the UI review, after where-column and before wizard-one-form and detail-pane. The six changes for the 0.15.0 test findings archive before the series; this change's MODIFIED blocks for "The header summarises sessions by state" and "Key hints explain themselves on hover" are based on header-tooltips-and-table-fit's delta text, and for "Long names never hide columns" on where-column's.
