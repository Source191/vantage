# Tasks

## 1. The catalogue (Application)

- [x] 1.1 Red: `PullRequestCatalogueShould.AskForOneBranchAtATime` refreshes two branches of one checkout and asserts the fake host was asked twice, once per branch, and `Find` answers each row from its own lookup. Green: `IPullRequests.ListAsync` takes a `BranchName`, `FakePullRequests` answers per repository and branch and records requests, and the catalogue keys `answers` and `running` by checkout and branch. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` passes (the callers compile against the new signature in 2.1).
- [ ] 1.2 Red: `KeepAListedAnswerForAMinute` (no second call at 50 seconds, a new one at 61) and `KeepAnUnavailableAnswerForThreeMinutes` (no second call at two minutes for a not-signed-in answer), with the fake clock. Green: the two windows. Verify both pass.
- [ ] 1.3 Red: `RunAtMostFourLookupsAtOnce` refreshes twelve pairs against a fake host that holds each call until released and asserts four are in flight, then the rest follow. Green: the `SemaphoreSlim(4)` around the host call. Verify the fact passes and the existing facts for no-remote and unknown-host answers still pass.

## 2. The callers (Application)

- [ ] 2.1 Red: `WorktreeInventoryShould.LookUpOnlyTheBranchesWithRows` scans a root holding five repositories of which two have worktrees and asserts the fake host was asked for those two repositories' branches only, and nothing for a leftover row. Green: `WorktreeInventory` passes each registered row's repository and branch to `RefreshStaleAsync`. Verify the Core suite passes.
- [ ] 2.2 Red: `SessionRowEnricherShould.LookUpEachRowsBranch` asserts a session with a branch asks for that branch and a session with none asks for nothing. Green: the enricher passes checkout and branch pairs. Verify the Core suite passes.

## 3. The hosts (Infrastructure)

- [ ] 3.1 Red: `GitHubPullRequestsShould.AskForTheBranchOnly` asserts the request holds `--head fix-123` and `--limit 20` and no `--limit 100`. Green: the request. Verify `dotnet test test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build` passes.
- [ ] 3.2 Red: `AzureDevOpsPullRequestsShould.AskForTheBranchOnly` asserts `--source-branch fix-123` and `--top 20`; `BuildTheLinkFromTheRemote` feeds az's real output shape (no `webUrl`) for organisation northwind, project "Northwind Trading", repository Northwind.Orders and pull request 4521 and asserts the row's URL is `https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders/pullrequest/4521`. Green: the request and the link; the sample file replaced by the real shape. Verify the Infrastructure suite passes.
- [ ] 3.3 Red: `AzureDevOpsPullRequestsShould.KeepABranchWithASlash` and the GitHub twin assert a branch `feature/republisher` round-trips through the request and the row. Green: nothing, if 3.1 and 3.2 already pass it; otherwise the adapter. Verify both pass.

## 4. Words and docs

- [ ] 4.1 Red: `WordingOfPullRequestsShould` asserts the no-pull-request sentence reads "No pull request found for <row>'s branch." with no mention of 100. Green: the sentence. Verify the Core suite passes.
- [ ] 4.2 README's "Pull requests" section: lookups run per branch for rows on screen, the 60-second and three-minute windows, four at once, and what a lookup costs; remove the "latest 100" wording. Verify by reading the section against design.md.

## 5. Version and checks

- [ ] 5.1 Raise `Version` one preview step above `main`'s (a minor raise). Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` all pass.
- [ ] 5.2 The product owner installs the build with `tools\install-local.ps1`, adds a worktree on an Azure DevOps branch with an open pull request (the section 8 commands of the test page, if the earlier ones were cleaned up) and a GitHub branch with one, opens Vantage, presses `w` with Task Manager open, then marks the GitHub pull request ready with `gh pr ready`. Verify the Azure DevOps row shows `!<n> active` within about 75 seconds, `p` opens its page, the GitHub row changes from draft to open within about 75 seconds, and the CPU shows no 20-second spike; record the timings here.

## Workflow follow-up

- Archive the change in the same pull request, syncing the delta spec into `openspec\specs`.
- Remove the throwaway worktrees from 5.2 with the test page's clean-up commands.
