# Tasks

## 1. P0: CI for stacked pull requests

- [x] 1.1 Remove `branches: [main]` from `check.yml`'s `pull_request` trigger and update its comment; verify a pull request based on another branch runs `check`, and its version step compares against that branch (`Assert-VersionBumped.ps1` passes for a bump above the parent's)
  - Result: commit 596ded1 removed the filter; `check.yml` runs on pull requests to any branch, and the stacked P2 to P9 pull requests were each checked against their parent.
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

- [x] 8.1 Install the latest GitHub Release's nupkg with `tools\install-local.ps1 -FromArtifact` in a shell with NO_COLOR, CLAUDECODE, CLAUDE_PID and CLAUDE_CODE_* cleared; verify `vantage` starts and the header shows the release's version
  - Result (27 Oct 2026): the v0.15.0 release nupkg installed with `-FromArtifact`; the manual test plan in Notion ("Vantage 0.15.0 manual test and release plan"), section 1, is ticked.
- [x] 8.2 The README's "Manual checks for tab and attach changes", all six; verify each passes or has a fix pull request
  - Result: the Notion plan's sections 5, 6 and 14 cover the tab and attach checks and are ticked; PR #84 passed them again on 0.17.0-preview.1. The failures noted there are listed under 8.9.
- [x] 8.3 `header-and-help`: the tab title carries the version at start and after attaching and Ctrl+Z; the key-hint tooltips; verify both
  - Result: the Notion plan's section 2, ticked. The version moved from the tab title to the header (`cli-help-and-tab-title`), so the check became a plain `Vantage` title before and after attaching, with the version in the header.
- [x] 8.4 `settings-and-worktree-root`: `,` saves and applies without a restart; a new worktree lands under the worktree root; verify both
  - Result: the Notion plan's section 9, ticked.
- [x] 8.5 `folder-column-and-open-keys` and `pull-request-column`: `c`, `e`, `v` and `p` in both views against a GitHub and an Azure DevOps repository; verify each opens or copies the right thing
  - Result: the Notion plan's sections 7 and 8, ticked; Azure DevOps rows showed no pull request, listed under 8.9.
- [x] 8.6 `windows-terminal-setup`: `vantage setup-terminal` (decline, then `--yes`), the two profiles after a Windows Terminal restart, the two bell sounds, a tab opened with `o` using "Vantage session", what `wt new-tab --profile "Vantage session"` does before the restart (if it fails rather than falling back, add a retry without `--profile`, test first), `--remove` and `o` back in the default profile; verify each
  - Result: the Notion plan's section 12, ticked; `o` opened its tab normally before the Windows Terminal restart, so no retry without `--profile` was needed.
- [x] 8.7 `update-notice`: with a folder source holding a higher version and a recorded `update-check.json`, `U` updates in its own tab and Vantage restarts on the new version; remove the source afterwards; verify the new version shows in the header
  - Result: the Notion plan's section 13, ticked, with a fake 0.99.0 in a folder source; the kit was removed afterwards.
- [x] 8.8 `wt-title-escaping`: run its spike, record the facts in CLAUDE.md and archive it in a pull request (patch only if a shipped file changes); verify `openspec list` no longer shows it
  - Result: run and archived in PR #84 (28 Oct 2026); the facts are in CLAUDE.md's Windows Terminal section.
- [x] 8.9 Every fix found in 8.2 to 8.7 merged; verify the GitHub Release to publish is named here with its tag
  - Result: published as prereleases, 0.16.2-preview.1 and 0.16.2-preview.2, after PR #78 made prerelease versions GitHub prereleases. The fixes found by the test that are not yet made (Azure DevOps pull requests, column widths, tab clean-up on rename, `d` and `b`, the wizard's keys and others) stay as suggestions on the Notion plan, to start as their own OpenSpec changes; they no longer block this change.

## 9. P14: go public

- [x] 9.1 `git log --all --format=%ae | Sort-Object -Unique` lists only addresses meant for the public, and a secret scan of the whole history (`gitleaks git`) finds nothing; verify both outputs. Done 28 Oct 2026: gitleaks 8.30.1 found nothing in 462 commits, including merge diffs and the 77 pull request refs; the root commit's work address stays, by the owner's decision, because rewriting the root changes every commit and the pull request refs would keep it visible anyway.
- [x] 9.2 No workflow uses `pull_request_target`; GitHub's approval for all external contributors' workflow runs is on (`public-repository-guards` 3.3); verify with `Select-String -Path .github\workflows\*.yml -Pattern pull_request_target` and the repository's Actions settings
- [x] 9.3 Make the repository public, then turn on private vulnerability reporting and check branch protection on `main` still requires `check` and up-to-date branches, with the guards in `public-repository-guards` (groups 2 and 3) applied around the flip; verify in the repository settings
- [x] 9.4 nuget.org: sign in, create the Trusted Publishing policy (`Source191`, `vantage`, `publish.yml`), add the `NUGET_USER` secret; verify the policy is listed
  - Result: the policy and the `NUGET_USER` secret are in place; `publish.yml` succeeded on 28 Oct 2026 (twice).
- [x] 9.5 Run `publish.yml` with the tag from 8.9; verify the package page shows the version, the readme and the licence once validation finishes
  - Result: nuget.org lists 0.16.2-preview.1 and 0.16.2-preview.2. No stable version is published yet; that is an ordinary release from now on, not part of this change.
- [x] 9.6 In a clean Windows profile or Windows Sandbox: `dotnet tool install -g Source191.Vantage`, then `vantage`; verify it starts and, a day later or with `update-check.json` removed, makes its first update check without error
  - Result (PR #85, Windows Sandbox): `dotnet tool install -g Source191.Vantage --prerelease` installed 0.16.2-preview.2 and `vantage` started. An installed prerelease never checks for updates, so the first update check waits for the first stable version.
- [x] 9.7 Archive this change in a pull request (docs only, no bump); verify `openspec list` shows no open change from this sequence
  - Result: archived with `packed-tool-smoke-test` in one docs-only pull request.

## Workflow follow-up

- Two worktrees, two Claude sessions: one takes the refactors stream (P2 to P5), the other P10 then P6; the features stream continues from P7 once P3, and P5 if merged, are on `main`.
- Merge order: P0, P1, P2 to P5 in one burst, P10 and P6 as they are ready, P7 to P9 in one burst, P11, P12, then P13 and P14.
- This change spans several pull requests: P0, P11, P13's fixes and P14 carry its own tasks; the others tick a group here as their change merges. It is archived in P14.
