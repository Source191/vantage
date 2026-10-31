# Design

## Context

Each `TableBody` owns its own status line today: a `Label` (`footer`) at `Pos.AnchorEnd(1)` inside the tab's view, a `SpinnerView` (`busy`) beside it, and `LayOutStatus`, which shrinks the table and lifts the detail pane by one row while `StatusRowsNeeded` is 1. The tab's frame closes below that row, so the line sits inside the frame with the pane's top rule above it and nothing between it and the pane's last line. The words come from `Wording.Describe(State.Shown)`, in cyan. `ConsoleScreen.Frame` lays out the settings row, the `Tabs` strip (`Height = Dim.Fill(2)`), and the two-row key bar at `Pos.AnchorEnd(2)`. Each `TableBody` calls `ITabHost.FrameChanged()` at the end of every `Redraw`, so the frame already re-reads the active tab's counts, title and key bar.

`Footer` and `Outcome` are open hierarchies. `WordingOfOutcomesShould.HaveWordsForEveryOutcomeCase` builds every concrete `Outcome` by reflection and fails when `Wording.Describe` falls back to `ToString()`.

## Goals / Non-Goals

**Goals:**
- One status line for the console, under the settings row and outside every tab's frame, fed by the shown tab.
- A tone for every `Footer` and `Outcome` case, chosen on purpose and checked by a test, with the glyph and colour decided in `Wording`. The Tui only draws them.

**Non-Goals:**
- Changing a reducer's footer logic, a message's words, or when a message clears.
- Unit-testing the Terminal.Gui layout. The Tui stays untested code that only renders, and the headless terminal checks it.

## Decisions

### The console owns the row; the tab reports what to show
`ITabBody` gains the status the tab wants shown: its current `Footer` and whether an action it started is running. `ConsoleScreen.Frame.FrameChanged` reads it from the active tab, sets the spinner, glyph and words, and lays out again only when the number of status rows changes. The status rows start at the row after the settings row. The tab strip, the counts and the table's title, which sit at fixed rows today, move down by that number, and the tabs keep `Dim.Fill(2)` above the key bar. `TableBody` loses `footer`, `busy`, `statusRows`, `StatusRowsNeeded` and `LayOutStatus`. Its pane sits at `Pos.AnchorEnd(pane.Rows)` and its table fills the rest.
- *Alternative:* keep the label in each tab and draw a closing rule under the pane. That is less work, but the line stays inside the tab and still looks like part of it (option B in the exploration, which the owner turned down).
- *Alternative:* each tab draws into a row the frame lends it. That keeps three labels and three spinners where one is enough.
- *Tried first:* the row above the key bar, outside the frame. It worked, but the owner found it easy to miss, because experienced users do not read the key bar.
- *Alternative:* beside the versions on the settings row when the message fits, dropping below only when it does not. That moves the screen less often, but the owner preferred one place for every message.
- *Alternative:* a row always kept under the versions. Nothing would move, but the table would lose a row for good.

The screen moving down a row while a message shows is the accepted cost of putting it where the eye already is.

### Wrapping is a pure function
`StatusLayout.Lines(words, width)` in `Application\Views` breaks the words at spaces into rows of at most `width` cells, breaks a word wider than a row at its end and carries the rest to the next, and stops at three rows, the third ending with `…` when more was left. The console passes the screen's width less the three cells before the words (the margin, the glyph or spinner, and a space), and puts each row's words under the first row's. It is tested in `StatusLayoutShould`; the Tui only draws the rows it answers.

Because the spinner now lives in the window and not in a tab, a running action's spinner can no longer redraw a hidden tab over the shown one. `Shown(bool)` still keeps the waiting indicator still in a hidden tab, but no longer needs to touch the busy spinner.

### Closing and the success timeout go through the reducers
A click on the ✕ and the end of a success's ten seconds both reduce `MessageDismissed(Footer)` in the tab that showed the message, so they are tested like keys. Each of the three reducers clears its footer only when it is still that same instance (`ReferenceEquals`), so a timer started for an earlier message, even one with the same words, never clears a newer one. The console starts the timer when it first draws a message whose tone is `Success`, using `StatusLayout.ClearsAfter(tone)` (ten seconds for a success, none otherwise), and compares footers by reference to tell a new message from a redraw. `StatusLayout.Closes(footer, running)` says whether the ✕ shows: never while running, and never for `Footer.Blank` or `Footer.SettingHint`. The ✕ is drawn in the key bar's blue, the colour of every clickable word, with `Wording.StatusCloseTip` as its tooltip. The rows are wrapped two cells narrower, so the ✕ always fits after the last word.
- *Alternative:* an Esc key to clear the message. The owner asked for the ✕ instead.

### The line lines up with the versions
The status rows start at the same column as the settings row: the icon's indent, which `VantageIcon.Fit` answers when the terminal draws images, and 0 otherwise. The mark sits in that column and the words two cells after it, so a wrapped row starts under the first row's words.
### A tab switch shows the new tab's status at once
`Show(tab)` already ends in a frame redraw. Since the row reads `Active`'s status there, switching tabs swaps the message, or removes the row, without any further wiring. An action still running in a tab that is no longer shown keeps running. Its spinner and words come back when that tab is shown again, as its message does today.

