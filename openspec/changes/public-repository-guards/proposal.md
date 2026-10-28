# Proposal

## Why

`Source191/vantage` is about to go public (P14 of `public-release`), and its author wants every change to `main` and every nuget.org publish to need his own approval while the project finds its feet. Today `main` needs a pull request with 0 approvals, and two GitHub Apps with write access to code, workflows and Actions can open and merge one once `check` passes. Any workflow run that asks for `id-token: write` from a file named `publish.yml` can get a nuget.org key, where a version can never be replaced.

## What Changes

- **Code owner:** a new `.github\CODEOWNERS` names `@simonvane` for every path. That lets a repository ruleset require his review on any pull request he did not open himself, while his own pull requests merge through the ruleset's bypass.
- **Publishing needs approval:** `publish.yml`'s job runs in a GitHub environment named `nuget`. The environment needs his approval and only accepts runs started from `main`. The nuget.org Trusted Publishing policy is limited to that environment, so no run gets a key without him clicking Approve.
- **Docs:** the README's Continuous integration section and CLAUDE.md describe the environment, the code owner and the repository rules. `public-release`'s P14 tasks and design take the stricter workflow-run approval (all external contributors, not only first-time ones) and point at this change for the settings.
- **App access:** the `claude` GitHub App's installation is limited to selected repositories, done by hand in the browser alongside uninstalling CodeRabbit. A workflow it pushes to a branch would otherwise run with `CLAUDE_CODE_OAUTH_TOKEN`.
- **Repository settings, applied by hand with `gh` (recorded in tasks.md, not in files):** the `nuget` environment; wiki and projects off; an "Owner approves main" ruleset; a "Release tags" ruleset. After the repository goes public: a six-month collaborators-only interaction limit, approval for every external contributor's workflow runs, private vulnerability reporting, secret scanning with push protection, and Dependabot alerts.

Out of scope: rewriting history (the root commit's work address stays, by decision); removing CodeRabbit, which the author does in the browser; requiring `check` of administrators (`enforce_admins` stays off, by decision); signing commits or tags; multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Repository rules and CI gates change nothing Vantage does, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Repository files:** new `.github\CODEOWNERS`; `.github\workflows\publish.yml` (one `environment:` key and its comment).
- **Docs:** README (Continuous integration), CLAUDE.md (the release bullet), `openspec\changes\public-release` (design risk line, tasks 9.2 and 9.3).
- **GitHub settings:** environment, rulesets, wiki and projects, and the post-public settings above.
- **nuget.org:** the Trusted Publishing policy gains the environment `nuget`.
- **Version:** no bump; nothing under `src\` changes.
