# Design

## Context

See proposal.md for why. Every null here is Vantage's own, apart from the places where `TableBody` and `ConsoleScreen` read Terminal.Gui:
- `ScreenToCell`'s `out int?`, `Mouse.Position`, and the `Scheme?` a colour getter returns;
- `app.Driver`, `app.ToolTips` and `app.AddTimeout`, which Terminal.Gui 2.5.0 declares nullable.

Those stay, each on the line that touches the library. `Tui` has no test project, so its part is checked by the build, the smoke check and headless captures.

The change starts from main at `356588b` with null-suppressions merged. That change matches `detail is { } source && requests is { } asked && pane is { } shown` once in `TableBody.AskForDetail` and passes the three parts on as parameters, which removes `detail!` and `pane!`. This change removes the nullable fields themselves.

## Goals / Non-Goals

**Goals:**
- No nullable reference member, parameter or field in the view state, `Wording`'s key data, `StatusLayout` or the `Tui` state listed in the proposal.
- Every text, colour, timing and position on screen unchanged. In particular, the Config tab has no pane and its table fills the tab as it does now.

**Non-Goals:**
- **The selection stays nullable.** `ListSelection.SelectedKey`, `ListSelection.Selected` and `TableTitle.Selected` stay as they are: every reducer reads `state.Selected is not SessionRow row` as a type test, and a case there would touch three reducers and the `Tui` for no gain in meaning. `TableBody.selected` (`TRow?`) mirrors the selection and stays with it. All four are recorded here for null-guard-rule's allow-list.
- **Value types that stay:** `IScreenDefinition.ElasticColumn` (`int?`, "every column has its natural width", one reader) and `ConsoleScreen.shown` (`ConsoleTab?`, before the first tab is shown).
- **Left to null-suppressions:** `ConsoleScreen`'s `object? timer` locals, around Terminal.Gui's own `AddTimeout`.
- **Left to null-free-outcomes:** `DetailRequests.Accept`'s nullable answer, which that change makes `Show` or `Dropped` (with a sentinel for no selection); `TableBody.ShowDetail` already matches `Show` when this change starts.

## Decisions

**Dialog problem lines wrap the Domain problem; they do not add `None` to it.** `SettingsProblem` and `ReportTitleProblem` are what `Settings.Validate` and `ReportTitle.Validate` refuse with, so a `None` member there would let a validation refuse with nothing.
- Each view gets the shape `WizardProblem` already has, in `Application\Views` beside its state: `SettingsProblemLine` (`None`, `Shows(SettingsProblem Problem)`) and `ReportTitleProblemLine` (`None`, `Shows(ReportTitleProblem Problem)`).
- The reducers set `None` where they set null and `Shows(...)` where they set the problem; `ProblemReportView.TooLong` answers a `ReportTitleProblemLine`.
- The dialogs keep their own texts for nothing wrong, each through `is ...Shows shows ? Wording.Describe(shows.Problem) : ...`, so `Wording` gains nothing. `SettingsControls` writes `string.Empty`, and `ProblemReportDialog` writes `" "`, because a Label given its text later needs non-empty initial text.

**`SetupFacts.Reading` is a static null object.** It holds `new SetupState.Reading()` in all five pieces, exactly what `ConfigRows.SetupRows` builds from a null today.
- `ConfigRows.For(Settings, SetupFacts)` loses its `?`, and `Program.cs` passes `SetupFacts.Reading` for the console's first Config rows.
- `SetupState.Checked.Newest` is left alone: it comes from `UpdateCheckRecord.Newest`, which null-at-the-boundary makes null-free, so changing it here would mean doing it twice.

**The key bars take plain key lists and a named override.**
- `Placed(keys, row, words)` takes `IReadOnlyList<string>` keys and an `IReadOnlyList<BarWord>` of overrides. `BarWord(string Key, string Word, string Tip)` is a private record in `WordingOfConsole`.
- Every bar passes `[]` for the overrides, except the row of an interactive session with no transcript, which passes `[new BarWord("d", "Close tab", TerminalTabTip)]`. `SessionsBar`'s switch over the selection answers the keys and the overrides together.
- Alternatives: keeping the `(string, string?)` tuples keeps the very null being removed; a hierarchy per key is heavier than one override needs.

**`KeyHelp.Hint` is non-nullable; a help-only key has an empty hint.**
- `KeyHelp.HelpOnly(string key, string line, string tip)` builds one with an empty `Hint`, and `OnBar` answers whether `Hint` has words.
- The hint is text that is only ever shown or compared, so empty is the plain "no words": the help and `WordingOfKeysAndColumnsShould` print `"1 "` for it, as they do for null now, and the smoke script still finds bar keys by their hint.
- `Placed` keeps its fallback to the key's `Line` (`help.OnBar ? help.Hint : help.Line`).
- `CLAUDE.md`'s description of `Hint` changes to name the factory.

**`StatusLayout` says whether a tone clears itself, and for how long, as two facts.**
- `ClearsBySelf(StatusTone)` keeps the switch that names every tone (true for `Success` only), and `SuccessShownFor` becomes public.
- `ConsoleScreen.ShowStatus` asks `ClearsBySelf(tone)` and passes `SuccessShownFor` to `ClearLater`, the same ten seconds as now.
- Alternative: `Timeout.InfiniteTimeSpan` for "never" (a sentinel that every reader has to know).

**The shown status is one value.** `statusOwner` is set on every line that sets `shownStatus` and is read only when `shownStatus` has a value, so the two become one private `StatusShown`:
- `Nothing`, which is the initial value;
- `Message(Footer Footer, bool Running, int Width, ITabBody Owner)`.

