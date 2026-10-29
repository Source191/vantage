# Tasks

## 1. The sessions summary (Wording)

- [x] 1.1 Red: `WordingOfScreensShould.SummariseSessionsByState` asserts `Wording.SessionsSummary` reads "9 (4 stopped)" for nine rows with four stopped, "6" for six rows all needing input, and "none" for no rows. Green: the method, built from the rows' state labels. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` passes.
- [x] 1.2 Refactor: `SessionsScreenDefinition.ContextLines` calls `Wording.SessionsSummary` and keeps only the width fitting. Verify the Core suite passes and `dotnet build -c Debug` is clean.

## 2. Key tips (Wording)

- [ ] 2.1 Red: `WordingShould.GiveEveryKeyATip` asserts every `KeyHelp` in `SessionsKeys` and `WorktreesKeys` has a non-blank `Tip` of at most 80 characters that differs from its `Line`. Green: `KeyHelp` gains `Tip`; every key gets a sentence saying what it does and when to use it. Verify the Core suite passes and the help output in `WordingOfKeysAndColumnsShould` is unchanged.
- [ ] 2.2 Red: `WordingShould.FitEveryColumnMeaningOnItsLines` asserts every line of every `ColumnHelp.Meaning` in both views is at most 100 characters. Green: the PR meaning rewritten as two shorter lines. Verify the fact passes.

## 3. Tooltips (Tui)

- [ ] 3.1 `TableScreen.ShowToolTips` registers a view factory for each key label and hint label that builds an amber-on-background `Label` with a one-cell margin showing the key's `Tip`; `ShowColumnToolTip` builds the same view from the column's meaning with its line breaks kept. Verify with `dotnet run --project tools\HeadlessTerminal -- drive` and a script that hovers the x key (SGR motion after 8000 ms) and the PR header: the capture (`grid`) shows the tip in amber on its own cells, two lines for PR, nothing running past column 120.

## 4. Caps and the sideways scroll (Tui)

- [ ] 4.1 `KeyMapping` maps CursorLeft and CursorRight to two `Key` names the reducers ignore (a `SessionsViewShould` and `WorktreesViewShould` fact each: no effect, state unchanged). Verify the Core suite passes.
- [ ] 4.2 `TableScreen` sets `ColumnStyles` for NAME, REPOSITORY, FOLDER and BRANCH (`MaxWidth` 28, truncation indicator "…") from the definition's column names, and moves `table.ColumnOffset` on Left and Right within bounds. Verify with a headless drive at 120 x 36 against a worktree whose folder label is longer than 28 characters: the capture shows the ellipsis, and after Right the PR, AGE and TAB columns.
- [ ] 4.3 README: the keys table gains Left and Right, and the sessions and worktrees sections say long names are cut with an ellipsis. Verify the README's keys match `Wording`'s key lists by reading both.

## 5. The hint layout (Tui)

- [ ] 5.1 Red: `KeyHintColumnsShould.EndEveryKeyBy112AndEveryWordBy123`, `OverlapNoColumnOnAnyRow` (rows considered, so the third column under the logo is allowed) and `StartTheThirdColumnBelowTheLogo` (first row 2). Green: `Layout` with a first row per column and the widths design.md names. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` passes.
- [ ] 5.2 `TableScreen.AddKeyHints` starts each column at its first row. Verify with a headless capture at 126 x 36 that `<p>` reads "Open PR" beside it in both views under the logo, and at 120 x 36 that `<p>` shows with its words cut and "Close empty tabs" is whole.

## 6. The help fits the screen (issue #89)

- [ ] 6.1 `HelpDialog.Show(app, title, text)` with the scrolling read-only box and the Close button, titled through `DialogTabTitle` with `DialogName.Keys`, and `DialogEffects` uses it for `ShowHelp`. Verify with `dotnet run --project tools\HeadlessTerminal -- drive` at 120 x 36 with the keys `8000:?,2000:<PageDown>,2000:<PageDown>,1500:\^[,1500:q` then `count`: "Claude Code:" and "terminal for a plain claude" each count 1 in the capture; then at 120 x 40 without the PageDowns both count 1; then the same in the worktrees view with its own last line.
- [ ] 6.2 CLAUDE.md's Terminal.Gui fact about keeping the help short says the help scrolls; the README's help sentence says the arrow keys scroll it. Verify by reading both, and that the smoke script's Keys step still passes locally.

## 7. Version and checks

- [ ] 7.1 Raise `Version` one preview step above `main`'s (a patch raise). Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` all pass.
- [ ] 7.2 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal`. Verify exit 0.
- [ ] 7.3 The product owner installs the build with `tools\install-local.ps1` and, on the laptop screen and the monitor, rests the mouse on a key, on its words and on the PR header, scrolls with Right and Left in both views, reads the Sessions line with every session needing input, and presses `?` on the laptop to scroll to the Claude Code path. Verify the tooltip reads as a tooltip, the words show it too, the PR tooltip stays on screen, TAB is reachable, the line reads a bare count, and the help's last line is reached; record here.

## Workflow follow-up

- Archive the change in the same pull request, syncing the two delta specs into `openspec\specs`; the pull request closes issue #89.
- The WHERE column change retires the caps and the scroll for REPOSITORY and FOLDER; keep the caps on NAME and BRANCH.
