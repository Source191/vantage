# Proposal

## Why

The hand-off check exits 0 when Vantage quits on `q`, even if the attach never reaches Claude's screen, and its capture (`drive-raw.txt`, the `handoff-capture` artefact) cannot show that screen. Everything a pseudo-console child writes to its standard output (Vantage's return hint, and the whole of `claude attach`) goes to the driver's own stdout instead: PR #65's run 40301248696 has Claude Code v2.1.310 and the `ready` reply in the step log, while the capture holds 231 bytes between `ESC[?1049l` and `ESC[?1049h`. A minimal `drive pwsh` run with the driver's stdout piped shows the same for any child, so the cause is in `PseudoConsole.Start`, not in Vantage or Claude. A broken attach would pass unnoticed.

## What Changes

- **The pseudo-console child gets the pseudo-console as its standard handles**, so the capture holds the return hint and Claude's screen. The mechanism is not yet verified (`CreateProcess` is called with `inheritHandles` false and no `STARTF_USESTDHANDLES`); a spike with the `drive pwsh` reproduction settles it before the fix is chosen.
- **The hand-off check asserts the capture**: after the hand-off, Claude's own screen (the `Claude Code v` banner) must appear between Vantage leaving and re-entering the alternate screen, otherwise the check fails with its own exit code and message.
- **Docs**: the `check.yml` handoff comment and CLAUDE.md's "Hand-off check" say what the check now proves and where the capture is read; the checklist step in the Notion test plan is not touched here.
- **Version:** none. `tools\` is not in the packaged tool, so the bump rule does not apply.

Out of scope: any change to Vantage's attach, the wording of the return hint, `ClaudeJobs`, making the `handoff` job block merges, multiple Claude accounts (a later change), and the `claude-contract.yml` hand-off run beyond what it gets from the shared check.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Tooling only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Tools:** `tools\HeadlessTerminal\PseudoConsole.cs` (and `NativeMethods.cs` if a flag is added), `HandoffCheck.cs`, and a fact for the assertion in `tools\HeadlessTerminal.Tests`.
- **CI:** the comment on the `handoff` job in `.github\workflows\check.yml`; `claude-contract.yml` runs the same check and gets the assertion.
- **Docs:** CLAUDE.md "Hand-off check" paragraph.
