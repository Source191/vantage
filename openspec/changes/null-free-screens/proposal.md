# Proposal

## Why

The screens' own state uses null for things that are not missing at all:
- a dialog with nothing wrong (`SettingsState.Problem`, `ProblemReportState.Problem`);
- the Config tab's setup still being read (`ConfigRows.For(settings, null)`);
- a key bar word that is the key's own (`("x", null)`, about twenty times, for one override);
- a key only the help lists (`KeyHelp.Hint` null, seven times);
- a tab with no detail pane (`IScreenDefinition.Detail` and `TableBody`'s three detail fields);
- an empty detail pane and a detail read that was dropped;
- a status message that never clears by itself (`StatusLayout.ClearsAfter`);
- a cell with no paint of its own and a row with no tooltip;
- pointer, bar and status positions not yet known.

Each has a plain meaning the code already spells out elsewhere (`WizardProblem.None`, `Footer.Blank`). On 31 October 2026 the owner decided that Vantage's own models and results hold no null, with null confined to the line that receives it from outside Vantage. This change does that for view state, `Wording`'s key data and the `Tui` layer's own state.

## What Changes

- **Dialog problem lines:** `SettingsState.Problem` becomes a `SettingsProblemLine` (`None`, `Shows(SettingsProblem)`) and `ProblemReportState.Problem` a `ReportTitleProblemLine` (`None`, `Shows(ReportTitleProblem)`), the shape `WizardProblem` already has. The Domain problem types stay as they are, so a validation can never refuse with "nothing".
- **Setup still being read:** `SetupFacts.Reading` (every piece `SetupState.Reading`) replaces the null that `ConfigRows.For` takes; `Program.cs` passes it until the facts arrive.
- **Key bar data:** the bars are built from plain key lists. The one word that differs from a key's own (`d Close tab` on an empty terminal row) is a named `BarWord`. `KeyHelp.HelpOnly(...)` builds the seven help-only keys, whose `Hint` is empty and `OnBar` false.
- **Status line:** `StatusLayout.ClearsAfter(StatusTone)`, which answers `TimeSpan?`, becomes `ClearsBySelf(StatusTone)` (true for a success only) plus the `SuccessShownFor` duration. `ConsoleScreen`'s `statusOwner` and `shownStatus` become one `StatusShown` (`Nothing`, `Message(footer, running, width, owner)`).
- **Detail pane:**
  - `IScreenDefinition.Detail` answers a `TableDetail<TRow>` (`None`, `Pane(source)`).
  - `TableBody`'s `detail`, `requests` and `pane` become one `DetailPart` (`NoPane`, `Pane(source, requests, view)`), so the Config tab adds no pane and keeps its layout exactly.
  - A dropped read is a `DetailRead.Dropped`, not a null `DetailContent`.
  - `DetailContent.Empty` stands for an empty pane.
  - `detailKey` uses a private sentinel for "ask again".
- **Table state:** the two pointer fields become one `PointerOver` (`Nothing`, `Header(column)`, `Row(index)`).
- **Keys and paths:**
  - `KeyMapping.From`, which answers `Key?` for a key the reducers do not know, becomes `KeyMapping.TryFrom(key, out Key mapped)`, the house `Try` shape.
  - `ConfigView.PathOf`, which answers `string?`, becomes `ConfigView.TryPathOf(row, out string path)`. The Config filter gives a row with no path an empty field, which matches nothing, as null does now.
- **Screen definitions:** `IScreenDefinition.CellPaint` answers a `CellPaint` hierarchy (`None`, `Text(colour)`, `Fill(colour)`), and `RowTip` answers an empty tip for a view whose rows have none. Terminal.Gui's own `Scheme?` stays where `TableBody` hands it over.
- **Frame:**
  - `ConsoleScreen.shownBar` starts as an empty `KeyBar`.
  - `Fill` takes parts already joined, so it loses its nullable separator.
  - `Palette.SelectedRow` gains an overload in place of its `Color? = null`.
  - `VantageIcon` is given the cell size or told to hide, decided on the one line that reads Terminal.Gui's nullable driver.
- **Version:** one step above main when it merges (projected `0.19.0-preview.8`).
- `skip_specs: true`: every key bar, help text, dialog line, status message and its timing, tooltip, colour and layout reads exactly as before.

Out of scope:
- **Kept nullable:** `ListSelection.SelectedKey`, `Selected` and `TableTitle.Selected` stay nullable; the owner accepted that a type test on the selection reads better than a case.
- **Left to the boundary change:** `SetupState.Checked.Newest` follows `UpdateCheckRecord.Newest` in null-at-the-boundary.
- **Sibling changes:**
  - null-suppressions: the `!` operators in `TableBody`, which it removes by matching the three detail parts once in `AskForDetail`, which this change builds on; and `ConsoleScreen`'s `timer` fields.
  - null-free-outcomes: `ConsoleScreen.leaving`, `DetailRequests.Accept` (which it makes answer `Show` or `Dropped`, with a sentinel for no selection, which this change builds on) and the view's `(ViewEffect?, Footer)` tuples. Those tuples include `ConfigView.WithPath`, whose test this change rewrites to use `TryPathOf`.
  - null-free-rows and null-free-observations: the rows' and the observations' own nullables.
  - null-at-the-boundary: the adapters and port answers.
  - null-free-sessions: the sessions and the wizard's origin.
  - null-guard-rule: the architecture rule.
- **Multiple Claude accounts:** a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with no change in behaviour, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code (Core):**
  - `Application\Views\`: `SettingsState.cs`, `SettingsView.cs`, `ProblemReportView.cs`, `ConfigRow.cs` (`SetupFacts`), `ConfigRows.cs`, `ConfigView.cs`, `StatusLayout.cs`.
  - `Application\`: `KeyHelp.cs`, `WordingOfConsole.cs`, `WordingOfScreens.cs`, `WordingOfDetail.cs` (`DetailContent.Empty`).
- **Code (Tui and the tool):**
  - `Tui\`: `SettingsControls.cs`, `ProblemReportDialog.cs`, `DetailPane.cs`, `TableBody.cs`, `KeyMapping.cs`, `CellPaint.cs`, `IScreenDefinition.cs` and the three screen definitions, `ConsoleScreen.cs`, `Palette.cs`, `VantageIcon.cs`.
  - A new `TableDetail.cs` in `Tui`.
  - `src\Source191.Vantage\Program.cs` (one argument).
- **Tests:**
  - `SettingsViewShould`, `ProblemReportViewShould`, `ConfigRowsShould`, `WordingOfKeyBarShould` and `StatusLayoutShould` swap `null` and `ShouldBeNull` for the named cases.
  - New facts for `KeyHelp.HelpOnly`, `DetailContent.Empty` and the Config filter matching a path.
  - `WordingOfKeyBarShould` and `WordingOfKeysAndColumnsShould` keep every expected text.
- **Docs:** `CLAUDE.md`, where it describes `KeyHelp`'s `Hint` as "null for a key only the help lists" and a definition's `Detail` as "null on Config"; `src\Source191.Vantage\Source191.Vantage.csproj` (`Version`).

## Order

This is change 6 of the null programme, whose changes merge one after another in this order:
1. shared-snapshot (0.19.0-preview.3)
2. null-suppressions (preview.4)
3. null-free-outcomes (preview.5)
4. null-free-rows (preview.6)
5. null-free-observations (preview.7)
6. null-free-screens (this one, preview.8)
7. null-at-the-boundary (preview.9)
8. null-free-port-answers (preview.10)
9. null-free-sessions (preview.11)
10. null-guard-rule (no version)

Each version is one step above main when it merges, so the numbers shift if anything else lands in between.
