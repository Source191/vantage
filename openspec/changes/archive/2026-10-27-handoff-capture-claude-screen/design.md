# Design

## Context

`PseudoConsole.Start` creates the pseudo-console with `CreatePseudoConsole`, attaches it through a `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE` attribute list and calls `CreateProcess` with `inheritHandles` false and a zeroed `StartupInfo` (no `STARTF_USESTDHANDLES`). Terminal.Gui draws through the console and is captured. Anything the child writes through its standard output is not: with the driver's stdout piped (CI, or a tool capturing the run), the child's `[Console]::Out` and a grandchild's `cmd /c echo` both appear on the driver's stdout and neither is in `drive-raw.txt` (reproduced 2026-10-27 with `drive pwsh "500:\r" -NoProfile -Command ...`). In an interactive terminal the driver's stdout is a console, which is probably why this went unseen. The effect on the check: Vantage's `ReturnHint` and `claude attach` output are missing from the capture, and the hand-off in the capture is only the 231-byte gap.

## Goals / Non-Goals

**Goals:**
- A pseudo-console child's standard handles are the pseudo-console, whatever the driver's own stdout is.
- The hand-off check fails when Claude's screen did not appear during the hand-off.

**Non-Goals:**
- Changing Vantage, `ClaudeJobs` or the keystroke timings; changing whether the `handoff` job blocks a merge.

## Decisions

- **Spike first.** Reproduce with `drive pwsh` and a piped stdout, then try the fix candidates in order and keep the first that puts `NETOUT` and `GRANDCHILD` into the capture and nothing on the driver's stdout: (1) `STARTF_USESTDHANDLES` with the three standard handles set to `INVALID_HANDLE_VALUE`/null so the child takes the console's; (2) otherwise start the child with the driver's own standard handles explicitly redirected to the pseudo-console's pipe ends. Candidates are hypotheses; the reproduction decides.
- **The assertion reads the capture, not the log.** After the run, the check takes the text from the first `ESC[?1049l` (Vantage leaving) to the last `ESC[?1049h` after it (Vantage returning). Not the next one: found during apply, Claude draws on an alternate screen of its own, so the entry straight after Vantage's exit is Claude's, and requires the `Claude Code v` banner, which Claude prints on its first screen. `MarkerCount.Of` over `EscapeSequences.Strip` does the counting, so the rule is a small pure function with a fact over a literal capture string and over the PR #65 gap (no banner).
- **A distinct exit code** for "Vantage quit but Claude's screen never appeared": exit 3 and a message beside the existing 1 (blocked UI thread) and 2 (setup). The three meanings are stated in the class summary and CLAUDE.md.
- **The check's key script is unchanged.** The throwaway session has finished its reply by the time it is attached, and Claude's banner and `● ready` are on its first screen, as the PR #65 log shows.

## Risks / Trade-offs

- **Banner wording changes.** `Claude Code v` is Claude's own text; a rewording fails the check loudly, and the contract workflow's nightly run against the latest CLI catches it. The marker is one constant beside the check.
- **A fix that hides output.** Moving the child's output into the pseudo-console means the CI log no longer shows Claude's screen; the capture artefact does. The `check.yml` comment says so.
- **The spike finds no fix.** Then the assertion reads the step output instead, which needs the check to capture its own stdout; recorded in the spike's result before any other work.
