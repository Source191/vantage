# Tasks

## 1. Pinned tool and shared settings

- [ ] 1.1 Add `.config\dotnet-tools.json` pinning `dotnet-stryker` (the newest release at least two weeks old), and replace `CONTRIBUTING.md`'s global install with `dotnet tool restore`. Verify `dotnet tool restore` then `dotnet stryker --version` prints the pinned version.
- [ ] 1.2 Add `test\Source191.Torni.Core.Tests\stryker-config.json`:
  - `project` and `test-runner: mtp`;
  - the `html`, `markdown`, `json` and `progress` reporters;
  - `high` and `low` thresholds, with `break` 0 for now.

  Verify `dotnet stryker --mutate "**/Domain/NumberAwareOrder.cs"` from that folder runs with no other options and writes `mutation-report.md`.

## 2. Measure before building the workflow

- [ ] 2.1 Time a full Core run locally (`dotnet stryker`), and a `--since:<previous v* tag>` run for the latest release. Record both times, the mutant counts and the whole-Core score in `.claude\rules\checks.md`'s Mutation testing section, with the date and Stryker version. Also check the field names the report job reads (`files.<path>.mutants[]`, `status`, `mutatorName`, `replacement`, `location.start.line`) against the run's `mutation-report.json`, and keep a trimmed copy as the report job's test fixture.
- [ ] 2.2 Decide with the owner on the timings:
  - **A `--since` run under 30 minutes:** keep the per-release trigger.
  - **Over 30 minutes:** keep only the weekly run, and update design.md before going on.

  Set `break` in `stryker-config.json` from the baseline (design.md, Open Questions). Verified by the owner's recorded choice in design.md.

## 3. The workflow

- [ ] 3.1 Add `.github\workflows\mutation.yml`:
  - **Triggers:** `workflow_run` on `pipeline` (completed, `main`), weekly `schedule` and `workflow_dispatch`.
  - **Run conditions:** the `push` and `success` conditions.
  - **Permissions:** `contents: read` and `actions: read`.
  - **Concurrency:** the `mutation` group without `cancel-in-progress`.
  - **Timeouts:** 60 or 300 minutes.
  - **Pins:** every action pinned by commit with its version in a comment.

  Verify with `actionlint` if installed, and by a `workflow_dispatch` run on the branch completing, with the report in its summary and the artefact uploaded.
- [ ] 3.2 Add the release and range steps:
  - the `v*` tag on `head_sha` (otherwise a notice and stop);
  - the last finished mutation run's head commit from `gh run list`, falling back to the previous `v*` tag;
  - `--since:<that commit>` for `workflow_run`, none for `schedule` and `workflow_dispatch`.

  Verify by running the step's script locally against `main`'s history with a recorded `gh run list` answer, for three cases: a tagged commit, an untagged commit, and no earlier run.
- [ ] 3.3 Restore `pipeline`'s NuGet cache read-only (`actions/cache/restore`, same key), run `dotnet tool restore`, and run Stryker from `test\Source191.Torni.Core.Tests`. Append `mutation-report.md` to the job summary and upload `StrykerOutput` (30 days). Verified by the dispatch run in 3.1.
- [ ] 3.4 Write `run.json` (kind, release tag, `--since` commit, whole score) beside `mutation-report.json` and upload both as the `mutation-results` artefact. Set the job's `finding` output:
  - **After a release:** any `Survived` or `NoCoverage` mutant in the report.
  - **After a full run:** the score below the previous full run's, read with `gh run download`; skipped with a notice when that artefact is missing.

  Verify by running the step's script locally on the 2.1 fixture, a clean fixture and two `run.json` pairs (lower and higher score).
- [ ] 3.5 Add the `report` job, which follows `claude-contract.yml`'s report job:
  - `needs: mutation`, `if: always()` and the finding;
  - `issues: write` and `actions: read` only, with no checkout;
  - `gh label create mutation --force`;
  - a comment on the open `mutation` issue, or a new issue assigned to `vars.MUTATION_WATCHER` when set.

  The body is the release tag, range and run link, then at most 50 mutants (file, line, mutator, replacement) and a count of the rest; for a full run, both scores and the five files whose scores fell most. It is cut to 60,000 characters.

  Verify by running the body-building script locally on the fixtures from 3.4 and reading the Markdown. Then run one `workflow_dispatch` with a deliberately weak test commented out on the branch, and check the issue comment it makes. Close that issue afterwards.

## 4. Docs

- [ ] 4.1 Update the docs:
  - `.claude\rules\checks.md`: Mutation testing gets the workflow, its triggers, the range rule, the break score and the `mutation` issue; Workflows gets one bullet for `mutation.yml`.
  - `MAINTAINING.md` gets the optional `MUTATION_WATCHER` variable, beside `CLAUDE_CLI_WATCHER`.
  - `MAINTAINING.md` and `CONTRIBUTING.md`, where they list the workflows and the mutation command.
  - `CLAUDE.md`'s mutation line.
  - `Source191.Torni.slnx`: add `mutation.yml` to the solution items.

  Verify with `git grep -n "stryker"` that every mention matches the config file and the manifest.
- [ ] 4.2 Run `openspec validate mutation-testing-in-ci` and confirm `pipeline`'s `check` is green on the pull request.

## Workflow follow-up

- **Archive:** archive the change in the same pull request once 3.1's dispatch run and 4.2 pass.
- **After merge (owner):** on the first release after merge, confirm a `mutation` run started from `pipeline` with `--since` in its log and a report in its summary. Confirm the next Sunday's full run too. Before then, set the `MUTATION_WATCHER` repository variable to the login that should be assigned new `mutation` issues; this one-off step goes in the pull request's description.
