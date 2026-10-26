# Tasks

## 1. Workflow

- [x] 1.1 Pass `--fail-skips on` in the Contract tests step and exit with the test run's exit code, removing the `skipped: 0` search; verify locally that the contract tests without `VANTAGE_CLAUDE_CONTRACT` exit 2 with seven `FAIL_SKIP` failures
- [x] 1.2 Quote `^failed |^Test run summary: Zero tests ran` in the issue step; verify the extraction picks up a `FAIL_SKIP` failure and the "Zero tests ran" line
- [x] 1.3 Stop each quoted failure where the next starts; verify the issue step's script, run from the YAML with `gh` stubbed against run 40313991452's output, quotes 7 tests once each and an empty file gives the fallback sentence

## 2. Check on the runner

- [x] 2.1 Run `gh workflow run claude-contract.yml --ref contract-fail-skips`; verify it is green and the issue step is skipped (run 40313285334)
- [x] 2.2 On a temporary commit that sets `VANTAGE_CLAUDE_CONTRACT: 0`, run it again; verify it fails, opens one claude-cli issue assigned to the watcher that quotes `FAIL_SKIP`, then revert the commit and close the issue (run 40313991452, issue #68)
