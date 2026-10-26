# Tasks

## 1. Workflow

- [x] 1.1 In `claude-contract.yml`'s Contract tests step, rewrite `contract-output.txt` without escape codes after the run; verify against run 40302014353's artefact that `'^\s*skipped: 0\s*$'` matches after stripping and did not before
- [x] 1.2 In the skipped branch, add the `::error::` line to `contract-output.txt`; verify the issue step's extraction quotes it
- [x] 1.3 In the Raise the claude-cli issue step, fall back to "No failed test or error line was recorded; see the run log." when nothing matched; verify against an empty file

## 2. Check on the runner

- [x] 2.1 Run `gh workflow run claude-contract.yml --ref contract-check-false-alarm`; verify it is green and nothing new lands on issue #66
