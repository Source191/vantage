# Proposal

## Why

The TAB column's block is drawn in a session tab's exact colour, which is the colour Windows Terminal gives that tab only while it is selected. Whenever the Vantage table is being read, Vantage's own tab is the selected one, so every session tab shows the faded shade Windows Terminal gives an unselected coloured tab, and the block never quite matches the tab it stands for (the owner's observation, 30 October 2026). Windows Terminal's documented settings cannot keep an unselected coloured tab bright (a `tabColor` overrides the theme's `unfocusedBackground`), so the block should match the faded shade instead. Proposed now and implemented later.

## What Changes

- **The TAB block in the unselected shade:** the two-cell block is drawn in the colour Windows Terminal shows for that tab while another tab is selected, on every row and on the selected row, measured rather than guessed.
- **One rule for the shade:** the fade is worked out once from screenshots of tabs whose colours are known, and kept as one rule beside the tab palette, so a new palette colour gets its shade without a second table.
- **The help and README** say the block matches the tab as it looks from Vantage's tab.
- **Version:** one preview step above `main`'s when implemented; this documentation pull request changes no version.

Out of scope: changing the tabs' own colours or the palette, the hollow `▯` swatch for a plain `claude` (it is grey, not a tab colour), following a user's own Windows Terminal theme, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-listing`: ADDED "The TAB block matches the tab as it looks from Vantage", beside "The TAB column is a swatch" (whose text where-column and tabs-and-key-bar still hold unarchived).

## Impact

- **Domain:** `TabPalette` (or a `TabShade` beside it) gains the rule that turns a tab colour into its unselected shade, with a test per palette colour.
- **Tui:** `SessionsScreenDefinition.CellPaint` paints the TAB block with the shade.
- **Tests:** a domain fact per palette colour against the measured shades; the existing swatch facts follow.
- **Docs:** README's TAB row, the TAB column's help in `Wording`, CLAUDE.md's Windows Terminal facts (the measured fade and how it was measured).

## Order

Implemented after detail-pane (the owner's order, 30 October 2026), and after where-column and tabs-and-key-bar archive, so its requirement sits beside theirs.
