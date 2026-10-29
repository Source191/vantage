# Tasks

## 1. The token (owner)

- [ ] 1.1 Create a fine-grained personal access token for `Source191/vantage` with Contents read and write, and store it as the Dependabot secret `DEPENDABOT_BUMP_TOKEN` (Settings, Secrets and variables, Dependabot). Verify it is listed there and record its expiry date here.

## 2. The script

- [x] 2.1 Write `.github\scripts\Step-Version.ps1` (reads `<Version>` with the Assert script's regex, raises the prerelease's trailing number else the patch, rewrites only that text without a BOM, prints the new version, `-WhatIf` prints without writing, refuses a prerelease with no trailing number). Verify by running it on a temporary copy of the csproj: `0.17.0-preview.2` becomes `0.17.0-preview.3`, `0.17.0` becomes `0.17.1`, `0.17.0-local` is refused, only the Version line differs (`git diff --no-index` on the copies) and `Get-Content -AsByteStream -TotalCount 3` shows no BOM.
  - Recorded 29 October 2026 on temporary copies of the csproj: `0.17.0-preview.2` became `0.17.0-preview.3`, `0.17.0-preview.9` became `0.17.0-preview.10`, `0.17.0` became `0.17.1`, `0.17.0-local` was refused; each copy differed from its original in the Version line only and starts `<Pr` with no BOM; `-WhatIf` printed the next version and left the file unchanged.

## 3. The workflow

- [x] 3.1 Add the "Raise the version for Dependabot" step to `check.yml` as design.md describes (actor condition, head-branch checkout without persisted credentials, the Assert against `origin/<base>`, the script, the commit, the push with the secret, the `pushed` output, the notice when the secret is empty), and skip the existing version check when `pushed` is true. Verify the workflow still parses: push the branch and confirm the pull request's own `check` run starts and the step is skipped for a human actor.
  - Recorded 29 October 2026 on pull request #107 (run 40924363492): the workflow parsed, `check` passed, and for a human actor "Check out the Dependabot branch" and "Raise the version for Dependabot" were skipped while "Check the version was raised" ran.
- [x] 3.2 README's "Version bumps" paragraph: Dependabot pull requests are bumped by the step, the secret's name and how to renew it, `Step-Version.ps1` as the hand tool; `dependabot.yml`'s comment no longer says a bump is needed by hand. Verify by reading both against design.md.
- [ ] 3.3 On the next Dependabot pull request that touches `src\` (or one started from Insights, Dependency graph, Dependabot, "Check for updates"), verify the bot's bump commit appears on the branch, the new run's version check passes and the pull request is green; record the pull request number and run id here.

## Workflow follow-up

- Archive the change once 3.3 is recorded; `skip_specs` means no spec sync.
