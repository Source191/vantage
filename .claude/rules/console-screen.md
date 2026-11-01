---
paths:
  - "src/Source191.Vantage.Tui/**"
  - "src/Source191.Vantage.Core/Application/Views/**"
  - "src/Source191.Vantage.Core/Application/Detail/**"
  - "test/Source191.Vantage.Core.Tests/Application/Views/**"
  - "test/Source191.Vantage.Core.Tests/Application/Detail/**"
  - "**/LatestSessions*.cs"
  - "**/RunningLoads*.cs"
  - "**/SetupFactsReader*.cs"
  - "**/FolderLabels*.cs"
  - "**/WhereLabel*.cs"
  - "**/StatusTone*.cs"
---

# The console screen

How the console is laid out and refreshed. The rules every view follows (reducers, effects and their markers, keys and columns as data in `Wording`) are in `CLAUDE.md`.

## Layout

- The console is one Terminal.Gui application, `Tui\ConsoleScreen`: one settings row (`Wording.SettingsRow`, orange labels and white values as `TextPart`s; the roots and the worktree root are on the Config tab) to the right of `Tui\VantageIcon` where the terminal draws images, a `Tabs` strip with the fixed titles Sessions, Worktrees and Config (a title that changed left the strip measured for the old one), the active tab's title in bold cyan and every frame grey (`Palette.TabFrame`: `Tabs` draws a title with the Focus role and the lines with Normal; frames coloured per tab broke at the joins the tabs share), the table's header a closed box in the same grey with white names, holding one `TableBody<TState, TRow>` per view, the counts (`Wording.SessionCounts`, `WorktreeCounts`, `ConfigCounts`) after the titles and the table's title right-aligned on the same row (a filter being typed before it, cut at its end), and the two-row key bar (`Wording.SessionsBar`, `WorktreesBar`, `ConfigBar`, placed `KeyBarItem`s drawn as clickable label pairs with tooltips) at the bottom.
- Left and Right (`TabKeys.TryFor`) step between the tabs as `NextTab` and `PreviousTab`, 1, 2 and 3 are `SelectTab`, and `<` and `>` scroll a table's columns in `TableBody`, because Shift+Left and Shift+Right never reached Vantage in Windows Terminal though they worked headless.
- `ConsoleState` holds every tab's state and the active tab. `Program.cs` hands the console one `ScreenContext` (the tab title, the settings row, whether a release is known, the clipboard and the diagnostic log).
- A table's title is a `TableTitle` fact (`ListSelection.Title(noun, labelOf)`) that `Wording.Describe` turns into words.
- The REPOSITORY and WORKTREE cells are `Domain\WhereLabel` (repository and place `Inside` a repository, the repository's middle elided first, or the folder's label alone `Outside` one; REPOSITORY gives it no place, and neither cell shows the branch, which the detail pane does) over `Domain\FolderLabel` through `Application\FolderLabels` (roots read per redraw from `CurrentSettings`), fitted to the width the screen hands its elastic column.

## Refreshing

- The worktrees are scanned once as the console opens, whichever tab is shown, and that scan waits for the Sessions tab's first listing (succeeded or failed) through `Application\LatestSessions`, which holds the Sessions tab's latest listing for every scan, so a scan runs no `claude agents` of its own. After it the Worktrees tab rescans on every show and every 15 seconds while shown, never while hidden, and `Application\RunningLoads` lets one scan run at a time (the console reserves one on the UI thread before `TriggerLoad` and starts nothing when it cannot, and the scan function is wrapped in its `Track`, so the reload after an action is counted but never skipped).
- `TableBody` asks only while its tab is shown (a hidden tab drawn alone lands on the shown one) and never makes a refresh wait, and the waiting spinner centres on the table, not the tab.
- The Config tab is `ConfigView` over `ConfigRow`s (`ConfigRows.For(settings, SetupFacts)`, a case per setting, then one `Heading` the selection steps over (`ListPolicy.Selectable`) and any filter hides, then one `Setup` case per piece of setup, its key stable across a save); its setting rows are shown at once and the setup is read off the UI thread by `SetupFactsReader` each time the tab is shown and after each save, and it has no refresh timer.

## Detail pane

- Under the Sessions and Worktrees tables `Tui\DetailPane` shows what the selected row does not: a definition's `Detail` is a `Tui\TableDetail<TRow>`: `Pane` over an `Application\Detail\IDetailSource<TRow>` (`SessionDetails` and `WorktreeDetails` over `DetailLoader`, whose every fact fails on its own as `Unavailable`), or `None` on Config, which adds no pane.
- The source states the most lines it takes, so the pane has a fixed height per tab and the table never jumps; `DetailRequests` numbers the requests and shows only the newest answer for the selected row; `Wording.Detail` words a `DetailContent` (four fact rows of up to three columns, then a worktree's commits or changed files) and `DetailLayout` sets it out at the pane's width. The help's Pane section comes from `Wording.SessionsPane` and `WorktreesPane`.

## Status line

- The status line is the console's, not a tab's: `ConsoleScreen` draws it under the settings row from the shown tab's `ITabBody.Status` and `Running`, with rows only while `State.Shown` has words or an action runs (`StatusLayout.Lines` wraps it onto at most three, and the tab strip, counts and table title move down by that many).
- Its mark and colours come from `Wording.ToneOf` (a `StatusTone` per status message and outcome case, each chosen in `StatusTonesShould`), `StatusMark` and `StatusWordsTone`; the mark shares its cell with the spinner and hides while it spins, or it blanks it.
- The line starts at the settings row's indent (after the icon), and a blue ✕ after the last word (`StatusLayout.Closes`) and a success's ten seconds (`StatusLayout.ClearsAfter`) both reduce `MessageDismissed` in the tab that showed the message, which clears it only while it is still that same `StatusMessage` instance.
- The detail pane closes the tab, and the reducers clear a message when the selection moves or the filter changes; no tab has a standing message: a Config row's description (`Wording.ConfigDescription`) is only its hover tooltip (`IScreenDefinition.RowTip`).
