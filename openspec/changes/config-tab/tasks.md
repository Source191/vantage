# Tasks

## 1. The settings rules (Domain)

- [x] 1.1 Red: `SettingsShould` asserts `Validate` answers `SettingsProblem.RootAlreadyListed` for a root listed twice (compared as `FolderPath`), `WithRootRemoved` drops the root at an index keeping every other setting and answers `SettingsProblem.LastRoot` for the only one, and `WithUpdateCheckToggled` turns the check either way. Green: the check in `Validate` (so a root dialog, which validates the saved settings with its one value changed, refuses a duplicate through the same rule), the two methods and their words. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 2. The rows (Application)

- [x] 2.1 Red: `ConfigRowsShould` asserts the setting rows for the default settings, two roots and a custom editor (the three "The Config tab lists the settings" scenarios), each row's kind and key. Green: `ConfigRow` (a case per kind of row carrying its facts, its key stable across a save) and `ConfigRows.For(Settings, SetupFacts?)`.
- [x] 2.2 Red: `ConfigRowsShould` asserts the setup rows from a `SetupFacts` for the "everything set up", "nothing set up", "another channel" and "unreadable Claude settings" scenarios, and "reading…" before the facts arrive. Green: `SetupItem`, `SetupState`, `SetupFacts`; the last update check reads off while the check is off.
- [x] 2.3 Red: `SetupFactsReaderShould`, over `FakeTerminalFragment`, `FakeClaudeSettings`, `FakeUpdateCheckRecords` and a log path, asserts each fact and that a throwing port gives that fact only as unreadable. Green: `SetupFactsReader.ReadAsync` (I/O failures per fact), with `FakeClaudeSettings.FailTheReadWith`.

## 3. The tab's reducer and keys (Application)

- [ ] 3.1 Red: `ConfigViewShould` asserts Enter on a setting row raises `EditSetting` with its key, on Check for updates `ToggleUpdateCheck`, on a setup row the advice footer per kind ("A setup row says how to change it"); `a` raises `AddRepositoryRoot`; `d` on a root raises `RemoveRepositoryRoot(index)` and elsewhere the footer; `c` and `e` raise the folder effects with the full value (a file's folder for `e`). Green: `ConfigView.Reduce` over `ListSelection<ConfigRow>`, `ConfigViewState`, the effects.
- [ ] 3.2 Red: `ConsoleViewShould` asserts `NextTab` and `PreviousTab` move Sessions → Worktrees → Config and back, stopping at the ends, `SelectTab` for 1, 2 and 3, and Esc with no filter on Worktrees or Config returning to Sessions; `SessionsViewShould` and `WorktreesViewShould` assert `w` and `,` do nothing. Green: `ConsoleTab.Config`, the `ISwitchesTab` effects, the reducers' key cases; `SwitchToWorktrees`, `SwitchToSessions` and `OpenSettings` removed. Verify `EffectRoutingShould` still passes.
- [ ] 3.3 Red: `SettingsViewShould` reshaped: `SettingsState.Open` for one field (a root at an index, a new root, the worktree root, the editor, the base); Save validates that field only, refuses a duplicate root, and settles on `SaveSettings` with the whole new `Settings`. Green: the narrowed reducer; the old whole-dialog tests that no longer apply removed.

## 4. The handlers (Application)

- [ ] 4.1 Red: `RemoveRepositoryRootHandlerShould` asserts the saved settings without the root (over `InMemoryState` and `TestSettings`) and the `LastRoot` outcome for the only root. Green: `RemoveRepositoryRootHandler` and its outcome record, registered in `Composition.Handlers`.
- [ ] 4.2 Red: `ToggleUpdateCheckHandlerShould` asserts the flipped flag is saved and returned. Green: `ToggleUpdateCheckHandler` and its outcome, registered.
- [ ] 4.3 Red: `EffectDispatcherShould` and `DialogEffectsShould` pass with `EditSetting` and `AddRepositoryRoot` as dialog effects and the two handlers registered. Green: the registrations.

## 5. The words (Wording)

- [ ] 5.1 Red: `WordingOfScreensShould` asserts the one settings row (the two "The settings take one line" scenarios), `ConfigCounts` (`6 settings · 1 to set up`, the second part amber), the Config columns' `ColumnHelp`, `ConfigKeys` and the help listing 1, 2, 3, Left, Right, Shift+Left and Shift+Right without w or ,. Green: `SettingsRows` down to one row, the Config words, the key lists.
- [ ] 5.2 Red: `WordingShould` asserts the Config bar (on a root row and elsewhere) and that every tab's bar fits 120 columns for every row kind; `WordingOfOutcomesShould` the remove and toggle outcomes and the advice footers. Green: the words.

## 6. The screen (Tui)

- [ ] 6.1 `ConfigScreenDefinition` and a third `TableBody` in `ConsoleScreen` (Config tab, setup facts loaded when shown and after each save, no refresh timer), Left and Right left to the window as tab keys, Shift+Left and Shift+Right to `TableBody.Scroll`. Verify with a headless drive at 120 x 36 pressing Right twice: the Config tab is shown with its rows and counts, and Left goes back.
- [ ] 6.2 The per-setting dialog: `SettingsDialog` and `SettingsControls` narrowed to the field being edited (the editor dialog keeps the preset choice and the custom fields), routed from `DialogEffects` for `EditSetting` and `AddRepositoryRoot`. Verify headless: Enter on Worktree root opens it with the value, Esc closes it unchanged, a relative path is refused in the dialog.
- [ ] 6.3 One settings row: `VantageIcon.Height` 1 with `Fit` sizing from the cell aspect (clamped 1 to 3), `StripTop` 1. Verify headless that the strip starts on row 1 and every table gains a row; the icon is checked in the owner's terminal.
- [ ] 6.4 The smoke script's settings step opens the Config tab and an edit dialog and cancels it, taking its keys and markers from `Wording`. Verify the smoke check exits 0 against the Release build.
- [ ] 6.5 README (the screen, the keys tables without w and , and with 3, Left, Right, Shift+Left and Shift+Right, the settings section rewritten for the Config tab), CONTRIBUTING.md wherever it names the settings dialog or the smoke steps, and CLAUDE.md (the console paragraph, the settings dialog references, the smoke check's steps, the icon and `Tabs` facts). Verify the README's keys tables match `Wording`'s key lists.

## 7. Version and checks

- [ ] 7.1 Raise `Version` one preview step above `main`'s. Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes` and `openspec validate config-tab --strict` pass.
- [ ] 7.2 Run the hand-off check (`dotnet run -- check` from `tools\HeadlessTerminal`) and the smoke check against the packed tool. Verify both exit 0.
- [ ] 7.3 The product owner installs the build with `tools\install-local.ps1` and moves between the tabs with Left, Right, 1, 2 and 3, reads the Config rows and counts, edits the worktree root and cancels, adds and removes a root, toggles the update check, reads the advice on a setup row, scrolls columns with Shift+Left and Shift+Right on a narrow window, and checks the one-row icon. Record here.

## Workflow follow-up

- Archive after tabs-and-key-bar has archived (and the changes it waits for), rebasing the `console-layout` and `session-listing` deltas on the text it leaves; then rebase detail-pane's "Long names never hide columns" on this change's Shift+Left and Shift+Right.
