# Proposal

## Why

The owner's review of the Config tab (30 October 2026, after #117) found three gaps: a repository root has to be typed or pasted as a full path, the wizard's Where choice always opens on New worktree although the worktree base has a setting for its default, and a setting's row says what it is set to but never what it does. Proposed now and implemented later; this pull request holds the plan only.

## What Changes

- **Browse for a root:** the repository roots dialog gains `Browse… (F3)`, which opens Terminal.Gui's own folder picker (`OpenDialog` in folder mode, several folders at once) inside the terminal; each folder chosen is added as a new line in the roots box, and F5 checks the list as it does now. Esc in the picker adds nothing.
- **Default place:** a new setting, New worktree or Current branch, kept in `config.json` as `defaultPlace` and shown on the Config tab after Default base; the wizard opened with `n` starts on it, as it starts on the default base. `s` on a worktree and `vantage here` stay on the current branch, and a file without the property reads as New worktree.
- **Each setting describes itself** (moved to detail-pane at the owner's check, 30 October 2026, and delivered there).
- **Version:** one preview step above `main`'s when implemented; this documentation pull request changes no version.

Out of scope: Windows' own folder picker (a separate window that can open behind Windows Terminal and that the headless checks cannot drive), a default for the opening prompt or the open-in-tab choice, editing a setup row, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None: the Config tab's capability arrives with config-tab, and this change adds to it.

### Modified Capabilities

- `config-tab` (added by config-tab, not yet archived): ADDED "The repository roots dialog browses for folders" and "Default place is a setting row".
- `settings`: "Settings live in one file with defaults" gains the default place and its default.
- `session-launch`: "A new worktree is the default launch mode" opens the wizard on the default place.

## Impact

- **Domain:** `Settings` and `SettingsText` gain `DefaultPlace` (reusing `LaunchMode` or a domain twin of it), resolved to New worktree when missing.
- **Ports and adapters:** `VantageConfig` gains `DefaultPlace`; `SettingsFile` reads and writes `defaultPlace` (enum by name, as `defaultBase` is).
- **Application:** `ConfigRow.DefaultPlace`, `SettingsScope.Place`, `SettingsInput.PlaceChanged`, `WizardState` opening on the setting; the dialog title and the Browse button's words.
- **Tui:** a `Browse… (F3)` button and its flow in `SettingsDialog` over `OpenDialog`; the place selector in `SettingsControls`.
- **Tests:** `SettingsShould`, `SettingsFileShould`, `ConfigRowsShould`, `ConfigViewShould`, `SettingsViewShould`, `WizardViewShould`, Wording facts for every description; the smoke run opens and cancels the picker.
- **Docs:** README (the Config tab section, the wizard's Where), CLAUDE.md (the picker and row-tooltip facts), the specs.

## Order

Implemented after config-tab has archived (its ADDED requirements land in `openspec\specs\config-tab`); independent of tab-swatch-shade, wizard-one-form and detail-pane, though wizard-one-form touches the same wizard and the default place should be rebased onto whatever it leaves.
