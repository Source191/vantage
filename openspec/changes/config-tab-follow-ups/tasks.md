# Tasks

## 1. The folder picker spike (Tui)

- [x] 1.1 A throwaway window outside the repository opens `OpenDialog` with `OpenMode.Directory` and `AllowsMultipleSelection`, styled with `Palette.Focusable`, at 120 x 36 in Windows Terminal and under `tools\HeadlessTerminal`'s `drive`. Record in this task: how it takes the schemes, whether it writes its own tab title, whether it fits, what `FilePaths` answers for one and several folders, and whether the headless terminal can drive it. Verify by the recorded captures.
  - Recorded 30 October 2026 (Terminal.Gui 2.5.0), a scratch app outside the repository driven by `tools\HeadlessTerminal`'s `drive` at 120 x 36 over a folder holding `alpha`, `beta` and `gamma`:
  - **Type:** `OpenDialog` derives from `FileDialog` and `Dialog<IReadOnlyList<string>>`, not from the plain `Dialog`, so `DialogTabTitle.Apply` has to take a `Runnable`.
  - **Tab title:** it has `BorderSettings.TerminalTitle` set and writes its own title (OSC 0 with the dialog's `Title`) when it becomes modal, so it needs `DialogTabTitle.Apply` like every other dialog.
  - **Schemes:** `SetScheme(Palette.Focusable(...))` reaches most of it (white on the console's black, the cyan focus, the grey active row); some parts keep Terminal.Gui's own grey on black. The owner's check in Windows Terminal judges the look.
  - **Fit:** it opens at 96 x 28 in a 120 x 36 terminal (32 rows once a row is selected), with the path box, the up button, the table of folders, the Find box, Tree, Cancel and `Select folder`.
  - **Keys and `FilePaths`:** focus starts in the path box; Tab moves into the table, Down moves the selection (the path box follows it), Shift+Down extends it, and Enter in the table confirms (it does not open the folder). One folder: `FilePaths` is `[alpha]` and `Path` the same. Two folders by Shift+Down: `FilePaths` is `[alpha, beta]` in order and `Path` is empty. Space does not toggle a row. Esc: `Canceled` is true and `FilePaths` is empty.
  - **Headless:** the headless terminal drives it, so the smoke run can open and cancel it. What Windows Terminal shows (colours, the Sixel icon behind it) is left to the owner's check, task 5.3.

## 2. Default place (Domain, Application, adapters)

- [x] 2.1 Red: `SettingsShould` asserts `Settings.Resolve` gives New worktree when the place is missing and keeps Current branch when given, and `ToText` and `Validate` carry it. Green: `Settings.DefaultPlace` and `SettingsText.Place`. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.
- [x] 2.2 Red: `SettingsFileShould` asserts `defaultPlace` round-trips by name and a file without it reads New worktree; `JsonFileStoreShould` asserts the property name in the written file. Green: `VantageConfig.DefaultPlace` and `SettingsFile`.
- [x] 2.3 Red: `WizardViewShould` asserts the wizard opened with `n` starts on the setting, and a fixed folder starts on the current branch whatever it says. Green: `WizardState` reads `settings.DefaultPlace`.
- [x] 2.4 Red: `ConfigRowsShould` asserts a Default place row after Default base, `SettingsViewShould` asserts `SettingsScope.Place` opens on it and saves the choice, and `ConfigViewShould` asserts Enter raises `EditSetting(SettingsScope.Place)`. Green: `ConfigRow.DefaultPlace`, the scope, `SettingsInput.PlaceChanged` and the reducer case.

## 3. Descriptions (Application, Wording)

Moved to detail-pane at the owner's check (30 October 2026) and delivered there; nothing to do here.

- 3.1 (moved) Red: `WordingOfConfigShould` asserts a distinct, non-empty description for every `ConfigRow` case and every `SetupItem`, and the three scenarios' words. Green: `Wording.ConfigDescription`.
- 3.2 (moved) Red: `ConfigViewShould` asserts `Shown` is `Footer.SettingHint` for the selected row while the footer is blank, and that a message wins otherwise; `WordingShould` gives the hint's words. Green: the footer case and `ConfigViewState.Shown`.

## 4. The screen (Tui)

- [x] 4.1 The place selector in `SettingsControls` for `SettingsScope.Place`, with the dialog title and caption from `Wording`. Verify headless: Enter on Default place opens it, F5 saves Current branch, and `n` then opens the wizard on Current branch.
  - Recorded 30 October 2026, `drive` at 120 x 36 against the Debug build (the owner's config.json backed up first and restored byte for byte after): Enter on Default place opened "Where the wizard opened with n starts a session (↑↓ then Space)" over ○ Current branch and ◉ New worktree; Space then F5 wrote `"defaultPlace": "CurrentBranch"`, and `n` then opened the wizard with ◉ Current branch. Terminal.Gui's \OptionSelector\ starts its cursor on the first option whatever is chosen, so Space alone picks Current branch and Up wraps to New worktree, as the base and editor selectors already do.
- 4.2 (moved to detail-pane) The row tooltip: `IScreenDefinition.RowTip` (null for sessions and worktrees), shown by `TableBody` from the same mouse handler as the column-header tooltip. Verify headless with SGR motion over a Config row after 8000 ms.
- [x] 4.3 `Browse… (F3)` in the roots dialog over `OpenDialog`, as the spike found it, appending each chosen folder as a line; the smoke run opens and cancels it when the spike says the headless terminal can. Verify headless, or record why the owner's check covers it.
  - Recorded 30 October 2026, `drive` at 120 x 36 against the Debug build, nothing saved: Enter on a root, then F3, opened the picker in the folder holding the first root (`SettingsState.BrowseFrom`); Tab, Down, Down and Enter chose `repos`, and the roots box read the two roots with `C:\Users\SimonVane\source\repos` on a third line (already a root, so F5 would refuse it). The tab title went `Vantage (Settings)`, `Vantage (Browse)`, `Vantage (Settings)`, `Vantage`. The smoke script now opens the picker with F3 and cancels it (`SmokeScriptShould`), run in 5.2.
- [x] 4.5 Browse in the worktree root dialog (added at the owner's check, 31 October 2026). Red: `SettingsViewShould` asserts `FoldersChosen` in `SettingsScope.WorktreeRoot` replaces the field with the folder chosen and leaves it for none, and `BrowseFrom` there is the folder holding the worktree root typed. Green: the reducer and `BrowseFrom` by scope. Then the button and F3 in that dialog over a single-selection `OpenDialog`, README's Worktree root line and CLAUDE.md's picker fact. Verify headless that F3 opens the picker beside the worktree root and a folder chosen replaces the box with nothing saved, then re-run 5.1's and 5.2's checks.
  - Recorded 31 October 2026, `drive` at 120 x 36 against the Debug build, nothing saved (config.json's hash unchanged): Enter on Worktree root, then F3, opened the picker titled "Choose the worktree root" in `C:\Users\SimonVane\source`; Tab, Down, Down and Enter chose `repos`, and the box read `C:\Users\SimonVane\source\repos` in place of the worktree root. The tab title went `Vantage (Settings)`, `Vantage (Browse)`, `Vantage (Settings)`. The checks are re-run with 4.6 and 4.7.
- [x] 4.6 Visual Studio in place of Cursor (the owner's check, 31 October 2026). Red: `SettingsFileShould` asserts a file naming Cursor reads as VS Code and VisualStudio as Visual Studio, `EditorLocatorShould` that Visual Studio is `vswhere`'s `productPath` when that file exists and nothing otherwise, and the Wording facts that the dialog lists VS Code, Visual Studio, Rider, Explorer and Custom. Green: `EditorPreset.VisualStudio` in Cursor's place, the locator over `IProcessRunner` again, the words. README's Open folder with line and CLAUDE.md. Verify headless that the dialog lists the five, then re-run 5.1's and 5.2's checks.
  - Recorded 31 October 2026, `drive` at 120 x 36 against the Debug build, nothing saved: Enter on Open folder with listed ○ Visual Studio and no Cursor; `vswhere -latest -property productPath` on the owner's machine answers `C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\IDE\devenv.exe`.
- [x] 4.7 Say what the default base means (the owner's check, 31 October 2026: the tooltip left unclear whether origin's default branch is fetched first; it is not, the remote-tracking ref is used as last fetched). Red: `WordingOfConfigShould` asserts the Default base description names the last fetch and that no fetch is made, and `Wording.Describe(WorktreeBase)` gives `origin's default branch, as last fetched` and `the commit checked out now (HEAD)`. Green: the words, used by the Config value, the base dialog and the wizard's selector. README's base lines.
  - Recorded 31 October 2026, in the same run: the Default base dialog and the wizard's base selector read "origin's default branch, as last fetched" and "the commit checked out now (HEAD)". Vantage never fetches: `LaunchService` branches from `git symbolic-ref refs/remotes/origin/HEAD` (HEAD when unset).
- [x] 4.4 README (the Config tab section: the place row, Browse; the wizard's Where) and CLAUDE.md (the `OpenDialog` facts). Verify the README's Config rows and keys match `Wording`.

## 5. Version and checks

- [x] 5.1 Raise `Version` one preview step above `main`'s. Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes` and `openspec validate config-tab-follow-ups --strict` pass.
  - 0.18.0-preview.11, two steps above `main`'s preview.9: this branch is built on wizard-one-form's, which still reads preview.9 and needs its own step, and tab-swatch-shade holds preview.10. Whichever of the three merges after another raises its version again.
- [x] 5.2 Run the hand-off check and the smoke check against the packed tool. Verify both exit 0.
  - Done 30 October 2026: the hand-off check exits 0 (throwaway session stopped and removed), and the smoke check exits 0 against 0.18.0-preview.11 packed and installed into a temporary tool path, opening and cancelling the folder picker from the roots dialog among its 26 steps; Northwind.Smoke and its worktree were seeded as CI seeds them and removed after.
  - Re-run 31 October 2026 after main's merge and tasks 4.5 to 4.7: the hand-off check exits 0 and the smoke check exits 0 against a fresh Release pack in a temporary tool path, Northwind.Smoke seeded and removed.
- [ ] 5.3 The product owner installs the build, adds a root with Browse, browses for the worktree root, opens a folder in Visual Studio with e, sets Default place to Current branch and opens the wizard. Record here.

## Workflow follow-up

- Implement after config-tab has archived; rebase the `session-launch` delta on wizard-one-form's text if that lands first. Archive in the pull request that completes it.
