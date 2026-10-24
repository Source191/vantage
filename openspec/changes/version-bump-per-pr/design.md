# Design

## Context

`release.yml` already publishes a release whenever a push to `main` changes `Version`, comparing the pushed commit with its first parent; that half works and stays as it is. What is missing is anything on the pull request side. `check.yml`'s `check` job is the one check branch protection requires, and its checkout of a pull request is GitHub's merge ref (`refs/pull/<n>/merge`): a merge commit whose first parent is the tip of `main` it was computed against and whose second parent is the pull request's head.

## Goals / Non-Goals

**Goals:**
- A pull request that changes the packaged tool without raising `Version` cannot merge.
- The check can be run locally against any two commits, so its pass and fail paths are seen before it reaches CI.

**Non-Goals:**
- Checking that the bump is the *right* level (patch, minor, major). That is a judgement about behaviour; the README rule guides it and review catches it.
- Checking pushes to `main`; `release.yml` already handles a push.

## Decisions

**The check is a step in the `check` job, not a job of its own.** Branch protection already requires `check`, so a step there is enforced with no settings change; a separate `version` job would need adding to the required checks by hand, and would be easy to forget. It runs first, before the build, so a missing bump fails in seconds rather than after the twenty-minute build. Guarded by `if: github.event_name == 'pull_request'`.

**Compare the merge commit's two sides: `HEAD^1` against `HEAD`.** With `fetch-depth: 2` the checkout holds the merge commit and both parents. `git diff --name-only HEAD^1 HEAD` is exactly what the pull request would change on `main`, and `git show HEAD^1:<csproj>` is `main`'s version. This mirrors `release.yml`'s own `HEAD^ HEAD` comparison. Alternative: fetch `origin/main` and use `origin/main...HEAD`; rejected because it needs the merge base (a deeper fetch) and compares against whatever `main` is when the job runs rather than the commit the merge ref was built on.

**A script file, `.github\scripts\Assert-VersionBumped.ps1`, taking `-Base` and `-Head` commits.** The workflow step calls it with `HEAD^1` and `HEAD`; locally it runs against any two refs. Alternative: inline in the YAML like `release.yml`'s version check; rejected because inline script cannot be run outside a runner, and the pass and fail paths should be seen before CI. It is PowerShell, not C#, so it sits outside the TDD rule; it is verified by running it against scratch commits (tasks 1.x).

**"Changes the packaged tool" is a path list:** anything under `src/`, plus `Directory.Build.props`, `global.json` and `nuget.config` at the root, since each can change what is compiled or restored. Everything else (README, CLAUDE.md, `.github/`, `openspec/`, `test/`, `tools/`, the solution file) is exempt. A list of what counts rather than what is exempt, so a new top-level folder is exempt by default; a new folder that ships would have to be added, which is rare and visible in review.

**Compare with PowerShell's `[semver]` (`System.Management.Automation.SemanticVersion`)**, not strings, so 0.10.0 is above 0.9.0 and a pre-release suffix orders correctly. The head's version must be strictly greater than the base's; equal or lower fails. A version that does not parse fails with a message naming the value, rather than passing.

**Read `Version` with the same regex `release.yml` uses** (`<Version>\s*([^<]+?)\s*</Version>` over `git show <commit>:<csproj>`), for both sides, so neither depends on the working tree and a byte order mark from git's console output cannot break an `[xml]` cast.

**Failure output says what to do**: the base and head versions, the tool paths that changed (first few), and the README section on bump levels, written with `::error::` so it shows on the pull request's checks page.

## Risks / Trade-offs

- [A pull request's merge ref goes stale when `main` moves, so a passing check can be against an old base] → the user enables "Require branches to be up to date before merging"; updating the branch reruns the check against the new `main`.
- [Two open pull requests both bump to the same version; git merges the identical line without a conflict] → with up to date required, the second one's rerun sees `main` already at that version and fails, so it bumps again.
- [Every tool pull request now edits the same csproj line, so parallel work conflicts more often] → accepted; the conflict is one line and resolving it is the bump the second pull request needs anyway.
- [A pull request with merge conflicts gets no merge ref and GitHub does not run `pull_request` workflows for it] → it cannot merge either, so nothing slips through.
- [The path list misses a new folder that ships] → review; the list is in one place in the script with a comment.

## Migration Plan

This pull request bumps to 0.4.0 and touches `src\`, so its own check exercises the pass path on the runner; merging it publishes `v0.4.0` through the unchanged `release.yml`. Rollback is removing the step; nothing else depends on it.
