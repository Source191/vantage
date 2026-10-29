# Design

## Context

`check.yml` has two jobs on `windows-latest` (windows-2025-vs2026, 4 vCPU for this public repository): `check`, the only job branch protection requires, and `handoff`, which is `continue-on-error`. `check` runs the `build-test-pack` composite action, which `release.yml` shares. Measured in run 40869201276:

| `check` step | Time |
|---|---|
| `setup-dotnet@v4` (installs SDK 10.0.403 and runtime 10.0.14) | 43 s |
| `dotnet build -c Release` (16 s of it restore) | 45 s |
| `dotnet test -c Release --no-build` | 22 s |
| `dotnet format --verify-no-changes --no-restore` | 41 s |
| Install Claude Code | 26 s |
| Smoke check | 15 s |
| Checkout, version check, pack, upload, set-up | about 25 s |

`handoff` spends 33 s in `setup-dotnet@v6`, 28 s installing Claude Code, 40 s building the whole solution in Release and 42 s in the hand-off check. The image's README lists SDKs 10.0.113, 10.0.204, 10.0.303 and 10.0.402; `global.json` asks for 10.0.301 with `rollForward: latestMinor`, so the image's 10.0.402 satisfies it. `Directory.Build.props` sets `EnforceCodeStyleInBuild` and `TreatWarningsAsErrors`, and `.editorconfig` sets IDE0005, IDE0011 (braces), IDE0022 and CA1510 to CA1513 to warning, with IDE0055 (formatting) left at its default. Locally the full `dotnet format --verify-no-changes` took 25 s, `dotnet format whitespace --verify-no-changes --no-restore` 6 s; a whitespace fault failed the latter (exit 2) and an unused using failed `dotnet build` (exit 1).

## Goals / Non-Goals

**Goals:**
- `check` catches exactly what it catches today on any pull request that touches code, CI or tests.
- Nothing about the speed-ups can make `check` pass when it should fail: each one either keeps the check or falls back to the slow path.

**Non-Goals:**
- Changing the local test loop: CLAUDE.md and the README keep the full `dotnet format --verify-no-changes`, so a developer still runs every format rule before pushing.
- Faster tests or a faster smoke check: their time is real work (real git and processes, fixed keystroke pacing).

## Decisions

### Use the runner image's SDK

Remove `actions/setup-dotnet` from the composite action, `handoff` and `claude-contract.yml`, and add a "Show the .NET SDK" step (`dotnet --version`) where it was, so each log records the SDK that built it. `global.json` stays the floor: an image without a 10.0 SDK at 10.0.301 or later fails that step with the SDK's own message rather than building with the wrong one.

- *Rejected: `setup-dotnet` with `global-json-file`.* It installs exactly 10.0.301, which the image does not carry, so it still downloads an SDK on every run.
- *Rejected: pinning `global.json` to the image's SDK.* Every image update would break or change the build until someone edited it.

This also removes the `setup-dotnet@v4` in the composite action that Dependabot never raised: its `github-actions` entry scans `.github\workflows` only.

### Check whitespace only; leave style and analysers to the build

