# Tasks

## 1. Starting state

- [ ] 1.1 Red then green: `ClaudeStatesShould.ShowStartingForABackgroundJobWithAProcessAndNoStatus` (pid 100, status null, state "blocked" gives kind Working and label "starting"); add `DisplayState.Starting` and the `ClaudeStates` arm; `ShowTheRawValuesForAnUnknownCombination` still passes
- [ ] 1.2 Red then green: a header counts fact (beside the existing `SessionCounts` tests) where a starting row comes before a working row and the counts read "● 2 working"; `CountedState`'s Working arm uses `DisplayState.Working.Label`

## 2. Resume continues the session

- [ ] 2.1 Red then green: `ResumeSessionsHandlerShould.RemoveEachJobBeforeResumingIt` asserts the captured requests are `rm <jobId>` and then `--bg --resume <sessionId> -n <name>` for each chosen session, in that order; the existing resume tests still pass
- [ ] 2.2 Red then green: `ResumeSessionsHandlerShould.NameTheSessionIdWhenAResumeFailsAfterTheRemoval` asserts `ResumeNote.Failed` carries the `SessionId`; a session with no transcript is still neither removed nor resumed (extend `SkipASessionWithNoTranscriptWhenResumingAndSaySo` to assert no `rm`)
- [ ] 2.3 Red then green: a `WordingOfOutcomesShould` fact that a failed note reads "fix-123: Workspace not trusted; resume it with claude --bg --resume sess-1"; update the `ResumeNote.Failed` arm in `WordingOfOutcomes`
- [ ] 2.4 Refactor: update `ResumeSessionsHandler`'s summary to say why the job is removed first; `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass

## 3. Documentation and release

- [ ] 3.1 README "After a reboot" paragraph says each resumed session continues in its own row; verify by reading it beside the key table's `R` line
- [ ] 3.2 CLAUDE.md's `--bg --resume` fact records that a listed stopped or failed job is copied too (verified 2026-11-01, CLI 2.1.316) and that `R` removes first, like `r`
- [ ] 3.3 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to 0.19.0-preview.15; `Assert-VersionBumped.ps1` passes
- [ ] 3.4 Manual check: `tools\install-local.ps1` with the Claude Code environment variables cleared, stop a session with `x`, press `R`, tick it; the next refresh shows one row with the same session id and the row reads starting, not "/blocked", while it starts

## Workflow follow-up

- Archive the change in the pull request that completes it.
