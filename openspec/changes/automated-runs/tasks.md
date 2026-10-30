# Tasks

## 1. The run (Domain and Ports)

- [x] 1.1 Red: `SessionListingShould` (Core.Tests) asserts a `SessionListing` built from two sessions and one `AutomatedRun` gives them back apart, and `SessionListing.Empty` holds neither. Green: `Domain\AutomatedRun(SessionId, FolderPath, int ProcessId)` and `Ports\SessionListing`. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 2. The rule (Adapters)

- [x] 2.1 Red: `ClaudeAgentsListingShould` facts from JSON fixtures (Northwind folders): an interactive row with a pid and no status, with and without a name, parses as an `AutomatedRun` and not a session; an interactive row with a pid and `idle` stays an `InteractiveSession`; a background row with no pid and no status stays a stopped `BackgroundSession`; a background row with a pid and no status stays a `BackgroundSession` (Unknown state, as today). Green: `ClaudeAgentsListing.Parse` answers a `SessionListing`. Verify the Infrastructure facts pass.
- [ ] 2.2 `IClaudeCli.ListSessionsAsync` answers `SessionListing`; `ClaudeCli`, `LaunchService`, `Program.LoadAsync` and `Program.ScanAsync` follow, the last two reading `.Sessions` where they only need sessions. Red first: a `LaunchServiceShould` fact that a run named like the new session is no clash, and a `WorktreeInventoryShould` fact that a run in a worktree is not its live session. Verify `dotnet build -c Debug` and `dotnet test -c Debug --no-build`.
- [ ] 2.3 Red: `ClaudeContractShould.ListAPromptlessPrintRunAsAnAutomatedRun` starts `claude -p` in a temporary trusted folder with standard input redirected and never written, polls `ListSessionsAsync` (up to 20 s) until its pid is among the runs and not among the sessions, then closes standard input and asserts exit code 1. Trait `Category=ClaudeContract`. Verify with `$env:VANTAGE_CLAUDE_CONTRACT = 1; dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug -- --filter-trait "Category=ClaudeContract"` from a trusted repository, and that it skips without the variable.

## 3. The memory (Application)

- [ ] 3.1 Red: `SessionRowEnricherShould` facts with the fake process registry: two runs whose trees use 215 MB and 216 MB give `AutomatedRuns(2, 431 MB)`; a run whose process has gone is counted and adds nothing; no runs give `AutomatedRuns.None`; the rows are unchanged. Green: the enricher takes the `SessionListing` and returns the rows and the summary (a small result record), measured from the same snapshot as MEM. Verify the Core facts pass.
- [ ] 3.2 Red: `SessionsViewShould` facts: `RowsLoaded` with a summary sets `SessionsViewState.AutomatedRuns`; `LoadFailed` keeps the last one; the table title counts sessions only. Green: `RowsLoaded(rows, automated)` and the state member; `Program.LoadAsync` passes the summary. Verify the Core facts pass.

## 4. The words and the screen

- [ ] 4.1 Red: `WordingOfScreensShould` facts: `SessionCounts` with one working, one stopped and `AutomatedRuns(1, 216 MB)` ends with `TextPart("◇ 1 automated 216 MB", TextTone.Grey)`; two runs read `◇ 2 automated 431 MB`; `AutomatedRuns.None` adds nothing; no sessions and one run show only the run's part. Green: the words in `WordingOfConsole`; `SessionsScreenDefinition.Counts` passes the state's summary. Verify the Core facts pass.
- [ ] 4.2 README: the Sessions section and the counts say what an automated run is, why it is not a row and what the ◇ part shows. CLAUDE.md's CLI facts: the measured table from design.md (the review, `claude -p`, a real `claude`'s first 130 ms, `claude logs` never registering), with the date and CLI version. Verify the README's words match `Wording`.

## 5. Version and checks

- [ ] 5.1 Raise `Version` one preview step above `main`'s. Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes` and `openspec validate automated-runs --strict` pass.
- [ ] 5.2 Run the hand-off check and the smoke check against the packed tool. Verify both exit 0.
- [ ] 5.3 Headless: with `claude -p` held open on standard input in a worktree that has a session, drive the packed tool and replay the screen. Verify one row for the session and the counts ending `◇ 1 automated` with a figure.
- [ ] 5.4 The product owner commits in a worktree with the security-guidance plugin on and watches the Sessions tab: no second row, the ◇ part appears for the review and goes when it ends. Record here.

## Workflow follow-up

- Archive in the pull request that completes it, after tabs-and-key-bar has archived so the counts requirement sits beside its own.
