# Tasks

## 1. The spike

- [x] 1.1 Reproduce the leak with a test that fails for the right reason: a `PseudoConsole` adapter test (real process, in `HeadlessTerminal.Tests`) that runs `pwsh -NoProfile -Command "[Console]::Out.WriteLine('NETOUT'); cmd /c echo GRANDCHILD"` with the test process's stdout redirected, and expects both markers in the captured output. Verify it fails with the markers missing from the capture; record the result here. Result 2026-10-27: red for the right reason, the capture held only the title and cursor sequences and `NETOUT` was missing.
- [x] 1.2 Try candidate (1) from the design (`STARTF_USESTDHANDLES` with no handles) in `PseudoConsole.Start` and `NativeMethods.cs`; if the test from 1.1 passes, keep it, otherwise try candidate (2). Verify the 1.1 test is green and the driver's stdout shows neither marker; record which candidate worked and commit. Result 2026-10-27: candidate (1) worked, no need for (2); the test is green and was red before the flag. Not committed: the project rule is to commit only when asked.

## 2. The assertion

- [x] 2.1 Test `HandoffCaptureShould` (red): a literal capture whose text between `ESC[?1049l` and `ESC[?1049h` holds `Claude Code v2.1.310` is accepted; the PR #65 shape (clears only between the markers) and a capture with no hand-off at all are refused. Verify it fails because the type does not exist. Result 2026-10-27: red with CS0103 (`HandoffCapture` does not exist).
- [x] 2.2 Implement the minimal `HandoffCaptureShould`'s subject (a pure function over the capture text, using `EscapeSequences.Strip` and `MarkerCount.Of`); then refactor. Verify the facts pass. Result 2026-10-27: `HandoffCapture.ShowsClaudeScreen`; green.
- [x] 2.3 Wire it into `HandoffCheck.DriveAsync`: after the run, read the capture and return exit 3 with its own message when Claude's screen is missing; update the class summary to list exits 0, 1, 2 and 3. Verify `dotnet run --project . -- check` from `tools\HeadlessTerminal` exits 0 on this machine with the banner in `drive-raw.txt`, and that it exits 3 when the keystroke script is edited locally to skip the attach key (revert the edit). Result 2026-10-27: the first real run exited 3 although Claude's screen was captured, because Claude draws on an alternate screen of its own (`ESC[?1049h` straight after the hint, `ESC[?1049l` on detach), so the next entry after Vantage's exit is Claude's, not Vantage's return. A fact for that shape went red, and the rule now reads from Vantage's first exit to the last entry after it. Then: exit 0 with the banner captured; exit 3 with the attach key replaced by Esc; the edit reverted.

## 3. Documentation

- [x] 3.1 Update the comment on the `handoff` job in `.github\workflows\check.yml` and CLAUDE.md's "Hand-off check" paragraph: what the check proves (Claude's screen appeared), the exit codes, and that the capture holds Claude's screen (read it with `grid` or `count "Claude Code v"`). Verify `openspec validate handoff-capture-claude-screen` passes. Result 2026-10-27: also the README's hand-off job paragraph (its caveat is answered: the live runs reach Claude's screen) and `tools\HeadlessTerminal\README.md`'s exit codes.

## Workflow follow-up

- [ ] 4.1 Run `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes`; archive this change in the pull request that completes it. After merge, read the `handoff-capture` artefact of the pull request's own check run and confirm the banner is in it. Progress 2026-10-27: build clean, 1312 passed and 7 skipped (the contract tests), format clean after restoring CRLF; no version bump, since the gate watches only `src\`, `Directory.Build.props`, `global.json` and `nuget.config`. Archiving and the post-merge artefact check remain.