`ShowStatus`'s "new message" test and `CloseStatus`'s guard match `Message`, and the close mark reduces through its `Owner`.

**A tab's detail is a two-case part, so a tab with no pane adds nothing to its view.** A null object `IDetailSource` was ruled out by null-suppressions: `DetailPane` always draws a one-row divider (`Rows = lines + 1`), so an empty pane on the Config tab would take a row from its table.
- `IScreenDefinition.Detail` answers a `TableDetail<TRow>`, a closed record in `Tui`: `None` for Config, and `Pane(IDetailSource<TRow> Source)` for Sessions and Worktrees.
- `TableBody` turns it into its own private `DetailPart` in the constructor:
  - `NoPane` adds nothing, which is the layout Config has today;
  - `Pane(IDetailSource<TRow> Source, DetailRequests Requests, DetailPane View)` builds the requests and the pane and adds the pane under the table, as the `detail is not null` branch does now.
- `AskForDetail` matches `detailPart is DetailPart.Pane part` in place of null-suppressions' three-part match, and hands `part` on as it handed the three parts on.
- Each switch over the two cases names both, with an `UnreachableException` discard arm, as the two-case rule in `CLAUDE.md` says.
- Two types say one fact twice, but the definition must not build views: it is asked for `Detail` once per tab, and a property that built a pane would hand out a new one each time.

**A dropped read is a case, and an empty pane is a value.**
- `ReadAsync` answers a private `DetailRead` instead of `DetailContent?`: `Read(DetailContent Content)`, or `Dropped` when the read was cancelled. `Attempt.RunAsync` falls back to `Dropped` when a read fails, and `ReadDetail` goes on only for `Read`.
- `DetailContent.Empty` is a static instance with an empty title and no lines. `DetailPane` starts at it, `TableBody` shows it when the selection clears, and `DetailPane.Show` writes its divider title as `string.Empty` when the title is empty (today's `shown is null` branch), so the divider never shows two stray spaces.
- `detailKey` starts as, and is reset to, a private `static readonly object AskAgain = new()`. `Equals(key, detailKey)` is false for every real key, as it is for null now.

**The pointer is one value.** `headerUnderMouse` and `rowUnderMouse` become one `PointerOver` field, a private record with `Nothing`, `Header(int Column)` and `Row(int Index)`.
- It is worked out from `ScreenToCell`'s answer on the line that calls it.
- The "same as before" test becomes record equality, and hiding the tooltip sets `Nothing`.

**A key with no mapping and a row with no path use the house `Try` shape.**
- `KeyMapping.From` answers `Key?` when a Terminal.Gui key has no reducer key. It becomes `KeyMapping.TryFrom(GuiKey key, [NotNullWhen(true)] out Key? mapped)`, and `TableBody.OnKey` returns early when it answers false, as it does now for null. The translation is the boundary; the `out` with `[NotNullWhen]` is the `TryParse` convention the owner kept. Alternative: a `Key.None` case, which would let the reducers be handed "no key".
- `ConfigView.PathOf` answers `string?` for a row whose value is not a path. It becomes `ConfigView.TryPathOf(ConfigRow row, [NotNullWhen(true)] out string? path)`.
  - The Config policy's filter fields give such a row `string.Empty`, which `RowFilter.Matches` never matches against a non-blank filter, exactly as it skips null now.
  - `WithPath` tests `TryPathOf` where it tested `is string path`.
- `ProblemReportView.TooLong` is covered by the problem-line decision above.

**`CellPaint` is a three-case hierarchy in `Tui`.**
- The cases are `None` (the row's own colour), `Text(Color Foreground)` (its own colour off the selected row only, the grey hollow block) and `Fill(Color Foreground)` (its own colour on every row: the TAB swatch, today's `Always: true`).
- `TableBody.CellColour` switches over the three and still answers Terminal.Gui's `Scheme?`: null where the row's scheme applies.
- `RowTip` answers `string`. Sessions and Worktrees answer `string.Empty`, and `TableBody` shows a tooltip only for a tip with words.

**The frame's small nullables go.**
- `shownBar` starts as `new KeyBar([], [])`.
- `Fill(View row, IReadOnlyList<TextPart> parts)` loses its separator; the counts pass parts already joined by a private `Joined(parts, separator)`.
- `Palette.SelectedRow()` and `Palette.SelectedRow(Color foreground)` replace the optional `Color?`.
- `VantageIcon.Fit(ImageView icon, Size cell)` sizes and shows the icon, and `VantageIcon.Hide(ImageView icon)` hides it and answers 0. `ShowSettings` chooses with `app.Driver?.SixelSupport is { IsSupported: true } support ? VantageIcon.Fit(icon, support.Resolution) : VantageIcon.Hide(icon)`, so `Fit` no longer takes a nullable (null-suppressions has already removed its `support!`).

## Risks / Trade-offs

- [An empty `Hint` or tip is a magic value] → both are display text, where empty already means "no words"; `KeyHelp.HelpOnly`, `OnBar` and the one `Length` check in `TableBody` keep the meaning named where it is read.
- [A layout or a timing moves in the `Tui` without a test to catch it] → the smoke check, plus headless `drive` and `grid` captures taken before the first change and after the last, compared line by line. The captures cover the three tabs (Config before and after its setup is read), the settings dialog, the problem report dialog, and a success message still shown at 5 s and cleared at 12 s.
- [Two small problem-line types and two detail types] → each follows a shape the code already has (`WizardProblem`, `LaunchPlanResult`), and each keeps a `None` out of a type that means something else.
