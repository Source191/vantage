# Tasks

## 1. Repository files and docs

- [x] 1.1 New `.github\CODEOWNERS` with a comment line and `* @simonvane`; verify GitHub shows no CODEOWNERS errors on the pull request's Files tab (`gh api repos/Source191/vantage/codeowners/errors?ref=<branch>` returns `"errors":[]`)
- [x] 1.2 `publish.yml`: `environment: nuget` on the `publish` job, with a comment in the file's style saying the environment's approval and `main`-only rule are what make the Trusted Publishing key safe; verify `actionlint` passes if installed, else that `check` passes on the pull request
- [x] 1.3 README, Continuous integration: the publishing paragraph names the `nuget` environment and its approval; the protection sentence names the owner approval ruleset, CODEOWNERS and the release tag ruleset. CLAUDE.md's release bullet gets the same facts in one clause; verify `dotnet format --verify-no-changes` and `openspec validate --all` show no new failure
- [x] 1.4 `public-release`: design's risk line says approval for all external contributors, and tasks 9.2 and 9.3 point at this change; verify `openspec validate public-release` passes

## 2. Before going public (gh, one command at a time, each read back)

- [x] 2.1 Create the `nuget` environment with `PUT repos/Source191/vantage/environments/nuget` and the body `{"can_admins_bypass":false,"deployment_branch_policy":{"protected_branches":false,"custom_branch_policies":true}}`, then `POST .../environments/nuget/deployment-branch-policies` with `{"name":"main","type":"branch"}`; verify `gh api repos/Source191/vantage/environments/nuget` and `.../deployment-branch-policies` show exactly that and nothing else. Required reviewers are refused (422) on a private repository on the Team plan, so they are added in 3.2, straight after the flip
- [x] 2.2 Simon sets Environment `nuget` on the nuget.org Trusted Publishing policy and notes whether it is still pending; verify the policy page lists the environment. Done 28 Oct 2026: policy `vantage-publish` is Active (not pending), locked to repository owner #156836384 and repository #1431992095, which match `gh api repos/Source191/vantage`, with Environment `nuget`
- [x] 2.3 Turn off wiki and projects with `gh repo edit Source191/vantage --enable-wiki=false --enable-projects=false`; verify `gh repo view --json hasWikiEnabled,hasProjectsEnabled` shows both false
- [x] 2.4 Create the "Owner approves main" ruleset (design.md, Decisions); verify `gh api repos/Source191/vantage/rulesets/<id>` shows 1 approval, code owner review, and the admin role as the only bypass actor in `pull_request` mode
- [x] 2.5 Create the "Release tags" ruleset (`refs/tags/v*`, update and deletion, admin role bypass always); verify it the same way
- [x] 2.6 CodeRabbit is uninstalled from Source191 and revoked under Simon's applications; verify `gh api orgs/Source191/installations` no longer lists `coderabbitai`
- [x] 2.7 Simon limits the `claude` installation to selected repositories in the browser (vantage only if he uses cloud sessions on it); verify `gh api orgs/Source191/installations` shows `claude` with `repository_selection` `selected`

## 3. Going public

- [ ] 3.1 `gh repo edit Source191/vantage --visibility public --accept-visibility-change-consequences`, only after Simon confirms at that moment; verify `gh repo view --json visibility` shows PUBLIC
- [ ] 3.2 The `nuget` environment's required reviewer: `PUT .../environments/nuget` with 2.1's body plus `"reviewers":[{"type":"User","id":1423185}],"prevent_self_review":false`; verify the environment shows the `required_reviewers` rule with `simonvane`, and still `can_admins_bypass` false and the `main` policy
- [ ] 3.3 Interaction limit: collaborators only for six months; verify `gh api repos/Source191/vantage/interaction-limits` shows the limit and its expiry
- [ ] 3.4 Fork pull request workflow runs need approval for all external contributors; verify `gh api .../actions/permissions/fork-pr-contributor-approval` shows `all_external_contributors`
- [ ] 3.5 Private vulnerability reporting on; verify `gh api .../private-vulnerability-reporting` shows `enabled: true` and SECURITY.md's link opens the report form
- [ ] 3.6 Secret scanning, push protection and Dependabot alerts on; verify `security_and_analysis` shows them enabled and `gh api .../vulnerability-alerts` answers 204
- [ ] 3.7 Read back: branch protection still requires `check` with `strict: true`, both rulesets and the organisation ruleset are active, and the environment has its reviewer and `main` policy; record the outputs in this pull request's description

## Workflow follow-up

- At the owner's request the first pull request (#80, groups 1 and 2) merged before the flip. Group 3's ticks, its read-back and the archive go in a follow-up pull request (docs only, no version bump), which completes the change.
- Tick the matching boxes on the Notion test plan's step 18.
