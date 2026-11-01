# Design

## Context

See proposal.md for why. `Wording` is one static partial class in `Application` (`Wording.cs` and seven `WordingOf*.cs` files), each with a `Wording*Should` test pinning its sentences verbatim; the smoke script (`tools\HeadlessTerminal\SmokeScript.cs`) reads its keys by hint and its markers from `Wording`, and uses none of the waiting words. Five dialog titles already come from `Wording` (`WizardTitle`, `SettingsDialogTitle`, `ChooseSolutionTitle`, `MemoryDialogTitle`, `ProblemReportDialogTitle`) and so do some captions (`PlaceCaption`, `WizardChosenCaption`, `ProblemReportTitleCaption`), so the Tui is inconsistent with itself rather than following a different rule. `Confirm.Ask` (`Confirm.cs:30`) prefixes `_` to each button it is given; every other dialog writes the `_` into its literal, as `Wording.BrowseButton` does. `KeyHelp(string Key, string Hint, string Line, string Tip)` names its key by text (`"enter"`, `"esc"`, `"←"`, `"n"`), which `WordingOfKeysAndColumnsShould:13-29` pins and `SmokeScript.Sequence` (`:86-91`) turns into keystrokes; `Application.Views.Key(KeyName, char)` is what the reducers see. `DisplayState(Kind, Label)` is a sealed record with six singletons, `Starting` (Kind `Working`, label "starting") and `Unknown(label)`, built only in `Adapters\Claude\ClaudeStates`; `SessionActivity`, `StateTransitionNotifier`, `Palette.ForState` and the counts read `Kind` only, and `WordingOfScreensShould:187` pins that a starting row counts as working. `Microsoft.CodeAnalysis.CSharp` is referenced by `Infrastructure.Tests` alone, where `OneTypePerFileShould` parses every source file and `RepositoryFolder.Root()` finds the repository. `Core.Tests` references Tui and already tests `ConfigScreenDefinition`.

## Goals / Non-Goals

**Goals:**
- Every word a person reads is declared in `Wording`, pinned verbatim by a fact, and read by the Tui or the Application at the last moment; nothing visible changes.
- A fact the Tui needs (which key, which enum member, which glyph) is carried as a value from the Application, never recovered from text.
- A mechanical check catches the next sentence declared in `Application` outside `Wording`.

**Non-Goals:**
- Reshaping `IScreenDefinition`, the `Cell` switches or the key dispatch (other open changes).
- Tying `KeyHelp` entries to reducer arms (V-06 c).
- Testing Terminal.Gui layout.

## Decisions

### Tab-scoped words are closed-enum switches; dialog words are one member each

`Wording.WaitingText(ConsoleTab)` and `Wording.LoadFailurePrefix(ConsoleTab)` switch over the three tabs with no discard arm, so a fourth tab fails the build at each, as `TabTitleFor` does. The dialog titles and buttons become members named for their dialog (`DeleteBranchTitle`, `DeleteBranchButtons`, `RenameTitle`, `CloseEmptyTitle`, `CloseEmptyButtons`, `ForceRemoveTitle`, `UpdateVantageTitle`, `UpdateButtons`, `DeleteSessionTitle`, `DeleteButtons`, `KeysTitle`, `CloseButton`, `CancelButton`, `CancelWithEscButton`, `CopyNameButton`, `StartButton`, `SaveButton`), following the five titles already there rather than a `DialogTitle(DialogName)` switch, which could not serve the state-taking titles. The alternative, one switch for all, was rejected for that reason. The tab-title word (`DialogWord`, "Delete session" for the tab, "Delete session" for the dialog) stays a separate fact: the tab word is short by design (`TabTitleDuring`), the title is the dialog's, and two members that happen to agree today are allowed to diverge. Each `Wording` member holds exactly the text the dialog passes today, `_` included where the dialog wrote it (`"_Cancel (Esc)"`, `"Cop_y name (F2)"`) and excluded for `Confirm.Ask`'s buttons, which add their own; so `BrowseButton`'s precedent is followed and nothing visible changes.

### Captions and hints are layout text, and the rule says so

The static captions in `WizardControls`, `SettingsControls`, `NameDialog` and `TypedConfirmationDialog`, and the two under-field hints (`NameDialog.cs:27`, `TypedConfirmationDialog.cs:42`), stay in their binders: they name the control beneath them, never vary with state and are read in place. A caption that varies with state (`WizardChosenCaption`) is `Wording`'s, as now. `CLAUDE.md`'s Wording section gains the sentence "static captions and hints beside a control may stay in the Tui binder that lays them out; the waiting words, failure prefixes, titles, buttons and anything that varies with state are `Wording`'s". The alternative, moving some thirty captions, buys a fact per caption and no one home, since each is read by one control.