### Tone is a closed enum named beside the words
`StatusTone` in `Application` has five members: `Success`, `Notice`, `Failure`, `Working` and `Hint`. Every switch over it names all five, so CS8509 marks the places a new member has to reach.
- `Wording.ToneOf(Outcome)` sits in `WordingOfOutcomes` beside `Describe(Outcome)`. `Wording.ToneOf(Footer)` sits in `WordingOfStatus.cs` with the glyphs, because the analyser rule S4136 keeps the `Describe` overloads in `Wording.cs` adjacent. Both keep a fallback arm, as open hierarchies do.
- `Wording.StatusMark(StatusTone)` answers the glyph as a `TextPart` in its `TextTone`: ✓ green, ! orange, ✗ red, and nothing for `Working` (the Tui draws the spinner in that cell) or `Hint`.
- `Wording.StatusWordsTone(StatusTone)` answers the words' `TextTone`: green, orange, red, cyan and plain respectively.
- `Palette.ForTone` already turns a `TextTone` into a colour.
- *Alternative:* an abstract `Tone` member on `Outcome` and `Footer`, which the compiler would enforce. It was rejected because tone is how a result is presented, which this project keeps in `Wording`, and because it would touch every outcome record's file.

### A missing tone fails a test, not the screen
The fallback arm answers `Notice`, so a new case never crashes the console. `StatusTonesShould` holds the intended tone of every concrete `Footer` and `Outcome` type in one table. A reflection fact, using the same reflection over the assembly as `HaveWordsForEveryOutcomeCase`, fails for any concrete type missing from that table, and a theory checks each entry against `ToneOf` with placeholder arguments. Cases whose tone depends on their data get their own facts.

### How the borderline cases fall
- **Failure:** `Footer.Failed`, `NoDialogFor` (a wiring bug), `NoPullRequest` with gap `Failed`, `StartSessionOutcome.Failed` and `NotTrusted`, `AdoptSessionOutcome.Failed`, `RenameSessionOutcome.Restored` and `Unsupervised` (the rename failed, whatever was recovered), `ProblemReportOutcome.NotOpened`, and `OpenInEditorOutcome.EditorNotFound`.
- **Notice:** every refusal that a reducer answers itself, such as `NoSessionSelected`, `RunsInOwnTerminal`, `NotRemoved`, `InvalidName`, `TypedWrongName`, `NoPullRequest` for its other gaps, `NoSolution` and `UpdateCheckOff`. Also the partial results: `DeleteSessionOutcome.KeptWorktree` and `WorktreeInUse`, `RemoveWorktreeOutcome.Refused` and `InUse`, `StartSessionOutcome.NotListedYet`, `OpenTabOutcome.NotFound`, `OpenTabOutcome.Opened` with `ColourReused` (the spec already calls that a warning), `ClipboardBusy`, `OpenInEditorOutcome.FolderMissing`, `AdoptSessionOutcome.NoMessages` and `ProcessGone`, `RenameSessionOutcome.NoMessages`, and a `CloseEmptyTerminalsOutcome` or `ResumeSessionsOutcome` that skipped or missed anything.
- **Success:** every completed action. That includes `RemoveWorktreeOutcome.BranchKept` and `DeleteSessionOutcome.BranchNotMerged`: the removal was done, and the branch is a question or a fact, not a refusal. `UpToDate` is also a success.
- **Hint:** `SettingHint`, `SetupAdvice`, and the user's own cancellations (`RenameCancelled`, `ForceCancelled`, `Kept`, `NothingChosenToResume`), because nothing went wrong and nothing was done.
- **Wrapping cases:** `Footer.Reported` takes its outcome's tone. `Footer.BranchKept` takes its removal's tone, and `Footer.Followed` takes its `Then` tone (the spec's "what followed"). `StartedAndOpened` takes its tab outcome's tone, and `DeleteSessionOutcome.RemovedWorktree` its worktree outcome's tone.

## Risks / Trade-offs

- [The spinner in the window might still repaint more than its cell] → The `drive` and `grid` capture in the tasks checks that the frame's bottom edge and the key bar survive a running action, and the hand-off check is run after it.
- [The whole tab strip and table jump down a row when a message appears, and a click aimed at a row just as it lands could hit the row above] → Accepted by the owner; a message comes from a key just pressed, so the pointer is rarely mid-click, and the reducers clear it on the next move.
- [Terminal.Gui may not re-lay the strip correctly when the `Tabs` view's `Y` changes at run time] → The headless captures check the strip, the counts and the frame title after a message appears and after it clears.
- [A tone table in the tests is one more place to update for a new case] → That is deliberate: it is how a new case is made to choose a tone, just as `WordingOfOutcomesShould` makes it choose words.
- [Orange is also the colour of the settings and pane labels] → The ! glyph, and the row's place under the frame, set the notice apart. Amber was not used because it already means needs input.

## Migration Plan

None: no state file, setting or key changes. The packaged tool's `Version` rises to the next preview.
