# Proposal

## Why

Mutation testing (Stryker, `.claude\rules\checks.md`) is the only check that shows whether the tests would catch a regression, not just whether they pass. Today it runs only when someone remembers to run it by hand, scoped by hand, so most releases ship with their changes never mutated.

Running it on every pull request would add a long job to the path every merge waits on. Running it after a release, on that release's changes only, catches gaps while the change is fresh and costs nothing: the repository is public, so GitHub's standard runners are free, and the job never sits on a pull request's or a release's path.

## What Changes

- **New workflow `.github\workflows\mutation.yml`:** runs Stryker on `src\Source191.Torni.Core` through `test\Source191.Torni.Core.Tests` (`--test-runner mtp`) on a Windows runner, with read-only rights and no secrets.
- **After each release, on that release's changes:**
  - It starts when `pipeline` finishes on `main` (`workflow_run`). A release created with the workflow's token starts no `release` event, so that event can't be used.
  - It carries on only when the commit carries a `v*` tag, which means the release job ran.
  - It mutates only the code changed since the previous release's tag (`--since:<previous tag>`).
- **Coalesced:** one concurrency group that never cancels, so a stack merged in a burst runs at most one job and queues one; GitHub drops the older queued run.
- **Weekly full run:** a schedule (and `workflow_dispatch`) mutates all of Core, for the whole score and anything the per-release runs missed.
- **Report:**
  - Stryker's markdown report goes in the job summary, and the HTML and JSON reports are an artefact.
  - Below a break score set from the first measured baseline (`--break-at`), the run fails, and GitHub's usual failed-run notice is the alert.
  - It never blocks a merge or a release.
- **Issue reporting:** one rolling issue labelled `mutation`, as `claude-contract.yml` keeps for CLI drift.
  - **Who writes it:** a separate `report` job, the only one holding `issues: write`, comments on the open issue, or opens one assigned to the `MUTATION_WATCHER` repository variable when none is open.
  - **After a release:** it comments whenever a mutant survived or had no covering test in that release's changed code. The comment lists the release, the range, each file, line, mutator and replacement (at most 50, then a count), and a link to the run.
  - **After the weekly full run:** it comments only when the whole-Core score falls below the previous full run's score, giving both scores and the files whose scores fell most, not every survivor.
  - **Closing:** the issue stays open until the owner closes it, and the next finding opens a new one.
- **Pinned tool:** `dotnet-stryker` pinned in a new tool manifest `.config\dotnet-tools.json`. The settings go in `stryker-config.json` beside the Core tests, so a local run and CI run the same way.
- **Docs:** `.claude\rules\checks.md` (Mutation testing, with the measured times), `CONTRIBUTING.md` and `MAINTAINING.md` where they list the workflows, and `CLAUDE.md`'s one line on mutation testing.
- **No Version bump or changelog line:** nothing under the tool paths in `ToolPaths.ps1` changes.

## Out of scope

- **Gating:** mutation testing on pull requests, or as a required check.
- **Other projects:** mutating `Source191.Torni.Infrastructure`, `Tui` or the tool project. Their tests drive real git, processes and Terminal.Gui, which is slow per mutant and better judged once Core's times are known.
- **Issue automation beyond the rolling issue:** one issue per release, closing issues automatically, or linking a finding to the pull request that made it.
- **Stryker dashboard or baseline storage** (`--with-baseline`).
- **Multiple Claude accounts:** a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a CI workflow, a tool manifest, a Stryker config and documentation. Nothing users see changes, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **New files:** `.github\workflows\mutation.yml`, `.config\dotnet-tools.json` and `test\Source191.Torni.Core.Tests\stryker-config.json`.
- **Docs:** `.claude\rules\checks.md`, `CONTRIBUTING.md`, `MAINTAINING.md`, `CLAUDE.md` and `Source191.Torni.slnx` (solution items).
- **Repository settings:** a `mutation` label, made by the report job with `gh label create --force`, and an optional `MUTATION_WATCHER` variable. Without the variable, new issues are left unassigned.
- **Runners:** one Windows runner per coalesced release run and one weekly, plus a short report job when there is something to say; no cost on a public repository. It never occupies a pull request's or a release's path.
