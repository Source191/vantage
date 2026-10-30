# Design

## Context

`ClaudeAgentsListing` reads waitingFor to decide the display state and drops the text. `StateTransitionNotifier.Observe(previous, current)` sees every state transition for the bell. `SessionRow.Memory` is the tree's total; `MemoryBreakdown` (behind `m`) has the process count. `WorktreeInspector` counts changed files, unpushed commits and the last commit for a folder and answers whether a branch is merged; `IGit` has no listing of commit subjects or changed file names. After tabs-and-key-bar and config-tab each tab is a `TableBody` inside the console's `Tabs`, defined by an `IScreenDefinition`; the Sessions table's WHERE cell is `Domain\WhereLabel` (repository, place, branch) and the Worktrees table has eight columns with WORKTREE elastic. Terminal.Gui 2.5.0 has no `TileView`.

## Goals / Non-Goals

**Goals:**
- Every fact about the selected row is readable without leaving the console, and nothing the pane reads slows a refresh.
- The tables keep only what is worth scanning down, so long names show whole at 120 columns.

**Non-Goals:**
- The session's screen or output; a live view of several sessions (options 11 and 13).
- Any fallback for Prompted that reads the transcript.
- Changing the footer's words or the worktrees footer's health sentence.

## Decisions

- **The screen was tried and dropped.** A spike (30 October 2026, CLI 2.1.314) found `claude logs <id>` answers the session's whole screen history (276 to 345 KB, 1.2 to 2.5 s), which a terminal replay rebuilt into the screen; the pane showed its last rows. The owner's terminal check found four rows too few to read a question and the facts repeating the table, and chose facts only. Nothing of `claude logs` ships: no port, no contract test, no CLI fact.
- **`Session` gains `WaitingFor` (string?)** from the listing, read in `ClaudeAgentsListing` beside the state words. The pane's Waiting for line shows it as the CLI's own words, `a first prompt` for a session with no transcript that needs input, and `nothing` otherwise.
- **`Application\SessionActivity` records the last prompt.** `Observe(previous, current)` keeps, per session id, the latest time a background session moved from needs input or idle to working; it is called from `SessionsScreenDefinition.OnInput` beside the notifier and lives for the process. Nothing persists it.
  - *Rejected: a file in the state folder.* It would be the first state holding session ids with times, and "not seen since Vantage started" is honest.
- **`DetailLoader` builds `SessionDetail` and `WorktreeDetail`** off the UI thread, for one row: for a session, the counts from `WorktreeInspector` when the folder is in a repository and the activity's time; for a worktree, the created-worktree record, the merge state against origin's default branch, and `IGit.UnpushedCommitsAsync` (hash, age, subject of the commits not on any remote) or `ChangedFilesAsync` (from `git status --porcelain`). Each fact is read on its own and a failure is `Unavailable` for that line only; the loader never throws into the screen.
- **`DetailRequests` orders the requests** (Application, tested): each has a rising number, the selection moving or a refresh asks again, and an answer is shown only while its row is selected and no newer answer is shown. `TableBody` asks only while its tab is shown, because a hidden tab drawn alone lands on top of the shown one, and never makes a refresh wait.
- **The pane's text is `Wording.Detail`'s:** a `DetailContent` of a title, four rows of facts (each a `DetailLine` with a label, text, tone and a span of one to three columns) and, for a worktree, the listing beneath. `DetailLayout` sets it out at the pane's width: each column as wide as its widest one-column fact, the columns a wider fact spans widened, the widest narrowed first when the pane is too narrow, a cut fact ending in an ellipsis, labels orange.
  - Sessions: Waiting for, Started, Prompted / Folder (two columns), Session / Worktree, Branch, Tab / Changes (two columns), Memory. State and PR stay in the row.
  - Worktrees: Health with its reason (three columns) / Branch with its merge state (two), PR / Folder (two), Created / Changes (three, amber when any); then up to five commits or files and how many more.
- **The pane is exactly as tall as its tab's facts can be:** the divider and four rows on Sessions (five rows), and the divider, four rows and six listing lines on Worktrees (eleven), so the table's height never changes with the selection. Each `IDetailSource` states its rows; the Config tab's definition has no source and no pane. The pane is a view with only its top border, the divider, inside the tab's frame.
  - *Rejected: a divider dragged with the mouse.* It worked in Terminal.Gui 2.5.0 (`ViewArrangement.TopResizable`, with the frame re-anchored after each drag) but a facts pane has no use for more rows than its facts.
- **REPOSITORY replaces WHERE** on the Sessions tab, elastic, holding `SessionRow.Repository` for a row in a repository (its middle elided when too long) and the folder's label from `FolderLabels` otherwise; the worktree's place and branch move to the pane. The Worktrees table keeps WORKTREE (elastic, `WhereLabel` with no branch), LAST COMMIT and SESSION; its rows keep their health colours, so trouble still shows down the table.
- **The help explains the pane.** Each view's help gains a Pane section generated from `Wording.SessionsPane` and `WorktreesPane` (`ColumnHelp` lists of the pane's labels and meanings), so the health words keep their explanation after HEALTH leaves the table.
- **The waiting spinner centres on the table,** not the tab body, so it never lands in the pane.
- **MEM goes.** `SessionsColumns` loses MEM; `SessionRow.Memory` stays for the pane.
- **Quitting disposes the tabs under the binding guard,** found while testing: taking a tab out of the strip selected the next, which the console took for a click and showed (logging `view Worktrees` and `view Config` as Vantage closed).

## Risks / Trade-offs

- [Four git calls per selection change on the Worktrees tab] → each is one short process on one folder; results are per row and discarded on move, so no cache grows.
- [The selected session's folder is counted on every five-second refresh] → three short git calls off the UI thread for one folder; the rows never wait.
- [A reason from the CLI is a sentence Vantage did not write] → it is shown as the CLI's words in the pane only, never in a footer or the log.
- [Branch, changes and health leave the worktrees table] → rows keep their health colours, and the pane shows them for the selected row; the filter still matches the branch and the health.

## Migration Plan

One pull request. CLAUDE.md gains the pane in its architecture paragraph; the README's tables and a pane section change. No state or settings change; `SessionActivity` is in memory only.
