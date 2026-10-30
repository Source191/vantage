# Design

## Context

After config-tab the settings are rows of the Config tab (`ConfigView` over `ConfigRow`s), each edited in a dialog scoped by a `SettingsScope` and bound by `SettingsDialog` and `SettingsControls`; every repository root row opens `SettingsScope.Roots`, a multi-line box with one root per line that `Settings.Validate` checks on F5. The wizard's Where choice is a `LaunchMode` that `WizardState` opens on `NewWorktree` unless the wizard has a fixed folder; the base already opens on `Settings.DefaultBase`. Column-header tooltips are shown by hand from `TableView.MouseEvent` with `ScreenToCell`, because a table raises no position reports unless `MousePositionTracking` is on. Terminal.Gui 2.5 ships `OpenDialog` (over `FileDialog`) with `OpenMode.Directory`, `AllowsMultipleSelection` and `FilePaths`; it has not been run in Vantage yet.

## Goals / Non-Goals

**Goals:**
- Each addition follows the pattern already there: a setting is a `Settings` property, a `SettingsFile` property, a `ConfigRow` case and a `SettingsScope`; text lives in `Wording`; the picker is one more control of the roots dialog.

**Non-Goals:**
- A picker anywhere but the roots dialog (the worktree root and a custom editor stay typed).
- Remembering the place last used instead of a setting.

## Decisions

- **The picker adds lines; F5 still decides.** `OpenDialog` answers folder paths and the binder appends them to the roots box, so validation, duplicates and saving stay in `SettingsView` and `Settings.Validate`. Alternative: save straight from the picker; rejected, it would bypass the one place roots are checked.
- **A spike before the picker is built.** It answers how `OpenDialog` takes `Palette`'s schemes, whether it sets the tab title itself (`BorderSettings.TerminalTitle`, so `DialogTabTitle.Apply` is needed), whether it fits 120 x 36, and whether the headless terminal can drive it for the smoke run. Its findings go in CLAUDE.md's Terminal.Gui facts.
- **The default place is a domain value.** `Settings.DefaultPlace` holds a `LaunchMode` (or a domain enum of the same two names if `LaunchMode` cannot move below Application, which the layering test decides), written by name as `defaultPlace`. `WizardState` opens on it when there is no fixed folder.
- **Descriptions are words keyed by row.** `Wording.ConfigDescription(ConfigRow)` (a switch over the cases and `SetupItem`), a `Footer` standing case (`Footer.SettingHint`) that `ConfigViewState.Shown` returns for the selected row when the footer is blank, and a row tooltip in `TableBody` that asks the definition for an optional row tip (`IScreenDefinition.RowTip`, null for the sessions and worktrees tables), shown the way the column-header tooltip is.

## Risks / Trade-offs

- [`OpenDialog` may not suit the console's look or size] → the spike decides before any code; the fallback is to keep typing paths, and the change records why.
- [Row tooltips need mouse position tracking on the table, which the header tooltip already turns on] → the same handler serves both, so no second tracking path.
- [wizard-one-form reshapes the wizard] → whichever lands second rebases its `session-launch` delta on the other's text.

## Migration Plan

`defaultPlace` is a new optional property: older files read as New worktree, and an older Vantage ignores the property. Rolling back is installing the earlier release.

## Open Questions

- Whether `OpenDialog` can be driven by the headless terminal for the smoke run (the spike answers it; if not, the smoke run skips the picker and the owner's terminal check covers it).
