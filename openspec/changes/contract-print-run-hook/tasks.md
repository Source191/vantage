# Tasks

## 1. Hold the print run open

- [ ] 1.1 Red: record run 41189280715 (issue #127) as the failing run of `ClaudeContractShould.ListAPromptlessPrintRunAsAnAutomatedRun`. The fact passes on a machine with its own SessionEnd hook, so confirm the clean-machine behaviour instead: with a fresh `CLAUDE_CONFIG_DIR` and no hook, the run exits 1 at about 8 s without being listed.
- [ ] 1.2 Green: `StartPrintRun` passes `--settings` with a SessionEnd hook (`pwsh -NoProfile -Command Start-Sleep -Seconds 20`, timeout 40) serialised with `System.Text.Json`. Verify that the fact passes locally with `$env:VANTAGE_CLAUDE_CONTRACT = 1` and `--filter-trait "Category=ClaudeContract"`, and that the same settings keep a run listed as interactive with no status under a fresh `CLAUDE_CONFIG_DIR`.
- [ ] 1.3 Refactor: rewrite the fact's doc comment (a 3 s wait for input, the hook holding the run registered, no model call) and confirm that `ListingDeadline` covers the wait for exit; verify with `dotnet build -c Debug` and `dotnet format --verify-no-changes`.
- [ ] 1.4 Correct the automated-run bullet in `CLAUDE.md`'s Claude Code CLI facts: clean-machine timings (input wait 3 s, registered about 7 s, exit 1 about 8.5 s, CLI 2.1.316), local hooks lengthen them, and the contract fact holds the run open with a SessionEnd hook. Verify that no other text still says about 17 s (`Select-String -Pattern '17 s'` outside the archive).

## 2. Prove it on the runner

- [ ] 2.1 Run `gh workflow run claude-contract.yml --ref v-contract-print-run` and verify that the contract job passes with all eight facts.

## Workflow follow-up

- Archive the change in the same pull request; the pull request closes #127.
