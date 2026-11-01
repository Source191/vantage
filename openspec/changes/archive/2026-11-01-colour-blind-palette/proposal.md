# Proposal

## Why

The accessibility review of 1 November 2026 (see `colour-blind-status-cues`) found three faults that remain once every status has words. The tab palette was ordered for normal colour vision: simulated for protanopia, its third colour (Yellow) and fifth (Green) differ by ΔE 4.2, and Yellow and Lime by 1.5, so a red-green colour-blind user with three or four session tabs open sees two tabs in what looks like the same colour. A dialog list's chosen row after focus moves on, and the name and typed-confirmation fields when unfocused, draw white on #7A7574 at 3.64:1, under the 4.5:1 WCAG AA asks of text. And the Config tab sets its setup rows apart from its settings only by drawing them grey.

## What Changes

- The tab palette's order becomes Red, Blue, Green, Teal, Pink, Brown, Purple, Orange, Lime, Grey, Yellow: the same eleven colours, ordered so that Vantage's own green tab and the first five session tabs stay apart under protanopia and deuteranopia (no pair under ΔE 15). Tabs open now keep their colours; only the next free colour picked changes.
- The dialog fields' and list highlight's background becomes #606060: white text on it is 5.04:1 and the fill stands 3.11:1 from the console's background, so an unfocused field is still seen as a field.
- The Config tab gains a heading row reading Setup between the settings and the setup rows. The selection steps over it, a filter hides it, and it counts as neither a setting nor a row in the counts and the title; the setup rows stay grey.
- The tool's version rises to the next preview in the pull request that implements this.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-attach`: the palette's order is part of how a tab's colour is chosen.
- `console-layout`: dialog fields and list highlights meet the contrast the rest of the console does.
- `config-tab`: the Setup heading row.

## Impact

- **Code:** `Domain\TabPalette` (order), `Tui\Palette.FieldBackground` (value), `Application\Views\ConfigRow` (a `Heading` case), `ConfigRows`, `ListSelection` and `ListPolicy` (rows the selection steps over), `ConfigView`, `Wording` (`ConfigSetting`, `ConfigValue`, `ConfigCounts`, `ConfigColumns`), `Tui\ConfigScreenDefinition`.
- **Tests:** `OwnTabShould`'s `Green` (index 4 today), the `TabColourSelection`, `OpenTabHandler` and `RenameSessionHandler` facts that name palette indices, `ListSelectionShould`, `ConfigViewShould`, `ConfigRowsShould` and the Config wording facts.
- **Depends on:** `colour-blind-status-cues`, which introduces `Palette.FieldBackground`.
- **Users:** a session tab may get a different colour from the one the same position would have had; nothing stored changes, since a tab's registration records the colour it was given, name and hex.

## Out of scope

- New tab colours: the Okabe-Ito set was weighed and the owner chose reordering instead (seven distinct colours against six, at the cost of every colour's name and hex value).
- More than six mutually distinct tab colours, which the current eleven cannot give (the best seven of them still have a pair at ΔE 10.5); the tab's title carries the session name, and the detail pane names the colour.
- The faded TAB block's shade, which matches Windows Terminal's own unselected tab on purpose.
- Themes, a light or high-contrast palette, and screen reader work beyond words for every cue.
- Multiple Claude accounts, which is a later change.
