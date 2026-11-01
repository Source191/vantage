# Design

## Context

See proposal.md for why. What shapes the approach:

- **Colour choice:** `Domain\TabColourSelection` takes `TabPalette.Default` and picks the first colour no live tab uses (least used when all are), and `Application\Actions\OwnTab` prefers the colour named Green for Vantage's own tab, found by name, not index. A registration stores the `TabColour` it was given (name and hex) in its state file, so reordering the palette moves no existing tab.
- **Measured order** (Machado 2009 at full severity in linear RGB, CIE76 ΔE, the worst pair across normal vision, protanopia and deuteranopia): of the current eleven, the best set of six including Green is Red, Blue, Green, Teal, Pink, Brown, worst pair 15.0 (Brown and Pink); the best seven fall to 10.5. Tritanopia, about one person in ten thousand, was measured and not optimised for. The remaining five are ordered by the same greedy rule, then Lime and Grey swapped so Lime (ΔE 1.5 from Yellow under protanopia) is not Yellow's neighbour.
- **Field background:** after `colour-blind-status-cues`, `Tui\Palette.FieldBackground` is the only background behind white field text and the chosen-row highlight (`Focusable`'s Active role, `NameDialog`, `TypedConfirmationDialog`). Measured against white #E6E6E6 and the console's #0C0C0C: #5E5E5E gives 5.20:1 and 3.02:1, #606060 5.04:1 and 3.11:1, #646464 4.74:1 and 3.31:1.
- **Config rows:** `ConfigRow` is an open record hierarchy behind one `ListSelection<ConfigRow>`; `ConfigRows.For` builds the settings then the setup; `ConfigCounts` counts `row is not ConfigRow.Setup` as settings; `ListSelection.Move` clamps an index into `Visible`, `Load` keeps the key or the nearest index, and `Title` counts `Visible` and `Rows`.

## Goals / Non-Goals

**Goals:**

- The colours a user sees together most often (Vantage's tab and the first few sessions) stay apart for red-green colour blindness.
- Field and highlight text passes 4.5:1, and the field's fill passes 3:1 against the console's background.
- The Config tab's two parts read apart without colour.

**Non-Goals:**

- A heading row anywhere but the Config tab: the policy can say which rows the selection skips, but no other view gets one.
- Recolouring tabs already open.

## Decisions

### 1. Reorder, keep the colours

`TabPalette.Default` becomes Red, Blue, Green, Teal, Pink, Brown, Purple, Orange, Lime, Grey, Yellow; names and hex values are unchanged. Its summary comment changes from "neighbouring colours contrast well" to say the first six stay apart for protanopia and deuteranopia. A `TabPaletteShould` fact pins the order, so a later reshuffle has to change it on purpose.

- **Alternative, the Okabe-Ito set:** offered and declined by the owner (see proposal).
- **Alternative, dropping Lime or Yellow:** rejected; with eleven tabs open a user would hit the least-used warning one tab sooner, and those two are last, where they matter least.
- **Alternative, a fact that computes ΔE:** rejected as more than the change needs; the figures are recorded here and the order is pinned.

### 2. One value for the field background

`Palette.FieldBackground` becomes `new(96, 96, 96)` (#606060): the middle of the three measured, with margin on both ratios. Nothing else changes, since the first change gave the three uses one name.

### 3. A heading is a row the selection steps over

`ConfigRow.Heading(string Key)` (one, key `heading:setup`) sits between `SettingRows` and `SetupRows` in `ConfigRows.For`. `ListPolicy<TRow>` gains `Func<TRow, bool> Selectable`, true for every row in every view but Config's heading; `ListSelection` uses it so that:

- `Move` steps from the selected row to the next selectable row in the direction moved, staying put at either end;
- `Load` keeps the selected key or picks the nearest selectable row, never the heading;
- `Title` counts only selectable rows in `Shown` and `Total`;
- `Select(key)` of an unselectable row leaves the selection as it is, and `Reselect` (after a filter is cleared or edited) goes through `Load`, so neither can land on the heading. Today only `SessionsView`'s jump to a session calls `Select`, and a click on a table selects nothing, but the rule belongs to the selection, not to its callers.

The heading's filter fields are empty, so any filter text hides it, with no special case. `Wording.ConfigSetting` names it Setup and `ConfigValue` answers empty, each through a case of its own (both have a `row.ToString()` fallback arm that would otherwise print the record); `ConfigDescription`'s empty fallback already suits it, so its hover tip is blank; `SettingsScope.For` throws for it, which is unreachable while it cannot be selected; `ConfigCounts` counts settings as rows that are neither `Setup` nor `Heading`; `ConfigView`'s Enter, c and e never see it because it is never selected; `ConfigColumns`' SETTING meaning says "under the Setup heading" instead of "in grey". `ConfigScreenDefinition.RowColour` draws the heading in `Palette.Orange` with the settings row's labels, the console's colour for a label.

- **Alternative, a selectable heading whose keys explain themselves:** rejected; the owner asked for a heading the selection does not rest on, and every key on it would need a refusal message.
- **Alternative, drawing the heading in the table's frame:** rejected; Terminal.Gui 2.5's `TableView` has no section rows, and a view laid over the table would scroll apart from it.
- **Alternative, a `Selectable` flag on the row record:** rejected; rows are facts for `Wording`, and which rows can be selected is the view's policy, beside `KeyOf` and `FilterFields`.

## Risks / Trade-offs

- [Selection rules in `ListSelection` are shared by every view] → `Selectable` defaults to true through each existing policy, and the existing `ListSelectionShould` facts must pass unchanged before the new ones are added.
- [The heading's colour is the one cue that it is a heading] → its word, Setup, and the empty VALUE carry it too; it cannot be selected, so nothing acts on it if it is mistaken for a row.
- [Users used to a colour's position notice the change] → the release notes say the palette was reordered for colour-blind users; colours of open tabs do not change.

## Migration Plan

None: registrations already store the colour each tab was given.
