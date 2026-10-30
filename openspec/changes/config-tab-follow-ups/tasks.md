# Tasks

## 1. The folder picker spike (Tui)

- [ ] 1.1 A throwaway window outside the repository opens `OpenDialog` with `OpenMode.Directory` and `AllowsMultipleSelection`, styled with `Palette.Focusable`, at 120 x 36 in Windows Terminal and under `tools\HeadlessTerminal`'s `drive`. Record in this task: how it takes the schemes, whether it writes its own tab title, whether it fits, what `FilePaths` answers for one and several folders, and whether the headless terminal can drive it. Verify by the recorded captures.

## 2. Default place (Domain, Application, adapters)

- [ ] 2.1 Red: `SettingsShould` asserts `Settings.Resolve` gives New worktree when the place is missing and keeps Current branch when given, and `ToText` and `Validate` carry it. Green: `Settings.DefaultPlace` and `SettingsText.Place`. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.
- [ ] 2.2 Red: `SettingsFileShould` asserts `defaultPlace` round-trips by name and a file without it reads New worktree; `JsonFileStoreShould` asserts the property name in the written file. Green: `VantageConfig.DefaultPlace` and `SettingsFile`.
- [ ] 2.3 Red: `WizardViewShould` asserts the wizard opened with `n` starts on the setting, and a fixed folder starts on the current branch whatever it says. Green: `WizardState` reads `settings.DefaultPlace`.
- [ ] 2.4 Red: `ConfigRowsShould` asserts a Default place row after Default base, `SettingsViewShould` asserts `SettingsScope.Place` opens on it and saves the choice, and `ConfigViewShould` asserts Enter raises `EditSetting(SettingsScope.Place)`. Green: `ConfigRow.DefaultPlace`, the scope, `SettingsInput.PlaceChanged` and the reducer case.

## 3. Descriptions (Application, Wording)

- [ ] 3.1 Red: `WordingOfConfigShould` asserts a distinct, non-empty description for every `ConfigRow` case and every `SetupItem`, and the three scenarios' words. Green: `Wording.ConfigDescription`.
- [ ] 3.2 Red: `ConfigViewShould` asserts `Shown` is `Footer.SettingHint` for the selected row while the footer is blank, and that a message wins otherwise; `WordingShould` gives the hint's words. Green: the footer case and `ConfigViewState.Shown`.

## 4. The screen (Tui)

- [ ] 4.1 The place selector in `SettingsControls` for `SettingsScope.Place`, with the dialog title and caption from `Wording`. Verify headless: Enter on Default place opens it, F5 saves Current branch, and `n` then opens the wizard on Current branch.
- [ ] 4.2 The row tooltip: `IScreenDefinition.RowTip` (null for sessions and worktrees), shown by `TableBody` from the same mouse handler as the column-header tooltip. Verify headless with SGR motion over a Config row after 8000 ms.
- [ ] 4.3 `Browse… (F3)` in the roots dialog over `OpenDialog`, as the spike found it, appending each chosen folder as a line; the smoke run opens and cancels it when the spike says the headless terminal can. Verify headless, or record why the owner's check covers it.
- [ ] 4.4 README (the Config tab section: the place row, the descriptions, Browse; the wizard's Where) and CLAUDE.md (the `OpenDialog` facts, the row tooltip). Verify the README's Config rows and keys match `Wording`.

## 5. Version and checks

- [ ] 5.1 Raise `Version` one preview step above `main`'s. Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes` and `openspec validate config-tab-follow-ups --strict` pass.
- [ ] 5.2 Run the hand-off check and the smoke check against the packed tool. Verify both exit 0.
- [ ] 5.3 The product owner installs the build, adds a root with Browse, sets Default place to Current branch and opens the wizard, and reads the footer and the tooltips on several rows. Record here.

## Workflow follow-up

- Implement after config-tab has archived; rebase the `session-launch` delta on wizard-one-form's text if that lands first. Archive in the pull request that completes it.
