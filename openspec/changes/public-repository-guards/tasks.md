# Tasks

## 1. Repository files and docs

- [ ] 1.1 New `.github\CODEOWNERS` with a comment line and `* @simonvane`; verify GitHub shows no CODEOWNERS errors on the pull request's Files tab (`gh api repos/Source191/vantage/codeowners/errors?ref=<branch>` returns `"errors":[]`)
- [ ] 1.2 `publish.yml`: `environment: nuget` on the `publish` job, with a comment in the file's style saying the environment's approval and tag rule are what make the Trusted Publishing key safe; verify `actionlint` passes if installed, else that `check` passes on the pull request
- [x] 1.3 README, Continuous integration: the publishing paragraph names the `nuget` environment and its approval; the protection sentence names the owner approval ruleset, CODEOWNERS and the release tag ruleset. CLAUDE.md's release bullet gets the same facts in one clause; verify `dotnet format --verify-no-changes` and `openspec validate --all` show no new failure
- [x] 1.4 `public-release`: design's risk line says approval for all external contributors, and tasks 9.2 and 9.3 point at this change; verify `openspec validate public-release` passes

## 2. Before going public (gh, one command at a time, each read back)

- [ ] 2.1 Create the `nuget` environment with `PUT repos/Source191/vantage/environments/nuget` and the body `{"reviewers":[{"type":"User","id":1423185}],"prevent_self_review":false,"can_admins_bypass":false,"deployment_branch_policy":{"protected_branches":false,"custom_branch_policies":true}}`, then `POST .../environments/nuget/deployment-branch-policies` with `{"name":"main","type":"branch"}`; verify `gh api repos/Source191/vantage/environments/nuget` and `.../deployment-branch-policies` show exactly that and nothing else
- [ ] 2.2 Simon sets Environment `nuget` on the nuget.org Trusted Publishing policy and notes whether it is still pending; verify the policy page lists the environment
- [ ] 2.3 Turn off wiki and projects with `gh repo edit Source191/vantage --enable-wiki=false --enable-projects=false`; verify `gh repo view --json hasWikiEnabled,hasProjectsEnabled` shows both false
- [ ] 2.4 Create the "Owner approves main" ruleset (design.md, Decisions); verify `gh api repos/Source191/vantage/rulesets/<id>` shows 1 approval, code owner review, and the admin role as the only bypass actor in `pull_request` mode
- [ ] 2.5 Create the "Release tags" ruleset (`refs/tags/v*`, update and deletion, admin role bypass always); verify it the same way
- [ ] 2.6 CodeRabbit is uninstalled from Source191 and revoked under Simon's applications; verify `gh api orgs/Source191/installations` no longer lists `coderabbitai`
- [ ] 2.7 Simon limits the `claude` installation to selected repositories in the browser (vantage only if he uses cloud sessions on it); verify `gh api orgs/Source191/installations` shows `claude` with `repository_selection` `selected`

## 3. Going public

- [ ] 3.1 `gh repo edit Source191/vantage --visibility public --accept-visibility-change-consequences`, only after Simon confirms at that moment; verify `gh repo view --json visibility` shows PUBLIC
- [ ] 3.2 Interaction limit: collaborators only for six months; verify `gh api repos/Source191/vantage/interaction-limits` shows the limit and its expiry
- [ ] 3.3 Fork pull request workflow runs need approval for all external contributors; verify `gh api .../actions/permissions/fork-pr-contributor-approval` shows `all_external_contributors`
- [ ] 3.4 Private vulnerability reporting on; verify `gh api .../private-vulnerability-reporting` shows `enabled: true` and SECURITY.md's link opens the report form
- [ ] 3.5 Secret scanning, push protection and Dependabot alerts on; verify `security_and_analysis` shows them enabled and `gh api .../vulnerability-alerts` answers 204
- [ ] 3.6 Read back: branch protection still requires `check` with `strict: true`, both rulesets and the organisation ruleset are active, and the environment is unchanged; record the outputs in this pull request's description

## Workflow follow-up

- Archive this change in its own pull request before merging it (docs and CI only, no version bump), and merge with the admin bypass once `check` is green.
- Tick the matching boxes on the Notion test plan's step 18.
