# Proposal

## Why

Every Dependabot pull request that bumps a package under `src\` fails the first step of `check` (`Assert-VersionBumped.ps1`) until someone raises `Version` by hand, as was done for 0.11.4 and 0.11.5. The owner decided on the 0.15.0 test page (section 17) to automate the bump, so a dependency update needs only a review and a merge.

## What Changes

- **A bump step in `check.yml`.** On a pull request raised by `dependabot[bot]` that touches a tool path without raising `Version`, a step raises it one step (the prerelease number when the version has one, otherwise the patch), commits "Raise Version to <version> for the dependency update" on the pull request branch and pushes it with a token held as a Dependabot secret. The run that push starts is the one that checks the bumped commit; the current run skips its own version check and carries on.
- **A `Step-Version.ps1` script** beside `Assert-VersionBumped.ps1`, usable by hand as well.
- **Docs:** README's "Version bumps" paragraph describes the step and the token; `dependabot.yml`'s comment stops saying a bump is needed by hand.
- **No Vantage version raise:** nothing under `src\` changes. `skip_specs: true`: tooling only, no behaviour of the tool changes.

Out of scope: merging Dependabot pull requests automatically (the owner still approves and merges under the "Owner approves main" ruleset), bumping for pull requests people open, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: the change is CI tooling, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **CI:** `.github\workflows\check.yml` (the new step and the condition on the version check), a new `.github\scripts\Step-Version.ps1`, `.github\dependabot.yml` (its comment).
- **Secrets:** a fine-grained personal access token with Contents read and write on `Source191/vantage`, stored as the Dependabot secret `DEPENDABOT_BUMP_TOKEN`; created by the owner, with an expiry to renew.
- **Docs:** README's "Developing Vantage" section ("Version bumps").

## Order

Pull request 6 of six for the 0.15.0 test findings, which merge one after another in this order: 1 attach-and-command-line-clean-up, 2 header-tooltips-and-table-fit, 3 pull-request-lookups, 4 session-tabs-and-delete, 5 dialog-fixes, 6 dependabot-version-bump (this one raises no version). The redesign changes follow (the WHERE column, the slim header with the key bar and the Sessions and Worktrees tabs, the one-form wizard, the detail pane), and 0.17.0 with the preview suffix dropped is the release to publish once the six have merged.
