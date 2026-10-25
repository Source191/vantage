# Tasks

## 1. P0: CI for stacked pull requests

- [ ] 1.1 Remove `branches: [main]` from `check.yml`'s `pull_request` trigger and update its comment; verify a pull request based on another branch runs `check`, and its version step compares against that branch (`Assert-VersionBumped.ps1` passes for a bump above the parent's)
- [x] 1.2 Add each open change's P-number to its "Workflow follow-up"; verify `openspec validate --all` shows no new failure

## 2. P1: gate

- [x] 2.1 `remove-session-descriptions` merged and archived (minor); verify `openspec list` no longer shows it and `openspec\specs\session-launch` has no description requirement

## 3. P2 to P5: refactors stream

- [x] 3.1 P2 `unreachable-arms` merged and archived (patch); verify `openspec list` no longer shows it
- [x] 3.2 P3 `text-value-doors` merged and archived (patch), stacked on P2; verify `openspec list` no longer shows it
- [x] 3.3 P4 `facts-not-sentences` merged and archived (patch), stacked on P3; verify `openspec list` no longer shows it
- [x] 3.4 P5 `from-factory-spike` decided on its diff: merged (patch) or closed unmerged, with the reason in its tasks.md, and archived either way; verify `openspec list` no longer shows it. Merged: the product owner keeps `From`; the contract theory over all six types is worth the spelling change.

## 4. P6 to P9: features stream

- [x] 4.1 P6 `header-and-help` merged and archived (minor); verify `openspec list` no longer shows it
- [x] 4.2 P7 `settings-and-worktree-root` merged and archived (minor), stacked on P6 and started after P3 (and P5, if merged) are on `main`; verify `openspec\specs\settings` exists
- [x] 4.3 P8 `folder-column-and-open-keys` merged and archived (minor), stacked on P7; verify `openspec\specs\folder-actions` exists
- [x] 4.4 P9 `pull-request-column` merged and archived (minor), stacked on P8; verify `openspec\specs\pull-requests` exists

## 5. P10: standalone

- [x] 5.1 `windows-terminal-setup` merged and archived (minor), any time after P1; verify `openspec list` no longer shows it

## 6. P11: package ready

- [x] 6.1 Tool csproj: `PackageLicenseExpression`, `PackageReadmeFile`, `PackageIcon`, `PackageProjectUrl`, `RepositoryUrl`, `PackageTags` and `Copyright` as in design.md, with the README and a 128 x 128 PNG icon packed; verify `dotnet pack src\Source191.Vantage -c Release` succeeds and the nupkg holds the readme, icon and licence expression (open it as a zip)
- [x] 6.2 `THIRD-PARTY-NOTICES.txt` naming each packed dependency with its licence and full notice text, packed beside the tool; check each transitive licence on its nuget.org page or repository (stop and ask if any is not permissive); verify it lists every package from `dotnet list src\Source191.Vantage package --include-transitive` that ships, and every DLL in the packed `tools\net10.0\any` folder
- [x] 6.3 `.github\workflows\publish.yml` as in design.md, with comments in the style of `release.yml`; verify it parses (`actionlint` if installed, else a dry run with a step that lists the downloaded nupkg instead of pushing)
- [x] 6.4 `SECURITY.md` (report through GitHub private vulnerability reporting) and `.github\dependabot.yml` (nuget and github-actions, weekly); update `release.yml`'s comment about nuget.org; verify `check` passes on the pull request
- [x] 6.5 README: Install leads with `dotnet tool install -g Source191.Vantage` and keeps the local build steps for contributors; Continuous integration describes `publish.yml`; the Roadmap loses the nuget.org item. CLAUDE.md: publishing is `publish.yml` on a tested release tag, never from `release.yml`; verify every command in Install runs as written against a locally packed nupkg
- [x] 6.6 Raise `Version` to the next patch after `main`'s; verify `.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD` passes

## 7. P12: update notice

- [x] 7.1 `update-notice` merged and archived (minor), after P6 and P7; verify `openspec\specs\tool-updates` exists

## 8. P13: final manual test

- [ ] 8.1 Install the latest GitHub Release's nupkg with `tools\install-local.ps1 -FromArtifact` in a shell with NO_COLOR, CLAUDECODE, CLAUDE_PID and CLAUDE_CODE_* cleared; verify `vantage` starts and the header shows the release's version
- [ ] 8.2 The README's "Manual checks for tab and attach changes", all six; verify each passes or has a fix pull request
- [ ] 8.3 `header-and-help`: the tab title carries the version at start and after attaching and Ctrl+Z; the key-hint tooltips; verify both
- [ ] 8.4 `settings-and-worktree-root`: `,` saves and applies without a restart; a new worktree lands under the worktree root; verify both
- [ ] 8.5 `folder-column-and-open-keys` and `pull-request-column`: `c`, `e`, `v` and `p` in both views against a GitHub and an Azure DevOps repository; verify each opens or copies the right thing
- [ ] 8.6 `windows-terminal-setup`: `vantage setup-terminal` (decline, then `--yes`), the two profiles after a Windows Terminal restart, the two bell sounds, a tab opened with `o` using "Vantage session", what `wt new-tab --profile "Vantage session"` does before the restart (if it fails rather than falling back, add a retry without `--profile`, test first), `--remove` and `o` back in the default profile; verify each
- [ ] 8.7 `update-notice`: with a folder source holding a higher version and a recorded `update-check.json`, `U` updates in its own tab and Vantage restarts on the new version; remove the source afterwards; verify the new version shows in the header
- [ ] 8.8 `wt-title-escaping`: run its spike, record the facts in CLAUDE.md and archive it in a pull request (patch only if a shipped file changes); verify `openspec list` no longer shows it
- [ ] 8.9 Every fix found in 8.2 to 8.7 merged; verify the GitHub Release to publish is named here with its tag

## 9. P14: go public

- [ ] 9.1 `git log --all --format=%ae | Sort-Object -Unique` lists only addresses meant for the public, and a secret scan of the whole history (`gitleaks git`) finds nothing; verify both outputs
- [ ] 9.2 No workflow uses `pull_request_target`; GitHub's approval for outside contributors' workflow runs is on; verify with `Select-String -Path .github\workflows\*.yml -Pattern pull_request_target` and the repository's Actions settings
- [ ] 9.3 Make the repository public, then turn on private vulnerability reporting and check branch protection on `main` still requires `check` and up-to-date branches; verify in the repository settings
- [ ] 9.4 nuget.org: sign in, create the Trusted Publishing policy (`Source191`, `vantage`, `publish.yml`), add the `NUGET_USER` secret; verify the policy is listed
- [ ] 9.5 Run `publish.yml` with the tag from 8.9; verify the package page shows the version, the readme and the licence once validation finishes
- [ ] 9.6 In a clean Windows profile or Windows Sandbox: `dotnet tool install -g Source191.Vantage`, then `vantage`; verify it starts and, a day later or with `update-check.json` removed, makes its first update check without error
- [ ] 9.7 Archive this change in a pull request (docs only, no bump); verify `openspec list` shows no open change from this sequence

## Workflow follow-up

- Two worktrees, two Claude sessions: one takes the refactors stream (P2 to P5), the other P10 then P6; the features stream continues from P7 once P3, and P5 if merged, are on `main`.
- Merge order: P0, P1, P2 to P5 in one burst, P10 and P6 as they are ready, P7 to P9 in one burst, P11, P12, then P13 and P14.
- This change spans several pull requests: P0, P11, P13's fixes and P14 carry its own tasks; the others tick a group here as their change merges. It is archived in P14.
