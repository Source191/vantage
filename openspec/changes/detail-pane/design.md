# Design

## Context

`claude logs <id|name>` prints a background session's recent terminal output and takes no options (checked 2026-10-30, `claude logs --help`); it appears in the committed `claude --help` snapshot but not in config.yaml's list of commands Vantage may depend on, and its cost, size and whether it carries escape sequences are unmeasured. `ClaudeAgentsListing` reads waitingFor to decide the display state and drops the text. `StateTransitionNotifier.Observe(previous, current)` sees every state transition for the bell. `SessionRow.Memory` is the tree's total; `MemoryBreakdown` (behind `m`) has the process count. `WorktreeInspector` counts changed files, unpushed commits and the last commit for a folder and answers whether a branch is merged; `IGit` has no listing of commit subjects or changed file names. The diagnostic log records a child's `ChildCommand` (subcommand words from a fixed list), duration and exit code and never its output; `DiagnosticsPrivacyShould` fails on any fixture value in the output. The harness strips escape sequences with `EscapeSequences.Strip` in `tools\HeadlessTerminal`, which references Core. Terminal.Gui 2.5.0 has `ViewArrangement.TopResizable` (a view whose top edge can be dragged) and no `TileView`. After tabs-and-key-bar each tab is a `TableBody` inside the console's `Tabs`, and the table is `Dim.Fill(3)` tall.

## Goals / Non-Goals

**Goals:**
- The reason a session waits and its last screen are readable without attaching; a worktree's whole story is readable without the footer.
- Nothing the pane reads slows a refresh or reaches the diagnostic log.
- One new CLI dependency, measured, listed and contract-tested.

**Non-Goals:**
- Polling output for rows that are not selected; a live view of several sessions (options 11 and 13).
- Any fallback for Prompted that reads the transcript.
- Changing the footer's words or the worktrees footer's health sentence.

## Decisions

