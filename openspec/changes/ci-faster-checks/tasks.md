# Tasks

## 1. The documentation gate script

- [x] 1.1 Write `.github\scripts\Test-DocumentationOnly.ps1` (`-Base`, `-Head`, `git diff --no-renames --name-only`, answers `true` only for a non-empty list of `openspec/` paths and `.md` paths outside `.github/`, prints the first path that made it `false`, throws when git fails; comment-based help like `Assert-VersionBumped.ps1`). Verify locally: PR 91's merge (`-Base edebad6^1 -Head edebad6`) and PR 87's (`e177dbb^1`, `e177dbb`) answer `true`; PR 88's (`2cab984^1`, `2cab984`) answers `false` naming `.github/FUNDING.yml`; the same ref twice answers `false`; and a scratch commit on a throwaway branch that moves a file from `src\` to `openspec\` answers `false` (branch deleted afterwards).

## 2. The composite action: SDK and format

- [x] 2.1 In `.github\actions\build-test-pack\action.yml` replace `actions/setup-dotnet@v4` with a "Show the .NET SDK" step (`dotnet --version`), change "Format" to `dotnet format whitespace --verify-no-changes --no-restore` with a comment naming `EnforceCodeStyleInBuild` and `TreatWarningsAsErrors` as what makes the style and analyser passes redundant, and update the action's `description`. Verify locally that `dotnet build -c Release` followed by that format command passes on the branch, and that it fails (exit 2) after adding a double space to a `src\` file (reverted afterwards).
- [x] 2.2 Run the full `dotnet format --verify-no-changes` once on the branch after the Release build and confirm it passes too, so the build and the whitespace check still agree with it; record both local timings (full and whitespace) here. (29 October 2026, after a Release build: full 26.3 s, whitespace 6.2 s, both exit 0; a doubled space made the whitespace check exit 2.)
- [x] 2.3 README's "Continuous integration" paragraph: the `check` job checks whitespace with `dotnet format whitespace` (the build fails on style and analyser warnings) and uses the runner's .NET SDK through `global.json`; CLAUDE.md's CI line says the same in a clause. Verify by reading both against design.md, and that the local test loops in both still show the full `dotnet format --verify-no-changes`.

## 3. `check.yml`: concurrency, the gate and the background install

- [x] 3.1 Add the workflow-level `concurrency` block (group `check-<pull request number or run id>`, `cancel-in-progress` only for `pull_request`) with a comment on why pushes to `main` are never cancelled.
- [x] 3.2 Add the "Skip the build for a documentation-only change" step (id `documentation`, `pull_request` only, after the version check, `skip` output and a `::notice::`) and `if: steps.documentation.outputs.skip != 'true'` on every later step of `check` except the failure-only `smoke-capture` upload; comment why the job still runs and reports `check`.
- [x] 3.3 Add "Start the Claude Code install" after the gate (`Start-Process pwsh`, log in `$env:RUNNER_TEMP\claude-install.log`, `pid` output) and turn "Install Claude Code" into "Finish the Claude Code install" (`Wait-Process` with a five-minute timeout and a missing process ignored, print the log, foreground install with a `::warning::` when `claude.exe` is missing, then `GITHUB_PATH` and `claude --version` as today).
- [x] 3.4 README's "Continuous integration" paragraph: documentation-only pull requests (what counts, that pushes to `main` run everything) and the cancelling of superseded runs; CLAUDE.md's CI line gets the fast path in a clause. Verify by reading both against design.md.

## 4. `handoff` and `claude-contract.yml`

- [x] 4.1 In `handoff`: checkout first (depth 2, no persisted credentials), the gate step also runs `Test-DocumentationOnly.ps1` on pull requests and sets `enabled=false` with a notice for a documentation-only change, `setup-dotnet@v6` becomes "Show the .NET SDK", the install starts after the gate and finishes after the build and before "Trust the checkout", and "Build Release" becomes `dotnet build src\Source191.Vantage -c Release`; the token goes only to the hand-off step, and the gate receives only whether it exists.
- [x] 4.2 In `claude-contract.yml` replace `setup-dotnet@v6` with "Show the .NET SDK"; the install stays in the foreground. Verify by running the workflow by hand from the branch (Actions, claude-contract, Run workflow) and confirming the SDK line shows a 10.0 SDK at 10.0.301 or later and the contract tests run as before; record the run id here. (Run 40876779414: SDK 10.0.402, 7 contract tests passed with none skipped, hand-off check passed, 155 s.)
- [x] 4.3 README's "The hand-off job" paragraph: it skips documentation-only pull requests and builds only the tool. Verify by reading it against design.md.

## 5. Checks on GitHub

- [x] 5.1 Push the branch and open the pull request. Verify the `check` run is green, its log shows the SDK line, the whitespace format step, the background install's log and no fallback warning; record the job's total and the build, format and Claude steps' times here, against the 217 s baseline (target about 120 s), and `handoff`'s total against 160 s (target 90 to 100 s). (PR #96, run 40877017901: `check` 102 s against 217 s, SDK 10.0.402 with no download (6 s, the first `dotnet` call), build 46 s, test 15 s, whitespace format 6 s against 41 s, finishing the Claude install 1 s against 26 s with the background log printed and no fallback. `handoff` 115 s against 160 s, short of the 90 to 100 s target: building only the tool took 50 s, no faster than the whole solution's 40 to 47 s, so the saving is the SDK and the install.)
- [x] 5.2 Push a second commit to the pull request while the first run is still going. Verify the first run shows as cancelled and the second runs to green; record both run ids here. (Merging `main` into the branch 2 minutes after the first push cancelled run 40876782029 for 40150f7 mid-build, both jobs; run 40877017901 for b6c3fac ran to green.)
- [x] 5.3 Open a draft pull request based on this branch that changes only a Markdown file. Verify `check` is green in under 30 s with the notice and every step after the gate skipped, and `handoff` skipped with its notice; record the run id, then close the pull request and delete its branch. (Draft PR #97 against this branch, run 40877652590: `check` green in 18 s with the notice and every step after the gate skipped, `handoff` 14 s with "handoff skipped: documentation-only change"; closed and its branch deleted.)
- [x] 5.4 Verify `openspec validate ci-faster-checks` passes.

## Workflow follow-up

- Archive the change in the same pull request; `skip_specs` means no spec sync. The push to `main` after the merge is the first full run of the new `check` on `main`: confirm it ran every step and was not cancelled.
