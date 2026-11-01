# Proposal

## Why

The code review of 0.20.0-preview.7 (1 November 2026, sections 3.7 and 3.8; V-06, V-07, A-06, A-12, A-13 and D-13 in its appendix) found user-facing words living outside `Application\Wording` against the rule in `CLAUDE.md`'s Wording section, and facts the Tui re-derives from text the Application already holds. The waiting words and failure prefixes sit on the three screen definitions (`src\Source191.Vantage.Tui\SessionsScreenDefinition.cs:27-29`, `WorktreesScreenDefinition.cs:19-21`, `ConfigScreenDefinition.cs:24-26`), seven dialog titles and a dozen button labels in `DialogEffects` and the dialogs, the wizard's four preview sentences in `Application\Launch\LaunchPreview.cs:15-30`, the trust tab's title and message in `Application\TabScripts.cs:37-41`, and the STATE column's words in `Domain\DisplayState.cs:5-13`. `ConsoleScreen.Click` (`ConsoleScreen.cs:576-581`) decodes a key bar item's text back into a `Key`, `WizardControls.cs:56` hard-codes the two `LaunchMode` labels that `Wording.Describe(LaunchMode)` owns, and `"terminal_bell"`, the plural helper, the preset names and the "is it today" test each exist twice. Every reviewer looking for "every sentence Vantage shows" must read five projects instead of one, and nothing mechanical catches the next sentence that strays.

## What Changes

- **Waiting and failure words (V-07):** `Wording.WaitingText(ConsoleTab)` and `Wording.LoadFailurePrefix(ConsoleTab)`, discard-free switches like `TabTitleFor`, with the six current texts verbatim; the three screen definitions answer them for their tab.
- **Dialog titles and buttons (V-07):** the seven titles still in `DialogEffects` ("Keys", "Delete branch", "Rename session", "Close empty terminal sessions", "Force remove", "Update Vantage", "Delete session") and every button label in `Confirm.Ask` calls, `NameDialog`, `TypedConfirmationDialog`, `HelpDialog`, `MemoryDialog`, `ChooseSolutionDialog`, `LaunchWizardDialog` and `SettingsDialog` become `Wording` members, each pinned verbatim by a `WordingShould` fact. "Update Vantage" is one member serving both the dialog and the update tab's title (`TabScripts.cs:50`).
- **Captions stay (V-07):** the static captions and under-field hints in `WizardControls`, `SettingsControls`, `NameDialog` and `TypedConfirmationDialog` are layout text; `CLAUDE.md`'s Wording section records the exception, which the archived facts-not-sentences design decided and the rule never said.
- **Application sentences (A-06):** `LaunchPreview.Describe` keeps its decision and reads its four sentences from `Wording`; `TabScripts.Trust` reads its title and message from `Wording`. A Roslyn source fact fails for a string literal or interpolated string ending in `.`, `?` or `!` in `Application` outside `Wording*.cs`, `CommandLine.cs`, `Diagnostics\` and a `throw`.
- **Keys carry their key (V-06 a):** `KeyHelp` carries the `Application.Views.Key` it stands for, its `Key` text and `HelpKey` derived from it, `KeyBarItem` passes it through and `ConsoleScreen.Click` presses it instead of decoding text.
- **Enums shown once (V-06 b):** one `Tui\OptionSelectors` helper builds the Where and Worktree base selectors from `Wording.Describe` and converts index to enum one way in `WizardControls`, `SettingsControls`, `LaunchWizardDialog` and `SettingsDialog`.
- **One home for two glyphs (V-06):** `Wording.TabBlock` beside `Wording.TerminalSwatch`, and `Wording.Dot` (the ` · ` separator) made public for `ConsoleScreen`.
- **STATE words (D-13):** `DisplayState` becomes a closed hierarchy that carries no words but an unknown combination's raw CLI words; `Wording.StateWord(DisplayState)` owns "working", "needs input", "idle", "completed", "failed", "stopped" and "starting", with facts; `DisplayStateShould`'s label fact and `ClaudeStatesShould`'s "starting" assertion move with them.
- **Repetition inside Wording (A-12):** `EditorName` reuses `Describe(EditorPreset)`, `Counted` and `Count` become one plural helper, `When` and `DayAndTime` share one `IsToday`.
- **`terminal_bell` once (A-13):** `NotificationChannelState.TerminalBell` in Ports, read by `SetupFactsReader`, `TerminalSetupRunner`, `ClaudeSettings`, `FakeClaudeSettings` and the two Wording sentences.
- **Docs:** `CLAUDE.md`'s Wording section gains the caption exception and the sentence fact; `console-screen.md` names `Wording.WaitingText` and `StateWord`.
- **Version:** the next preview above `main` (0.20.0-preview.9 if nothing lands first); patch-level under semantic versioning, because no key, screen, message or file behaves differently.
- `skip_specs: true`: no visible word changes; where each lives does.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with tests and documentation, so `.openspec.yaml` declares `skip_specs: true`. The specs that quote "Listing sessions" (`openspec\specs\session-listing\spec.md:163`) and "Scanning worktrees" (`worktree-management\spec.md:131`) still hold, because the words do not change.

## Impact

- **Code:** `Application\Wording.cs`, `WordingOfScreens.cs`, `WordingOfConsole.cs`, `WordingOfConfig.cs`, `WordingOfDetail.cs`, `WordingOfOutcomes.cs`, `KeyHelp.cs`, `KeyBarItem.cs`, `Launch\LaunchPreview.cs`, `TabScripts.cs`, `SetupFactsReader.cs`, `TerminalSetup\TerminalSetupRunner.cs`; `Domain\DisplayState.cs`; `Ports\NotificationChannelState.cs`; `Adapters\Claude\ClaudeSettings.cs` and `ClaudeStates.cs`; in Tui the three screen definitions, `ConsoleScreen`, `DialogEffects`, the eight dialogs and binders named above and a new `OptionSelectors.cs`. No port method, handler, reducer or effect changes.
- **Tests:** new facts in `WordingShould`, `WordingOfScreensShould` and `KeyHelpShould`; `LaunchPreviewShould` and `TabScriptsShould` compare against the `Wording` members; `DisplayStateShould`, `ClaudeStatesShould` and `ClaudeContractShould:54` stop reading `Label`; new `OptionSelectorsShould` in `test\Source191.Vantage.Core.Tests\Tui`; new `SentenceLiteralsShould` beside `OneTypePerFileShould` in `test\Source191.Vantage.Infrastructure.Tests`, which already references Roslyn.
- **Other open changes:** `editor-not-found` edits `WordingOfOutcomes.EditorName`'s neighbour, `SettingsControls` and `SettingsDialog`'s Editor selector (its task 4.3) and adds a two-argument `Describe(EditorPreset, PresetSearch)`; `table-body-collaborators` slims `IScreenDefinition`; `columns-as-data` reshapes the definitions' `Cell` switches; `convention-nets` owns the discard arms in `SetupFactsReader` and `TerminalSetupRunner`. Whichever lands second rebases; this change keeps `IScreenDefinition`'s two members and the Editor selector as they are.
- **Users:** none.

## Out of scope

- V-06 (c), the reflection fact that presses every `KeyHelp` on a loaded state: `convention-nets`.
- V-05, columns as data in the definitions (`columns-as-data`), and the `"NAME"`/`"AGE"` coupling in `TableBody.StyleColumns`.
- The Editor preset selector, which `editor-not-found` changes.
- Moving captions or under-field hints out of the binders, or translating anything (`.claude\rules\locale.md`).
- Multiple Claude accounts, which is a later change.
