# Tasks

Each Core task is one red-green-refactor cycle with a commit per step, the test named before the code. A `Tui` task is a refactor step, since `Tui` has no test project. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`. The change starts after null-suppressions has merged.

## 1. Baseline captures

- [ ] 1.1 Before any code changes, pack and install the tool locally (`tools\install-local.ps1`). Then, from `tools\HeadlessTerminal`, take `drive` captures at 120 x 36 (`HARNESS_COLUMNS`, `HARNESS_ROWS`) of:
  - the Sessions tab with a session selected;
  - the Worktrees tab with a worktree selected and the pointer on a header and on a row (SGR motion after 8000 ms);
  - the Config tab while its setup is read and after;
  - the settings dialog with a refused root;
  - the problem report dialog with a title over the limit;
  - a success message (such as a settings save) at 5 s and at 12 s.

  Replay each with `grid` and keep the grids outside the repository. Verify that the six grids exist and show the screens named.

## 2. View state: dialog problems, setup being read, the status timing

- [x] 2.1 `SettingsViewShould.OpenOnTheOneValueWithItsField`, `RefuseToSaveWithTheProblemAndItsField` and the facts at its other two `ShouldBeNull` lines expect `new SettingsProblemLine.None()` and `new SettingsProblemLine.Shows(problem)`: red. Then green: `Application\Views\SettingsProblemLine.cs`, `SettingsState.Problem` of that type and `SettingsView` setting it, with `SettingsControls` writing `string.Empty` for `None`. Verify that the facts pass and `SettingsState.cs` declares no nullable reference type.
- [x] 2.2 `ProblemReportViewShould.OpenWithAnEmptyTitleAndNoProblem` and `ClearTheProblemOnceTheTitleIsShortAgain` expect `new ReportTitleProblemLine.None()`, and `SayATitleIsTooLongWhileItIsTyped` expects `new ReportTitleProblemLine.Shows(ReportTitleProblem.TooLong)`: red. Then green: `ReportTitleProblemLine`, `ProblemReportState.Problem` of that type and `ProblemReportView` (including `TooLong`), with `ProblemReportDialog` writing `" "` for `None`. Verify that the facts pass and `ProblemReportView.cs` has no `?` on a reference type.
- [x] 2.3 `ConfigRowsShould.ShowTheSetupAsReadingBeforeItIsRead` passes `SetupFacts.Reading` in place of `facts: null`: red. Then green: `SetupFacts.Reading` and `ConfigRows.For(Settings, SetupFacts)`. `WordingOfKeyBarShould` and `SettingsViewShould` pass `SetupFacts.Reading` where they passed `facts: null`, and `Program.cs` passes it for the console's first Config rows. Verify that every Core fact passes and `Select-String -Path src\Source191.Vantage\Program.cs -Pattern 'facts: null'` finds nothing.
- [x] 2.4 `StatusLayoutShould`'s two timing facts expect `ClearsBySelf(StatusTone.Success)` to be true with `SuccessShownFor` at ten seconds, and every other tone false (in place of `ShouldBeNull`): red. Then green: `ClearsBySelf` with its switch naming every tone, and `SuccessShownFor` public. `ConsoleScreen.ShowStatus` asks `ClearsBySelf` and passes `SuccessShownFor`. Verify that the facts pass and `StatusLayout.cs` has no `TimeSpan?`.
- [x] 2.5 `ConfigViewShould.FilterTheRowsByAPath` (a filter of `work` keeps only the `D:\work` root row and the rows whose setting names it) is a pinning fact: it passes at once against today's `PathOf`, and pins the filter before the refactor. Then refactor: `ConfigView.TryPathOf` replaces `PathOf`, the policy's filter fields give a row with no path `string.Empty`, and `WithPath` tests `TryPathOf`. Verify that `FilterTheRowsByAPath`, `CopyAPathWhole`, `OpenAFolderOrTheFolderHoldingAFile` and `SayWhenTheValueIsNotAPath` pass, and that `ConfigView.cs` declares no `string?` return.

## 3. Key data in Wording

- [ ] 3.1 `KeyHelpShould.PutAHelpOnlyKeyOffTheBar` (`KeyHelp.HelpOnly("1", ...)` has an empty `Hint` and `OnBar` false) and `KeepABarKeyOnTheBar`: red. Then green: `KeyHelp(string Key, string Hint, string Line, string Tip)`, `HelpOnly` and `OnBar`, with `TabKeyHelp`'s seven keys built by `HelpOnly`. Verify that the two facts, `WordingOfKeysAndColumnsShould` and the help facts pass with every expected text unchanged.
- [ ] 3.2 `WordingOfConsole`'s three bars take plain key lists and a `BarWord` override list in place of `(string Key, string? Word)` tuples (refactor step: no new fact). Verify that `WordingOfKeyBarShould` passes unchanged, including `"d Close tab"` and its tip, and that `Select-String -Path src\Source191.Vantage.Core\Application\WordingOfConsole.cs -Pattern 'string\?'` finds nothing.
- [ ] 3.3 `CLAUDE.md`'s code rules describe `Hint` as the key bar's word, empty for a key only the help lists (`KeyHelp.HelpOnly`), in place of "null for a key only the help lists". Verify by reading the sentence against `KeyHelp.cs`.

## 4. The detail pane

- [ ] 4.1 `DetailLayoutShould.LayOutNothingForTheEmptyContent` (`DetailLayout.Rows(DetailContent.Empty, 60, 5)` holds no text): red. Then green: `DetailContent.Empty`. `DetailPane` holds a `DetailContent` starting at `Empty` and writes an empty divider title for it. Verify that the fact passes and `DetailPane.cs` has no `DetailContent?`.
- [ ] 4.2 `IScreenDefinition.Detail` answers `TableDetail<TRow>` (`None` for Config, `Pane(source)` for Sessions and Worktrees). `TableBody` holds one private `DetailPart` (`NoPane`, `Pane(source, requests, view)`) in place of `detail`, `requests` and `pane`; it adds the pane only for `Pane`, shows `DetailContent.Empty` when the selection clears, and matches `DetailPart.Pane` once in `AskForDetail`. Verify the build, that `TableBody.cs` and `IScreenDefinition.cs` have no nullable detail member, and that the Config grid of 1.1 is unchanged.
- [ ] 4.3 `TableBody.ReadAsync` answers a private `DetailRead` (`Read(content)`, `Dropped`), `Attempt.RunAsync` falls back to `Dropped`, and `detailKey` uses the private `AskAgain` sentinel. Verify the build, and that `TableBody.cs` has no `DetailContent?` and no `object?`.
- [ ] 4.4 `CLAUDE.md`'s description of a definition's `Detail` ("null on Config") names `TableDetail<TRow>.None`. Verify by reading it against `IScreenDefinition.cs`.

## 5. Tui state

- [ ] 5.1 `TableBody`: `headerUnderMouse` and `rowUnderMouse` become one `PointerOver` (`Nothing`, `Header`, `Row`), and `KeyMapping.From` becomes `KeyMapping.TryFrom`, with `OnKey` returning early when it answers false. Verify the build, that `TableBody.cs` has no `int?` field and `KeyMapping.cs` no `Key?` return, and that the smoke check's keys still reach the views (5.4).
- [ ] 5.2 `CellPaint` and `RowTip`:
  - `CellPaint` becomes `None`, `Text(Color)` and `Fill(Color)`.
  - `IScreenDefinition.CellPaint` answers it, and `RowTip` answers `string`.
  - The Sessions definition answers `Text(Palette.Grey)` where it answered `new CellPaint(Palette.Grey)`, and `Fill(...)` where it answered `Always: true`. The other two definitions answer `None` and `string.Empty`.
  - `TableBody.CellColour` switches over the three cases, and shows a row's tooltip only for a tip with words.

  Verify the build, and that no screen definition answers `null`.
- [ ] 5.3 `ConsoleScreen`:
  - `statusOwner` and `shownStatus` become one private `StatusShown` (`Nothing`, `Message`).
  - `shownBar` starts as an empty `KeyBar`.
  - `Fill` takes joined parts.
  - `Palette.SelectedRow` has two overloads.
  - `VantageIcon.Fit(icon, cell)` or `VantageIcon.Hide(icon)` is chosen on the line that reads `app.Driver?.SixelSupport`.

  Verify the build, and that `ConsoleScreen.cs`, `Palette.cs` and `VantageIcon.cs` declare no nullable parameter or field other than `shown` (`ConsoleTab?`), `leaving` (null-free-outcomes) and the `timer` locals.
- [ ] 5.4 Repack and install locally, take the captures of 1.1 again, and compare each grid with its baseline. Verify that every line matches. Then run the smoke check (`dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>`) and confirm exit 0. Record the comparison here.

## 6. Version and the whole build

- [ ] 6.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to one step above main's (projected `0.19.0-preview.8`). Verify that `.github\scripts\Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 6.2 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution and `dotnet format --verify-no-changes` all pass, and `openspec validate null-free-screens` passes.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
