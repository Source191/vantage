# Design

## Context

See proposal.md for the failure. The fact starts `claude -p` with its standard input redirected and never written, strips the variables `InheritedEnvironment.ShouldDrop` names, polls `claude agents --json` through `ClaudeCli.ListSessionsAsync` once a second for up to `ListingDeadline` (30 s), then closes the input and expects exit 1.

A spike on 31 October 2026 (CLI 2.1.316, a fresh `CLAUDE_CONFIG_DIR` so that no user hooks run, as on the runner) measured:

| Variant | Registered | Listed | Exit |
|---|---|---|---|
| No hook | 7.0 s | not listed | 1 after 8.5 s |
| SessionStart hook sleeping 15 s | 6.1 s | interactive, no status | 1 after 22.1 s |
| SessionEnd hook sleeping 15 s | 6.5 s | interactive, no status | 1 after 22.3 s |

Every variant failed on input with "Input must be provided either through stdin or as a prompt argument when using --print", so none reached the model.

## Goals / Non-Goals

**Goals:**
- The fact passes on a clean machine and on a developer machine with its own hooks, without spending tokens.

**Non-Goals:**
- Changing how Vantage reads automated runs, or the other contract facts.

## Decisions

- **A SessionEnd hook through `--settings`.** It runs after the input error, so the run can no longer reach the model, and it is how the run stayed alive on the developer machine where the fact was written. A SessionStart hook works just as well in the spike, but it runs before the input check, so a future CLI that read the prompt differently could call the model while the hook holds the run open. Alternatives rejected: writing a prompt to the input (calls the model, so it spends tokens), an Agent SDK run (the same), and dropping the fact (the unit tests over listing fixtures would remain, but nothing would check that the real CLI still lists a print run this way).
- **The hook is `pwsh -NoProfile -Command Start-Sleep -Seconds 20` with a `timeout` of 40.** pwsh is on the runner and is a Vantage prerequisite. 20 s covers the run's registration at about 7 s plus a few listings. The run ends at about 22 s, within `ListingDeadline` of the input being closed. The timeout stops the CLI cutting the hook short.
- **The settings JSON is serialised with `System.Text.Json` from an object**, not built as a string, and passed as one `ArgumentList` element, so no quoting rules apply.
- **CLAUDE.md states the clean-machine timings** and says that a user's hooks lengthen them, so the next measurement is not misled the same way.

## Risks / Trade-offs

- [A later CLI ignores `--settings` hooks in print mode, or stops running SessionEnd on an input error] → the nightly run fails again, which is the contract check doing its job; the fact's doc comment names the hook so the cause is quick to find.
- [The developer's own SessionEnd hooks still run alongside the test's] → harmless: hooks are merged and the run only lives longer, still inside the deadline.
- [No local red] → on a machine with a SessionEnd hook the fact already passes, so the proof is the manual workflow run on the runner before merging.