- **A spike first.** Task 1 measures `claude logs` against a session that has worked for a while: wall time, output size, whether it holds escape sequences and cursor movement, what it answers for a stopped session, for a name that matches two sessions and for an interactive session (not supervised, so expected to fail). The numbers go into CLAUDE.md's CLI facts and decide two constants: the refresh interval for the output (the rows' five seconds if a call is under about 300 ms and a few hundred KB, else every other refresh) and the lines kept.
- **The output is fetched every other refresh,** only for the selected row and only while it has a process: a row without one shows no output without asking the CLI (a done session's logs exit 1 with "job not found"), and exit 1 for a row with a process is unavailable. Output larger than 1 MB is not kept and shows as unavailable.
- **`IClaudeCli.LogsAsync(jobId)` answers the raw text, or null when the command fails.** `ClaudeCli` runs `logs <jobId>` through the recorded process runner (so `ChildCommandWords.Claude` gains `logs`, and `ChildCommandShould` and `DiagnosticEventsShould` keep the list closed); only the job id is passed, never the name, so no text from a person reaches the command. config.yaml's CLI list gains `claude logs <id>`; the nightly contract test asks for the started session's logs and expects exit 0 and text (possibly empty) and asserts the help snapshot still lists `logs`.
- **The spike (30 October 2026, CLI 2.1.314) found a screen, not lines.** `claude logs` answers the session's whole screen history as one stream with no line breaks: each row padded with spaces to the session's own terminal width, with SGR colour, CSI K erase-line and CSI H cursor positions (no OSC or DCS); 276 to 345 KB and 1.2 to 2.5 s per call from a session busy for hours. Stripping the codes and cutting the text into rows of the right width rebuilds the screen exactly (1,040 rows of 201 cells in the sample); the width is not stated, so it is inferred as the width that divides the text exactly with the most rows ending in padding.
- **`Domain\AnsiText.Strip` removes CSI, OSC and DCS sequences** with the harness's two expressions; `EscapeSequences.Strip` in the harness delegates to it so there is one rule. **`Domain\SessionScreen.Bottom(text, rows)`** rebuilds the rows as above, drops Claude's own input box and status line (everything from the second-last row made only of `─`), drops blank rows at the end, and answers the last `rows` rows; a text whose width cannot be inferred answers none, shown as unavailable. The owner chose this over showing the rows untrimmed and over leaving the output area out (30 October 2026).
- **`Session` gains `WaitingFor` (string?)** from the listing, read in `ClaudeAgentsListing` beside the state words; `Wording.StateReason(session, hasTranscript)` renders "permission prompt" from it, "no prompt sent yet" for an idle, blocked session with no transcript, and nothing otherwise. The reason is the CLI's own words, shown as text, never interpreted.
- **`Application\SessionActivity` records the last prompt.** `Observe(previous, current, now)` keeps, per session id, the latest time a background session moved from needs input or idle to working; it is called from `SessionsScreenDefinition.OnInput` beside the notifier and lives for the process. The pane reads it; nothing persists it. The transcript write-time fallback the 27 October review suggested is dropped: config.yaml allows only checking that the transcript exists, and a rule change is the owner's to make in a change of its own.
  - *Rejected: a file in the state folder.* It would be the first state holding session ids with times, and "not seen since Vantage started" is honest.
- **`DetailLoader` builds `SessionDetail` and `WorktreeDetail`** off the UI thread, for one row: for a session, the counts from `WorktreeInspector` when the folder is a worktree, the logs text, the activity's time, the row's memory; for a worktree, the created-worktree record, `IsMergedAsync` against origin's default branch, the counts, and `IGit.UnpushedCommitsAsync(worktree, limit)` (hash, age, subject from `git log --format` of the commits not on any remote) or `ChangedFilesAsync(worktree, limit)` (from `git status --porcelain`). Each fact is loaded independently and a failure becomes `Unavailable` for that line, so one failing call never blanks the pane. The loader never throws into the screen.
- **Loading is sequenced.** `ConsoleScreen` hands each request a rising number and the pane applies only an answer whose number is the latest, so a slow answer for a row the user has left is dropped (the spec's "Selection moves while loading"). Requests start on selection change and after each `RowsLoaded`; the rows' refresh never awaits them, as the pull request catalogue never delays a refresh.
- **The pane's text is `WordingOfDetail`'s.** `Wording.DetailLines(SessionDetail)` and `DetailLines(WorktreeDetail)` answer lines of (label, text, colour kind); the `DetailPane` view draws them and the output lines beneath, in grey for the output's heading. `WordingOfDetailShould` holds the sentences, including "not seen since Vantage started", "nothing to measure", "unavailable", "no output: not under the supervisor" and "… n more".
- **`DetailPane` is a `FrameView` with `Arrangement = TopResizable`** under the `TableBody`'s table, `Height` nine by default, the table `Dim.Fill` above it; the console keeps one height for both tabs for the run. If the first task finds the drag unreliable in Terminal.Gui 2.5.0, the fallback is `+` and `-` resizing the pane, as its own small key pair in the bar's view group, and the spec's drag scenario is revised with the owner before the apply.
- **MEM goes.** `SessionsColumns` loses MEM and the elastic WHERE gains its cells; `SessionRow.Memory` stays for the pane. The smoke script's sessions marker stays NAME (the first column unique to the sessions table), so the script is unchanged.

## Risks / Trade-offs

- [`claude logs` is slow or large] → the spike sets the interval and a size cap; the call runs off the UI thread and a late answer is dropped.
- [Output carries cursor movement the stripper leaves as text] → the spike records what the CLI emits; the stripper covers CSI, OSC and DCS, and the pane shows text only.
- [Four git calls per selection change on the worktrees tab] → each is one short process on one folder; results are per row and discarded on move, so no cache grows.
- [A reason from the CLI is a sentence Vantage did not write] → it is shown as the CLI's words in the pane only, never in a footer or the log; `Wording` wraps it.
- [Nine rows of pane leave fifteen table rows at 36 rows] → three to ten sessions fit; the divider drags.

## Migration Plan

One pull request. config.yaml and CLAUDE.md gain the `claude logs` fact; the README's sessions columns lose MEM and gain the pane section. No state or settings change; `SessionActivity` is in memory only.

## Open Questions

- Whether the output area should show the rows in the session's own colours: the first version is plain text; the SGR codes are there if the owner wants colour later.
