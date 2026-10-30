# Design

## Context

`check.yml` runs on every pull request with a read-only token (`permissions: contents: read`) and checks out the merge ref with `persist-credentials: false`; its first step runs `Assert-VersionBumped.ps1 -Base HEAD^1 -Head HEAD`, which compares the two parents of the merge ref and exits 1 when a tool path changed without `Version` rising above the base. A workflow run that Dependabot's own push triggers gets a read-only `GITHUB_TOKEN` and sees Dependabot secrets, not repository secrets; a push made with `GITHUB_TOKEN` never starts another workflow run, while a push made with a personal access token does. Branch protection requires `check` on an up-to-date branch, and the "Owner approves main" ruleset requires the owner's approval, which the owner bypasses only for their own pull requests.

## Goals / Non-Goals

**Goals:**
- A Dependabot pull request that touches `src\` reaches green without a person touching the csproj.
- The bump commit is plain: one line in the csproj, a message that says why, no attribution trailers.
- A missing or expired token fails loudly and says what to do.

**Non-Goals:**
- Auto-merge: the owner's review and merge stay.
- Bumping on pull requests people open; their check keeps failing until they raise `Version`.

## Decisions

- **The step runs only for Dependabot and only when the bump is missing.** `if: github.event_name == 'pull_request' && github.actor == 'dependabot[bot]'`, placed before the existing version check. It checks out the pull request's head branch (not the merge ref) without persisting credentials, runs `Assert-VersionBumped.ps1 -Base origin/<base> -Head HEAD`, and only on a missing bump runs `Step-Version.ps1`, commits as the Actions bot ("Raise Version to <version> for the dependency update") and pushes to the head branch with `DEPENDABOT_BUMP_TOKEN` on the URL. It sets an output `pushed`; the existing version check is skipped when it is true, and the rest of the job still builds the old merge ref (harmless and informative), while the pushed commit's own run is the one the branch protection reads.
  - *Rejected: exempt Dependabot and bump on `main` after the merge.* A bot push to `main` is blocked by the ruleset, and a bump after the merge would release without the review the owner wants.
  - *Rejected: group NuGet updates into one monthly pull request.* Fewer bumps, but still by hand; the owner asked for automation. Grouping stays available later.
  - *Rejected: cancel the current run after pushing.* That needs `actions: write`, which the read-only token has not got.
- **The token is a fine-grained personal access token held as a Dependabot secret.** Contents read and write on `Source191/vantage` only. A Dependabot secret is the only kind the Dependabot-triggered run can read, and a personal token's push starts the new run that `GITHUB_TOKEN`'s would not. When the secret is empty (a person pushed to the Dependabot branch, so the run has repository secrets instead), the step prints a notice to bump by hand and does nothing.
- **`Step-Version.ps1` raises the last number.** It reads `<Version>` with the regex `Assert-VersionBumped.ps1` uses, raises the trailing number of the prerelease label when there is one (`0.17.0-preview.2` to `0.17.0-preview.3`) and otherwise the patch (`0.17.0` to `0.17.1`), rewrites only that text in the csproj (UTF-8, no BOM, as the file is) and prints the new version; `-WhatIf` prints without writing. A prerelease label with no trailing number is refused with a message, so the step fails rather than guessing.
- **Dependabot rebases drop the bump commit** when it updates the pull request; the step runs again on the new push and raises the version from the rebased csproj, so the result is right whatever `main` did in between.

## Risks / Trade-offs

- [The token expires] → the push fails with GitHub's message, the step prints a notice naming the secret, and the README says how to renew it.
- [Two Dependabot pull requests are open at once and both bump to the same version] → "Require branches to be up to date" makes the second update its branch after the first merges; Dependabot's rebase drops its bump and the step raises again from the new base.
- [The job's later steps run on the old merge ref after the push] → they are informative only; the new commit's run is what the branch protection requires.
