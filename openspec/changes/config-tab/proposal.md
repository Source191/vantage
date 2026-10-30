# Proposal

## Why

The settings live in a dialog behind `,` that shows nothing until it is opened, while their values take the console's second row, where several repository roots are cut to an ellipsis. Whether Vantage is set up fully (the Windows Terminal profiles, Claude's terminal bell, the diagnostic log, the last update check) is shown nowhere at all. A third tab gives the settings and that setup a table of their own, edited a row at a time, and gives the table back a row (the owner's request, 30 October 2026, after tabs-and-key-bar).

## What Changes

- **A Config tab,** after Sessions and Worktrees: a table with a row per setting (each repository root, the first labelled Repository roots and the others beneath it, the worktree root, what opens a folder, the default worktree base, the update check) and a row per piece of setup Vantage reads but does not edit (the settings file, the Windows Terminal profiles, Claude's terminal bell, the diagnostic log, the last update check). The setup rows are grey; a setup row that needs attention says what to run.
- **Edited a row at a time:** Enter on a setting opens a small dialog for that one value with the same checks the settings dialog makes now, and saving writes `config.json` and applies at once; Enter on any repository root opens one dialog holding every root, one per line, where roots are added, changed and removed (never the last); Enter on the update check turns it on or off. Enter on a setup row says how to change it. `c` copies the selected value, `e` opens the selected folder, `/` filters, as on the other tabs.
- **The counts beside the tabs** say how many settings there are and how much setup is missing (`9 settings · 1 to set up`).
- **Left and Right move between the tabs;** `1`, `2` and `3` choose one, a click on a title selects it, and Esc on Worktrees or Config with no filter goes back to Sessions. **BREAKING** (pre-1.0 minor): `w` and `,` go, the settings dialog goes with `,`, and scrolling a table's columns on a terminal too narrow for them moves from Left and Right to `<` and `>` (Shift+Left and Shift+Right, the first choice, did not reach Vantage in Windows Terminal).
- **One settings row:** the repository roots and worktree root leave the top of the screen for the Config tab, the versions row stays, the Vantage icon shrinks to one row and the tabs move up, so every table gains a row.
- **Version:** one preview step above `main`'s.

Out of scope: running `vantage setup-terminal` or switching Claude's bell on from the tab (the rows say what to run), editing Claude Code's own settings, more settings than `config.json` holds now, a settings form inside the tab, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

- `config-tab`: the Config tab's rows, what each shows, how a row is edited, added or removed, the setup rows and their advice, and its counts and keys.

### Modified Capabilities

- `settings`: "Settings can be edited from either view" becomes editing on the Config tab; "Settings are validated before they are saved" checks the one value being saved and refuses removing the last root; "Saved settings apply without a restart" shows the result on the Config tab rather than the header; "The editor is a preset or a custom command" names the row and the edit dialog rather than the settings dialog.
- `console-layout` (added by tabs-and-key-bar, not yet archived): "Sessions and worktrees are two tabs of one screen" gains the Config tab, Left and Right, and loses `w`; "The settings take two lines" becomes one line; "The Vantage icon heads the settings" shrinks the icon to one row; "The key bar names the keys for the selected row" drops `w` and `,` from the second row and adds the Config tab's bar; "One console keeps its state across hand-offs" switches with Left and Right in its scenario.
- `session-listing`: "Long names never hide columns" (as tabs-and-key-bar leaves it) scrolls columns with `<` and `>`.

## Impact

- **Domain:** `Settings` gains the rules for adding and removing one root (no duplicate, never the last).
- **Application:** a `ConfigView` reducer over `ListSelection<ConfigRow>`, a `ConfigRow` read model built from `CurrentSettings`, `ITerminalFragment`, `IClaudeSettings`, the diagnostic log's path and `UpdateCheck`'s record; effects and handlers for editing one setting, adding and removing a root and toggling the update check; `SettingsView` narrowed to one field per dialog; `ConsoleTab.Config`, `ConsoleView`'s Left, Right and `3`; Wording for the rows, counts, bar, help, tooltips and outcomes; `SettingsRows` down to one row; `w` and `,` out of the key lists.
- **Tui:** a third `TableBody` and screen definition, a small per-setting dialog in place of `SettingsDialog`, `VantageIcon` one row high, the tab strip one row higher, `<` and `>` for column scrolling.
- **Tests:** `ConfigViewShould`, handler tests per effect, `SettingsViewShould` reshaped, `ConsoleViewShould` (Left, Right, `3`, Esc), Wording facts (rows, counts, bar fits 120), `EffectDispatcherShould`, `DialogEffectsShould` and `EffectRoutingShould` cover the new effects; the smoke script's settings step opens the Config tab and an edit dialog instead.
- **Docs:** README (the screen, the keys tables, the settings section), CLAUDE.md (the console paragraph, the settings dialog references, the icon fact), the specs.
- **Release:** a preview raise; the hand-off check, the smoke check and the owner's terminal check.

## Order

Applied first of the three remaining UI changes (owner's decision, 30 October 2026, revising the earlier order), before wizard-one-form and detail-pane; it archives after tabs-and-key-bar, since its deltas build on `console-layout` and on tabs-and-key-bar's text of "Long names never hide columns". Detail-pane's delta of that requirement then rebases on this change's text (`<` and `>`).
