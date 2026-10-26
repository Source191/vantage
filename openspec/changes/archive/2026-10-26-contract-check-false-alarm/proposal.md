# Proposal

## Why

The first `claude-contract` run (40302014353) passed all 7 contract tests and the hand-off check, yet failed and opened issue #66 with an empty body. On the runner the test output is coloured: the summary line reads `<ESC>[m  skipped: 0`, so the step's `'^\s*skipped: 0\s*$'` check never matches, and a coloured `failed ...` line would escape the issue step's `'^failed '` pattern too. The skipped-tests error goes only to the console, never to `contract-output.txt`, so the issue step had nothing to quote.

## What Changes

- The contract tests step rewrites `contract-output.txt` without escape codes before reading it; the live log keeps its colour.
- When it finds skipped tests, the step also writes its `::error::` line to `contract-output.txt`, so the issue quotes it.
- The issue step says "No failed test or error line was recorded; see the run log." when there is nothing to quote.
- No behaviour change, no spec change, no version bump: `.github\` is not one of the paths that require one.