The composite's "Format" step becomes `dotnet format whitespace --verify-no-changes --no-restore`. With `EnforceCodeStyleInBuild` and `TreatWarningsAsErrors`, every IDE, CA and Sonar rule at warning already fails `dotnet build`, which is what `dotnet format`'s `style` and `analyzers` passes check at their default severity; whitespace (IDE0055 at its default severity) is the one part the build does not enforce. Workspace mode, not `--folder`: after the build every `obj\` holds generated `.cs` files that folder mode would also walk, while workspace mode formats only the projects' own documents. The step's comment says that the guarantee depends on those two properties in `Directory.Build.props`.

- *Rejected: raising IDE0055 to warning and dropping the step.* It would turn every unformatted line into a build error during the test loop, a change to how working on Vantage feels that this change does not need; the 6 s step is cheap.

### A documentation-only gate in each job

A new `.github\scripts\Test-DocumentationOnly.ps1 -Base <ref> -Head <ref>` runs `git diff --no-renames --name-only` and answers `true` only when the list is not empty and every path starts with `openspec/`, or ends in `.md` and does not start with `.github/`; it prints which path made it answer `false`. `--no-renames` lists both sides of a move, so a file moved out of `src\` into `openspec\` still counts as a code change. It writes nothing itself: the workflow step turns its answer into an output, and it runs locally with any two refs, as `Assert-VersionBumped.ps1` does.

- **In `check`:** a "Skip the build for a documentation-only change" step (id `documentation`) runs after the version check, on `pull_request` only, against `HEAD^1` and `HEAD` of the merge ref the job already fetches at depth 2, and sets `skip=true` with a `::notice::` saying why. Every later step except the failure-only artefact upload has `if: steps.documentation.outputs.skip != 'true'`, so on a push the output is empty and everything runs. The job still succeeds and reports `check`, which branch protection needs: a workflow-level `paths-ignore` would leave the required check pending for ever.
- **In `handoff`:** checkout moves before the gate (depth 2, still without persisted credentials) and the existing gate step also runs the script on pull requests, so one `enabled` output covers both "no token" and "documentation only" and every other step keeps its condition.
- *Rejected: a small first job that both jobs `need`.* It puts another runner start, about 15 s, in front of every code pull request.
- README.md counts as documentation although it is packed: the push to `main` runs the full `check`, which packs it, and a broken README cannot fail the build.

### Install Claude Code in the background

In `check`, a "Start the Claude Code install" step after the documentation gate runs `Start-Process pwsh` with the installer (`irm https://claude.ai/install.ps1 | iex`) writing to `$env:RUNNER_TEMP\claude-install.log`, and records the process id as an output. "Install Claude Code" becomes "Finish the Claude Code install", in the same place it is today: `Wait-Process` on that id (five-minute timeout, a missing process ignored), print the log, and if `~\.local\bin\claude.exe` is not there, warn and run the installer in the foreground; then add the folder to `GITHUB_PATH` and print `claude --version` as today. `Start-Process`, not `Start-Job`, because a job ends with the step's PowerShell session; the runner reaps orphaned processes only when the job ends, and the fallback keeps the job correct even if it did not.

In `handoff` the install starts after the gate and is finished after the build, before "Trust the checkout", because the installer and the trust step may both touch `~\.claude.json`. The token is never in the background process's environment: it is given only to the hand-off step, and the gate, which now also runs a script from the checkout, receives only whether the secret exists (`secrets.CLAUDE_CODE_OAUTH_TOKEN != ''`). `claude-contract.yml` keeps its foreground install, because its outcome raises the `claude-cli` issue and it does not block anyone.

### Cancel superseded pull request runs

`check.yml` gets a workflow-level `concurrency` with `group: check-${{ github.event.pull_request.number || github.run_id }}` and `cancel-in-progress: ${{ github.event_name == 'pull_request' }}`. A new commit on a pull request cancels the older commit's run, whose result branch protection no longer reads; each push to `main` is in a group of its own and is never cancelled or queued behind another.

### `handoff` builds only the tool

"Build Release" becomes `dotnet build src\Source191.Vantage -c Release`, which writes the `bin\Release\net10.0\vantage.exe` the hand-off check uses by default; `dotnet run` builds `tools\HeadlessTerminal` itself, as it does today. The test projects are built and run by `check`.

## Risks / Trade-offs

- [The image's SDK lags the newest patch by a week or two, so CI may build with 10.0.402 while a laptop has 10.0.403] → The SDK line in the log shows which; `global.json` keeps the floor, and an analyser difference between patches shows as a red build to fix, as it would today with whichever patch `setup-dotnet` fetched.
- [A style rule that `dotnet format` applies but the build does not report] → The local loop keeps the full `dotnet format`; task 2.2 runs it once against the branch to confirm it still agrees with the build.
- [A documentation-only pull request breaks something only the build sees, such as a doc file a project includes] → Only `openspec/` and `.md` files qualify, the push to `main` runs everything, and the next code pull request would fail the same way.
- [The background installer competes with the restore for bandwidth and the CPU with the build] → The saving is about 15 to 20 s rather than the full 26 s; the timings in tasks 4.1 and 4.2 record what it actually is.
- [A cancelled `handoff` leaves a throwaway session on its runner] → The runner is discarded at the end of the job.
- [Two open changes edit the same files: `dependabot-version-bump` adds a step to `check.yml` before the version check, `windows-only-package` extends the composite's package step] → Neither overlaps these edits; whichever merges later rebases. The documentation gate runs after the Dependabot step and the version check, so a Dependabot bump still happens.

## Migration Plan

None. Merging is the deployment; reverting the commit restores the old workflows. Branch protection and the ruleset are unchanged.