### `LaunchPreview` and `TabScripts` keep their decisions and borrow their words

`LaunchPreview.Describe` still chooses among its four cases (current branch, not a repository, no slug yet, planned worktree) and returns `Wording.PreviewCurrentBranch(repositoryName)`, `Wording.PreviewNeedsRepository`, `Wording.PreviewBeforeAName(repositoryName, worktreeRoot)` or `Wording.PreviewWorktree(folderName, worktreeRoot, branch)`; `LaunchPreviewShould` asserts which member is chosen and `WordingShould` pins the words, so the four sentences are pinned once. `TabScripts.Trust` reads `Wording.TrustTabTitle(folderName)` and `Wording.TrustTabMessage(folderName)`, and `Update` reads `Wording.UpdateVantageTitle`, shared with the dialog; the message still reaches the script only through `PowerShellScript.Quote`, which `TabScriptsShould.QuoteUserTextSafely` keeps checking. The alternative, declaring `LaunchPreview` and `TabScripts` as exceptions in `CLAUDE.md`, was rejected because it leaves nothing mechanical to hold the rule and the same `TabScripts` already reads `UpdateFailedHint` from `Wording`.

### The sentence fact parses source and skips `throw`

`SentenceLiteralsShould.KeepSentencesInWording` in `test\Source191.Vantage.Infrastructure.Tests`, beside `OneTypePerFileShould`, parses every file under `src\Source191.Vantage.Core\Application` with Roslyn and fails for a `LiteralExpressionSyntax` string or an `InterpolatedStringExpressionSyntax` whose last text part ends in `.`, `?` or `!`, except in `Wording*.cs`, `CommandLine.cs` and `Diagnostics\` (the three exceptions `CLAUDE.md` names) and except under a `ThrowStatementSyntax` or `ThrowExpressionSyntax` ancestor, because an exception's message is never user text by the house rule and today's five such messages (`EffectDispatcher.cs:30`, `ScreenLoop.cs:32`, `Views\ConsoleView.cs:23`, `Views\SettingsScope.cs:27`, `Diagnostics\DiagnosticLine.cs:45`) would otherwise fail it. It lives in `Infrastructure.Tests` rather than `Core.Tests\Architecture` because that project alone references `Microsoft.CodeAnalysis.CSharp` and adding it to `Core.Tests` would slow the two-second suite's build; the alternative, a text rule over `Views` and `Actions` only, misses `Launch` and `TabScripts`, which is where the strays were.

### `KeyHelp` is built from a `Key`; its text is derived

`KeyHelp(Key Press, string Hint, string Line, string Tip)` and `KeyHelp.HelpOnly(Key, line, tip)`; the string `Key` becomes a computed property from `Press` by one switch over `KeyName` naming every member (Enter "enter", Escape "esc", Left "←", Right "→", Up "↑", Down "↓", Backspace "backspace", Character the character itself), and `HelpKey` capitalises the named ones as today. The `Wording` lists change from `new("enter", ...)` to `new(Key.Enter, ...)` and `new("n", ...)` to `new(Key.Of('n'), ...)`; `WordingOfKeysAndColumnsShould` and `SmokeScript.Sequence` keep reading the same strings. `KeyBarItem` gains `Key Press`, filled by `Placed` and `OwnItem` in `WordingOfConsole`, and `ConsoleScreen.Click` presses `item.Press`. The alternative, a computed `Press` decoded from the string, keeps text as the home, which is the finding.

### One `OptionSelectors` helper, by position in `Enum.GetValues`

`Tui\OptionSelectors` (static): `Labels<TEnum>(Func<TEnum, string> describe)` and `For<TEnum>(int x, Func<TEnum, string> describe)` build a selector whose labels are `Enum.GetValues<TEnum>()` described in order; `Index<TEnum>(TEnum)` and `At<TEnum>(int?)` convert by `Array.IndexOf` over the same array, never by cast, so a reordered or renumbered enum cannot pick the wrong label. `WizardControls.Mode` and `BaseRef`, `SettingsControls.BaseRef` and `Place`, and the four `ValueChanged` handlers in `LaunchWizardDialog.cs:91-92` and `SettingsDialog.cs:94-95` use it; the Editor selector is left to `editor-not-found`. `Labels`, `Index` and `At` are pure and tested in `Core.Tests\Tui\OptionSelectorsShould`; `For` is the one line that constructs a Terminal.Gui view.

### `DisplayState` is a closed hierarchy whose only words are the CLI's

`public abstract record DisplayState(DisplayStateKind Kind)` with three nested cases: `Known`, a non-positional record with an explicit private constructor and six `public static readonly Known` fields of its own (`Known.Working` to `Known.Stopped`), so no `Known` with the `Unknown` kind can exist; `Starting` (Kind `Working`, no members: being its own case is what distinguishes it); and `Unknown(string RawWords)` (Kind `Unknown`). `DisplayState` exposes `public static DisplayState Working => Known.Working;` and the other five, so those call sites are unchanged; a nested type and a static member cannot share a name (CS0102), so the static `Starting` field and `Unknown(string)` method go, and their five call sites (`ClaudeStates.cs:16, 22`, `ClaudeAgentsListingShould:143`, `WordingOfScreensShould:187`, `DisplayStateShould`) write `new DisplayState.Starting()` and `new DisplayState.Unknown(text)`, the house's `new X.None()` shape. Record equality still separates `Starting` from `Working`; every `Kind` reader is untouched. `Wording.StateWord(DisplayState)` is written first against today's shape (`Starting` told apart by equality with `DisplayState.Starting`, `Unknown` read through `Label`) and the `Label` readers moved onto it, so every commit builds; the reshaping then only changes its body to a switch naming the three cases and, under `Known`, every `DisplayStateKind` (the `Unknown` kind under `Known` throws `UnreachableException`). `SessionsScreenDefinition.cs:43` and `WordingOfConsole.CountedState` read it, the latter keeping "need input" for several and "working" for a group that holds a starting row. `ClaudeStates.cs:22` keeps building the unknown case from `$"{status}/{state}"`. The alternative, a `bool Starting` flag on one record, admits `Starting` with any kind.

### `terminal_bell` is a constant in Ports, not on the interface

`public const string TerminalBell = "terminal_bell";` on `NotificationChannelState`, usable as a constant pattern in `Configured { Value: NotificationChannelState.TerminalBell }` at `SetupFactsReader.cs:51` and `TerminalSetupRunner.cs:152`, assigned by `ClaudeSettings.cs:58` and `FakeClaudeSettings`, and interpolated into `Wording.ClaudeBellPlan` and `ClaudeBellEnabled`. A constant on `IClaudeSettings` was considered and rejected because SonarAnalyzer's S1214 (constants in interfaces) is not silenced in `.editorconfig` and warnings are errors.

### Repetition inside `Wording`

`EditorName` becomes `editor.Preset == EditorPreset.Custom ? Path.GetFileNameWithoutExtension(editor.Executable) : Describe(editor.Preset)` (a ternary, not a switch with a discard arm). `Counted` (`WordingOfScreens.cs:80`) is deleted for `Count` (`WordingOfOutcomes.cs:310`), which produces identical text. A private `IsToday(DateTimeOffset at, DateTimeOffset now, TimeZoneInfo zone)` in `Wording.cs` serves `WordingOfConfig.When` and `WordingOfDetail.DayAndTime`, each keeping its own format.

## Risks / Trade-offs

- [A moved word changes by a character and the screen differs] → every member is pinned verbatim by a fact written from the current literal before the Tui reads it; the two spec quotations keep holding; the smoke check's markers come from `Wording` and never from the waiting words.
- [`KeyHelp`'s new shape ripples through 45 list entries and `KeyBarItem`] → the string `Key` and `HelpKey` are kept as computed properties, so `WordingOfKeysAndColumnsShould`, `WordingOfKeyBarShould` and `SmokeScript` compile and pass unchanged.
- [`DisplayState` reshaping breaks a `Label` reader not found by the build] → the build finds them all (`SessionsScreenDefinition`, `WordingOfConsole`, `DisplayStateShould`, `ClaudeStatesShould`, `ClaudeContractShould:54`, which compiles even when skipped); `NullFreeModelsShould` and `SealedTypesShould` check the new cases.
- [The sentence fact fails on a message that is not user text] → the `throw` exclusion and the three folder exclusions are the whole allow list; a new exception message outside a `throw` is a finding, not noise.
- [Four open changes touch the same files] → whichever lands second rebases, as proposal.md lists; the Editor selector and `IScreenDefinition` are left as they are to shrink the overlap.

## Migration Plan

Nothing to migrate: no state file, key, setting or command line changes. Rollback is reverting the pull request.
