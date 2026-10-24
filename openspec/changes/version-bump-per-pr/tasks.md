# Tasks

## 1. The check script

- [x] 1.1 Name the scenarios before the script: in a throwaway git repository in the session scratchpad, with `src\Source191.Vantage\Source191.Vantage.csproj` at `Version` 0.9.0 on a base commit, make one branch per scenario: (a) a change under `src\` with no bump, (b) a change under `src\` and 0.9.1, (c) a README-only change with no bump, (d) a `global.json` change with no bump, (e) a change under `src\` and 0.8.0, (f) a change under `src\` and 0.10.0, (g) a change under `src\` and `Version` `banana`. Expected: (b), (c) and (f) pass; (a), (d), (e) and (g) fail. Verify red: invoking `.github\scripts\Assert-VersionBumped.ps1 -Base base -Head <branch>` fails for every scenario because the script does not exist
- [x] 1.2 Write `.github\scripts\Assert-VersionBumped.ps1` (`-Base`, `-Head`; the tool path list in one place with a comment; `Version` read with `release.yml`'s regex over `git show`; `[semver]` comparison, strictly greater; `::error::` messages naming both versions, the first few changed tool paths and the README's bump-level section; `$ErrorActionPreference = 'Stop'`). Verify green: every scenario from 1.1 gives its expected result and exit code, and (g) names `banana` in its error
- [x] 1.3 Refactor the script for readability with the scenarios still giving the same results, and verify `Invoke-ScriptAnalyzer` reports nothing if PSScriptAnalyzer is installed (skip, and say so, if it is not)

## 2. CI wiring

- [x] 2.1 In `check.yml`'s `check` job, set the checkout to `fetch-depth: 2` and add a step before `build-test-pack`, guarded by `if: github.event_name == 'pull_request'`, that runs the script with `-Base HEAD^1 -Head HEAD`, with a comment saying why it compares the merge ref's two parents. Verify the workflow parses (`actionlint` if installed, otherwise by the run in task 5.1)

## 3. Catch-up release

- [x] 3.1 Set `Version` to 0.4.0 in `src\Source191.Vantage\Source191.Vantage.csproj`; verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass, and the script passes with `-Base main -Head HEAD` after the commit

## 4. Documentation

- [x] 4.1 README: rewrite the Releases paragraph (bump in every pull request that changes the tool; which paths count; the check that enforces it; the bump levels; the up-to-date branch protection setting and why), and change the lead-in of "Manual checks before a release" so the checks run on a pull request that touches `wt.exe`, tabs or attach. Verify "on its own pull request" and "Before bumping `Version`" no longer appear in README.md
- [x] 4.2 CLAUDE.md: rewrite the release sentence in "Build, test, release" to match (bump in every pull request that changes the tool, enforced by `check`; manual checks only for `wt.exe`, tab or attach changes), keeping the existing semantic versioning sentence. Verify "on its own pull request" no longer appears in CLAUDE.md

## 5. Integration

- [ ] 5.1 Open the pull request; verify on the runner that the version step ran (not skipped) and passed with 0.3.1 to 0.4.0, and that the `check` job is green
- [x] 5.2 The user enables "Require branches to be up to date before merging" on `main`'s branch protection (already on when checked); verify with `gh api repos/Source191/vantage/branches/main/protection --jq .required_status_checks.strict` returning `true`

## Workflow follow-up

- Archive the change in the same pull request (`/opsx:archive version-bump-per-pr`) and run `openspec validate`.
- After the merge, confirm `release.yml` published `v0.4.0` with the package attached.
