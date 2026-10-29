# Tasks

## 1. Settings: the preset and the words (Domain, Application, Tui)

- [x] 1.1 Red: `EditorChoiceShould.OfferNoVisualStudioPreset` asserts `Enum.GetValues<EditorPreset>()` is VsCode, Cursor, Rider, Explorer and Custom, and `SettingsFileShould.ReadAnOldVisualStudioPresetAsVsCode` reads a config.json naming `VisualStudio` and `D:\work` and asserts VS Code with `D:\work` kept. Green: remove the enum member; the build names `EditorLocator`, `EditorCommand` and `Wording.Describe`, whose Visual Studio arms and the `EditorLocatorShould` and `EditorCommandShould` cases for devenv go. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` and the Infrastructure suite pass.
- [x] 1.2 Red: `WordingOfKeysAndColumnsShould`'s expected lines read "e Open folder" for both views. Green: the `e` hint in `SessionsKeys` and `WorktreesKeys`; `SettingsControls` captions read "Open folder with (↑↓ then Space to choose)" and "Worktree root (new worktrees go here; the worktrees view lists every worktree wherever it is)". Verify the Core suite passes and the smoke script's settings step still finds its marker.

## 2. The wizard's rules (Application)

- [x] 2.1 Red: `WizardViewShould.MoveEnterThroughTheFormInOrder` asserts `NextField` goes name to Where with the chooser, name to prompt without it, Where to base, base to prompt, and prompt stays. Green: `WizardView.Next`. Verify the Core suite passes.
- [x] 2.2 Red: `WizardViewShould.StartAndOpenATabOnEnterInAFixedFoldersName` asserts `NextField` from the name with a fixed choice and a valid name yields `WizardEffect.Launch` with `OpenInTab` true, and with a blank name the required-name problem. Green: the reducer. Verify the fact passes.
- [ ] 2.3 Red: `RepositoryCatalogueShould.OfferNoRootAsAChoice` asserts the choices hold no "(no repository)" entry and recent-first ordering is unchanged. Green: drop the `noRepository` choices. Verify the Core suite passes.
- [ ] 2.4 Red: `WordingShould` asserts `WizardHint` for a fixed folder reads "Enter starts and opens in tab   F5 start without a tab   Esc cancel" and for the full wizard is unchanged. Green: the sentence. Verify the fact passes.

## 3. The wizard's controls (Tui)

- [ ] 3.1 `WizardControls.All` in form order with the form column taken from the state (48 with the chooser, 1 without); `LaunchWizardDialog` raises `NextField` from the Where and base selectors' `Accepting`, marks the dialog's own `Accepting` handled, and sizes a fixed-folder dialog to 84 x 20. Verify with `dotnet run --project tools\HeadlessTerminal -- drive` at 120 x 36: Tab from the list lands on the name, Enter on Where leaves the dialog open with focus on the base, and the `s` dialog on the seeded linked worktree shows its fields from the left edge with the new hint line.
- [ ] 3.2 Add a `SmokeScript` step that opens the fixed-folder wizard with `s` on the linked worktree and cancels it, keyed on the "New session in" title. Verify `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree>` passes locally.

## 4. The solution chooser (Tui)

- [ ] 4.1 `ChooseSolutionDialog.Show(app, folder, files)` with a `ListView` of relative paths, Enter answering the index and Esc null, titled through `DialogTabTitle` with `DialogName.OpenSolution`; `DialogEffects.ChooseSolution` calls it. Verify `DialogEffectsShould` still passes and a headless drive on a folder seeded with `Api.sln` and `Worker.sln` shows both whole, one per line, and Esc opens nothing.
- [ ] 4.2 Seed two solutions in the smoke repository in `check.yml` and add a `SmokeScript` step: `v` on the row opens the chooser (marker "Open solution"), Esc. Verify the smoke check passes locally and on the pull request.
- [ ] 4.3 The product owner opens the chooser on a real two-solution folder. Verify the tab reads "Vantage (Open solution)"; if it still reads "Vantage", record whether `IsModalChanged` fires inside `app.Invoke` and fix `DialogTabTitle` for every dialog, with the finding recorded here.

## 5. Docs

- [ ] 5.1 README: the keys table (`e` Open folder; Tab and Enter in the wizard), the wizard section (no "(no repository)"; a fixed folder's Enter), the worktrees view's `s`, the Settings section (the presets, Visual Studio reads as VS Code). Verify `grep -n "no repository\|Visual Studio" README.md` shows only the Settings sentence about old files.

## 6. Version and checks

- [ ] 6.1 Raise `Version` to `0.17.0`, dropping the preview suffix (the owner folded the release into this change). Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` all pass.
- [ ] 6.2 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal`. Verify exit 0.
- [ ] 6.3 The product owner installs the build with `tools\install-local.ps1`: `n`, Tab from the list, Enter on Where and on the base; `s` on a worktree row, a name, Enter (a tab opens); `v` on a two-solution folder; `,` to see the presets and the captions. Verify each against the spec scenarios and record here.

## Workflow follow-up

- Archive the change in the same pull request, syncing the three delta specs into `openspec\specs`.
