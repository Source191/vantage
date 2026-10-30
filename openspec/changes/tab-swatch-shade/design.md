# Design

## Context

Vantage colours a session's tab from `Domain\TabPalette` (each `TabColour` a name and a hex value), through `--tabColor` or the OSC 4 sequence on palette entry 264. `SessionsScreenDefinition.CellPaint` draws the TAB block in that hex value with `CellPaint(colour, Always: true)`, and `Palette.SelectedRow(colour)` keeps it on the selection bar. Windows Terminal draws a selected coloured tab in its colour and an unselected one faded; how it fades (an alpha over the tab row's background, a fixed blend, or something else) is not documented.

## Goals / Non-Goals

**Goals:**
- The block looks like the tab in the strip as the user sees it from Vantage's tab, for every palette colour.

**Non-Goals:**
- Tracking which tab is selected: Vantage's own tab is selected whenever its table is visible, so the unselected shade is always the right one.
- Matching a user's custom Windows Terminal theme or tab row colour.

## Decisions

- **Measure, then model.** Screenshots of the tab strip with one tab of each palette colour unselected (and the tab row's background beside them) give the shades; the rule is then fitted to them (most likely the tab colour blended over the tab row's background at a fixed alpha) and checked against every palette colour. Alternative: a table of measured shades per colour, kept only if no single rule fits within one step per channel.
- **The rule lives with the palette.** A pure function in `Domain` beside `TabPalette` (`TabColour.Unselected` or `TabShade.Of`), so the Tui only paints and the tests need no terminal.
- **Measured on the owner's setup.** Windows Terminal 1.25, dark theme, the Vantage profiles; the fact, its date and the version go in CLAUDE.md's Windows Terminal facts, as the other measured facts do.

## Risks / Trade-offs

- [A Windows Terminal update changes the fade] → the CLAUDE.md fact names the version measured; the owner's terminal check after a Windows Terminal update is the guard, since no automated run sees the tab strip.
- [A light theme or a custom tab row colour fades differently] → out of scope; the block stays close enough to identify the tab.

## Migration Plan

Nothing is stored; the change only alters the colour drawn. Rolling back is installing the earlier release.

## Open Questions

- The exact fade, which task 1.1 measures before any code.
