# Design

## Context

CI tooling only; see proposal.md. Follows `contract-check-false-alarm` (#67).

## Decisions

- **Exit codes, not text:** checked on a throwaway xunit.v3 4.0.2 project and on the real contract tests. `--fail-skips on` reports a static `Skip` and a run-time `Assert.SkipUnless` as `failed <test>` with `FAIL_SKIP : <reason>` and exit code 2; a filter that matches nothing exits 8 with "Zero tests ran". Either fails the step without reading the output.
- **The issue still reads text:** quoting the failures is the one place wording matters, and if it changed the issue falls back to "No failed test or error line was recorded; see the run log." The run still fails, so nothing passes silently.
- **Only on the runner:** the README's local command keeps reporting skips as skips, which is what a developer without the opt-in variable expects.
