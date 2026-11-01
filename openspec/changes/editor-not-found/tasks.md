# Tasks

Each task is one red-green-refactor cycle (or smaller), with the test named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`, or the Infrastructure tests where the task says so.

## 1. Facts to check first

- [x] 1.1 Spike: from `pwsh`, run `OpenFileScript`'s start on a temporary file with an extension nothing is associated with (such as `.vantagenoapp`) and record the exception type and `NativeErrorCode` reached (1155 expected). Record the fact with its date and Windows build in `.claude\rules\windows.md`; if the code is not 1155, use the recorded one in 2.4. Verify the rule file holds the fact.
  Done, and the expectation was wrong: the start does not fail. It returned without an exception and Windows opened its OpenWith picker. `AssocQueryString` (ProgID, `ASSOCF_INIT_IGNOREUNKNOWN`) answers 0x80070483 (1155) for the unassociated type and S_OK for .cmd, .bat, .txt, .sln and .slnx; recorded in `windows.md`, and 2.4 asks it instead of reading an exit code.
- [x] 1.2 Spike: in Terminal.Gui 2.5.0, `OpenDialog` with `OpenMode.File`, an `.exe` filter through `AllowedTypes`, a starting `Path` and one file chosen: confirm what `FilePaths` and `Path` answer, whether the filter hides other files, and what Esc leaves. Record it beside the folder-picker fact in `.claude\rules\terminal-gui.md`. Verify the rule file holds the fact.
  Done from the 2.5.0 assembly, decompiled with ilspycmd (`OpenDialog`, `FileDialog`, `FileDialogState`, `AllowedType`, `SelectorBase`); the rule says which parts were read rather than observed. `OptionSelector.Labels` rebuilds every option, so 4.3 retitles the options instead.

## 2. Domain and adapters

- [x] 2.1 `EditorChoiceShould.RefuseAnExecutableThatDoesNotExist` and `SettingsShould.RefuseACustomEditorWhoseExecutableIsMissing`: `Settings.Validate` takes `fileExists` and a missing full-path `.exe` is `SettingsProblem.ExecutableMissing(path)`, after the full-path and batch-file rules. Red, then green; `SettingsView` maps the problem to `SettingsField.Executable`, `SettingsState` carries `FileExists`, and `SettingsFile` still reads a missing executable as Custom (`SettingsFileShould.KeepACustomEditorWhoseExecutableIsMissing`). Words in `Wording` with a fact in `WordingShould`. Verify the facts pass.
  Done (red folded into green: the new parameter would not build). The missing file is checked last, after the placeholder too, so the "No placeholder" scenario holds whether or not its executable exists; `EditorChoice.Custom` and `FromFile` pass no existence check. Core tests: 1981 passed.
- [x] 2.2 `EditorLocatorShould.FindRiderInTheUserProgramsFolder` and `...ChooseTheNewestRiderAcrossEveryInstallFolder`: `EditorFolders.UserPrograms` (`%LOCALAPPDATA%\Programs`), and `NewestRider` adds each child holding `bin\rider64.exe`. Red, then green. Verify in `test\Source191.Vantage.Infrastructure.Tests` with a temporary folder as the root.
  Done in `Core.Tests\Application\EditorLocatorShould`, where the locator's other facts already run over `FakeFolders`, rather than the Infrastructure tests with a temporary folder. `UserPrograms` is computed from `LocalAppData`, and VS Code's user install now uses it too. Core tests: 1984 passed.
- [x] 2.3 `DesktopShould.ThrowProgramNotStartedWhenWindowsRefusesTheProgram`: `Ports\ProgramNotStartedException` (its own file), thrown by `Desktop.StartAsync` for a `Win32Exception`, carrying Windows' reason. Red, then green. Verify in the Infrastructure tests with a file that exists but is not a program.
  Done: the reason is Windows' sentence for the native code (`new Win32Exception(code).Message`), since .NET's message also names the path and working folder. A `.txt` started without the shell fails with no dialog. `DesktopShould`: 12 passed.
- [x] 2.4 `OpenFileScriptShould.ExitTwoWhenNoApplicationOpensTheFile` (real `pwsh`, the temporary unassociated file from 1.1) and `DesktopShould.ThrowNoApplicationForExitTwo`: the script exits 2 for the recorded native error, `Desktop.OpenAsync` throws `Ports\NoApplicationException(extension)`, exit 1 still throws as now. Red, then green. Verify in the Infrastructure tests.
  Done differently, because 1.1 found no error to read: `Adapters\Processes\FileAssociations.NoneFor` asks `AssocQueryString` (one `[LibraryImport]`) and `Desktop.OpenAsync` throws `NoApplicationException(extension)` before starting `pwsh` when the answer is exactly no association, only for a full file path; `OpenFileScript` is unchanged (exit 1). Facts: `FileAssociationsShould` (real Windows data, opens nothing), `DesktopShould.ThrowNoApplicationForAnUnassociatedType` (nothing handed to the runner) and `NeverAskAboutTheTypeOfAnAddress`; the fake-runner `DesktopShould` facts now open a `.txt`, which every runner associates. 20 passed.

## 3. Handlers and their words

- [x] 3.1 `OpenInEditorHandlerShould.ReportACustomEditorWhoseExecutableIsMissing`: a Custom editor with no file in the fake folders answers `EditorNotFound` and the fake desktop starts nothing. Red, then green. Verify the fact passes.
  Done; `StartACustomEditorFromItsOwnExecutable` now puts the file in the fake folders. Core tests: 1985 passed.
- [x] 3.2 `OpenInEditorHandlerShould.ReportAnEditorThatWouldNotStart`: the fake desktop throws `ProgramNotStartedException`, and the outcome is `OpenInEditorOutcome.EditorNotStarted(editor, reason)` (its own nested record). Red, then green. Verify the fact passes.
  Done; `FakeDesktop.StartFailure` added. The new case's words and tone (3.4's) came in the same green commit, since the reflection facts fail a case with none. Core tests: 1990 passed.
- [ ] 3.3 `OpenSolutionHandlerShould.ReportATypeNoApplicationOpens`: the fake desktop throws `NoApplicationException(".slnx")`, and the outcome is the solution outcome's no-application case (the outcome becoming a closed hierarchy with `Opened`). Red, then green. Verify the fact passes.
- [ ] 3.4 `WordingOfOutcomesShould`: `EditorNotFound` reads "Could not find Rider; choose another in Open folder with on the Config tab (3).", `EditorNotStarted` names the editor and reason and the same pointer, and the no-application case names `.slnx`; all three are Failure in `StatusTonesShould`. Red, then green. Verify the facts pass and no text mentions the `,` key (`WordingShould` fact `NeverOfferTheCommaKey` over every outcome word).

## 4. The Open folder with dialog

- [ ] 4.1 `SettingsViewShould.MarkThePresetsNotFound`: the editor dialog opens with `PresetSearch.NotYetLooked`, and `SettingsInput.PresetsLocated` sets `Looked(missing)`; saving a missing preset is valid. `WordingShould`: `Describe(EditorPreset.Rider, Looked({Rider}))` reads "Rider (not found)", Explorer and Custom never marked. Red, then green. Verify the facts pass.
- [ ] 4.2 `SettingsViewShould.BrowseForAFileOnlyWhileCustomIsChosen` and `...ReplaceTheExecutableWithTheFileChosen`: `SettingsBrowse.OneFile` while Custom is chosen, `No` for a preset; `SettingsInput.FileChosen` replaces `Text.Executable`, an empty answer changes nothing; the picker starts in the folder of a full-path executable typed. Red, then green. Verify the facts pass.
- [ ] 4.3 `Tui\SettingsDialog` and `SettingsControls`: the four lookups off the UI thread applied as `PresetsLocated` (dropped after the dialog closes), the preset labels re-read from `Wording`, F3 reading `state.Browses` at the keypress, the Browse button visible with the Custom fields, and the file picker as recorded in 1.2. Verify with a `SmokeScript` step (or the headless terminal by hand) that opens the dialog, chooses Custom, presses F3 and Esc, and that the build and `DialogEffectsShould` pass.
- [ ] 4.4 README's Config tab section: the Open folder with dialog marks presets it cannot find and Browse (F3) picks a Custom executable; the f key's paragraph says a missing editor is reported with where to choose another. Verify by reading against the spec deltas and checking no em dash was introduced.

## 5. Version and whole-solution check

- [ ] 5.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next minor preview above the one on `main` when implementing (F3 is new in the dialog). Verify `Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 5.2 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate editor-not-found --strict` all pass; then `tools\install-local.ps1` and by hand: a Custom editor pointed at a renamed file gives the new line on f; the dialog shows Rider (not found) on a machine without it; F3 picks an executable; s on a solution with its association removed gives the no-application line.

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs\folder-actions` and `openspec\specs\settings` describe the new behaviour.
