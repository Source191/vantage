# Proposal

## Why

`claude-contract.yml` decides whether the contract tests were skipped by searching the test output for the summary line `skipped: 0`. That depends on Microsoft.Testing.Platform's wording: if the summary were reworded, every night would raise a false alarm. xunit.v3's `--fail-skips on` turns each skipped test into a failure that quotes its skip reason, so the exit code alone says whether the CLI was checked.

## What Changes

- The Contract tests step passes `--fail-skips on` and exits with the test run's exit code; the `skipped: 0` search, and the `::error::` line it wrote to `contract-output.txt`, go.
- The issue step quotes `failed ` lines, which now include a skip's `FAIL_SKIP : <reason>`, and the `Zero tests ran` line that a filter matching no tests produces (exit code 8); it no longer looks for `::error::` lines.
- Each quoted failure stops where the next one starts. Quoting a fixed 8 lines after every `failed ` line repeated the short `FAIL_SKIP` failures: the forced-skip run's issue (#68) listed 7 tests 18 times.
- The colour-code strip stays, because the issue step still reads the text.
- No behaviour change, no spec change, no version bump: `.github\` is not one of the paths that require one.
