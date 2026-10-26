# Proposal

## Why

Claude Code's native installer updates the CLI in the background, so most users run a newer CLI than the 2.1.309 Vantage was verified against, while Vantage's adapter tests only ever see a listing captured from 2.1.309. A change to the listing, the state words or the `--bg` commands would reach users before anyone noticed, and new CLI features arrive unannounced. Separately, nothing tells a user whose CLI is older than the stated minimum why Vantage misbehaves.

## What Changes

- A nightly GitHub Actions workflow (plus a manual trigger) installs the latest Claude Code and runs contract tests that drive Vantage's own Claude adapters against it: `claude --version` is read, a `claude --bg -n` session is started, found in `claude agents --json --all` with every field Vantage reads and a state Vantage knows, then stopped and removed. The existing hand-off check (attach and return) runs in the same workflow.
- Committed snapshots of the commands and flags in `claude --help` and `claude agents --help`, compared nightly, so a new or removed flag shows up as a diff; prose rewording does not.
- A CLI that fails to install, a failed contract test or a changed snapshot opens one rolling GitHub issue labelled `claude-cli`, or comments on the open one, with the CLI version and what differed, assigned to the login in the `CLAUDE_CLI_WATCHER` repository variable so each update notifies them.
- The header shows a warning beside the Claude Code version when it is older than the minimum Vantage supports, held as one constant in the code; the README and `openspec\config.yaml` state the same minimum, and a test keeps them in step.
- Version 0.15.0: a new behaviour that keeps existing keys and files working.

## Capabilities

### New Capabilities

None. The nightly workflow, contract tests and snapshots are project tooling, not behaviour of the tool, and are described in design.md and the README's Continuous integration section.

### Modified Capabilities

- `session-listing`: the header's versions requirement gains the below-minimum warning for the Claude Code version.

## Impact

- **Code:** a Domain rule for the minimum Claude Code version over the existing `ReleaseVersion`; `Wording.HeaderValues` takes the rule's answer. No port, adapter or key changes.
- **Tests:** contract tests in `test\Source191.Vantage.Infrastructure.Tests`, skipped unless an environment variable enables them, so the normal test runs and the `check` job never start a real session; a small help-surface parser with its own unit tests; the snapshot files.
- **CI:** a new scheduled workflow with `issues: write`; the `CLAUDE_CODE_OAUTH_TOKEN` secret it shares with `handoff`; a new `CLAUDE_CLI_WATCHER` repository variable. The CI-only trust seeding in CLAUDE.md extends to this workflow.
- **Docs:** README (Continuous integration, the prerequisites' minimum), CLAUDE.md (the contract tests, the trust exception, re-verifying the CLI facts).

## Out of scope

- Multiple Claude accounts, which is a later change.
- Refusing to start, or disabling keys, on an old or untested CLI: the warning is advisory.
- A maximum or "tested up to" version.
- Updating Claude Code for the user.
- Running the contract tests on pull requests: they stay nightly and manual, so a CLI release never blocks a merge.
- Reading Claude Code's changelog automatically; a scheduled routine for that can come later.
