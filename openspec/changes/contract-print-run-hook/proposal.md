# Proposal

## Why

The nightly Claude Code contract check failed on 31 October 2026 against CLI 2.1.316 (issue #127, run 41189280715): `ClaudeContractShould.ListAPromptlessPrintRunAsAnAutomatedRun` never saw its prompt-less `claude -p` listed as an automated run. The fact, added the day before, assumes such a run waits about 17 s on its open standard input. That figure was measured on a developer machine whose own SessionEnd hook kept the process alive. On a clean machine, such as the runner, the CLI waits 3 s for input, registers at about 7 s and exits 1 at about 8.5 s, before any listing can see it. The check is red every night until the fact is fixed, which hides any real CLI regression behind a known failure.

## What Changes

- The contract fact starts its `claude -p` with a SessionEnd hook passed through `--settings` that sleeps for 20 s. This keeps the run registered and listed long enough to be seen on any machine. It still makes no model call and still exits 1.
- The fact's doc comment and the automated-run bullet in `CLAUDE.md`'s Claude Code CLI facts are corrected: a 3 s wait for input instead of about 17 s, the timings on a clean machine, and the hook that holds the run open.
- No change to Vantage itself: the rule that an interactive listing row with a process and no status is an automated run is unchanged, and so is its requirement in `session-listing`.

Out of scope: multiple Claude accounts (a later change), the other contract facts and the help snapshots, and how GitHub notifies the watcher of a `claude-cli` issue.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a test and documentation fix, so the change declares `skip_specs: true`.

## Impact

- `test\Source191.Vantage.Infrastructure.Tests\Contract\ClaudeContractShould.cs`: `StartPrintRun` and the fact's doc comment.
- `CLAUDE.md`: the automated-run bullet under "Claude Code CLI facts".
- `.github\workflows\claude-contract.yml`: unchanged, but the fix is proved there by a manual run on the branch.
- Packaged tool: unchanged, so no `Version` bump.
