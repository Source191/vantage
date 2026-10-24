# Proposal

## Why

The last version bump was 0.3.1 in PR #12; the ten pull requests since (#13 to #22) merged without one, so the security hardening and the new `d` confirmation have never been released. The documented rule ("bump `Version` on its own pull request once the manual checks have passed") relies on someone remembering to raise that pull request, and nothing checks it. Every pull request that changes the packaged tool should carry its own bump, and CI should refuse one that does not.

## What Changes

- `check.yml` gains a step in the `check` job, run only on pull requests and before the build, that fails when the pull request changes the packaged tool and its `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` is not higher than `main`'s (compared as semantic versions, so 0.10.0 beats 0.9.0). "Changes the packaged tool" means a changed path under `src\` or a change to `Directory.Build.props`, `global.json` or `nuget.config`; a pull request touching only docs, CI, `openspec\`, `test\` or `tools\` passes without a bump. It sits in the job branch protection already requires, so no new required check is needed.
- The rule for the bump level, written in CLAUDE.md and the README: patch for fixes, refactors and text; minor for a new key or behaviour that keeps existing keys working; major for a change that breaks existing keys, state files or the `vantage here` command line; `0.y.z` while pre-1.0, where minor may break.
- The README's release text changes from "bump on its own pull request" to "bump in every pull request that changes the tool"; the manual checks are run only on a pull request that touches `wt.exe`, tabs or attach, not before every release.
- The catch-up release: `Version` goes from 0.3.1 to 0.4.0 in this pull request. Minor, because since 0.3.1 `d` asks for a confirmation and asks before deleting an unmerged branch it would previously have kept, which is new behaviour on an existing key.
- One repository setting the user changes by hand: "Require branches to be up to date before merging" on `main`'s branch protection, so a pull request's check always compares against the `main` it will merge into. Without it, two open pull requests that both bump to the same version merge cleanly and the second one never releases.

Out of scope: deriving the version automatically (labels, conventional commits, GitVersion, release-please); publishing to nuget.org; a changelog beyond GitHub's generated release notes; a merge queue; multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This change alters CI and documentation only, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `.github\workflows\check.yml`: one new step and a deeper checkout (`fetch-depth: 2`) so the pull request's merge commit and its base are both present.
- `src\Source191.Vantage\Source191.Vantage.csproj`: `Version` 0.4.0.
- Documentation: CLAUDE.md (the build, test and release paragraph), README (Releases and Manual checks before a release).
- GitHub settings, by the user: "Require branches to be up to date before merging" on `main`.
- Contributors: every pull request that changes `src\` now edits the csproj, so two open pull requests will conflict on that line or fail the check once the other merges; whoever merges second rebases and bumps again.
