# Tasks

## 1. Domain

- [x] 1.1 `RemoteRepositoryShould.ParseGitHubHttpsAndSshRemotes` (with and without `.git`, `ssh://git@github.com/...`): implement `RemoteRepository.Parse` returning `GitHubRepository(Owner, Name)`; verify the facts pass
- [x] 1.2 `RemoteRepositoryShould.ParseAzureDevOpsRemotes` (`https://northwind@dev.azure.com/...`, `https://northwind.visualstudio.com/...` with and without `DefaultCollection`, `git@ssh.dev.azure.com:v3/...`, `northwind@vs-ssh.visualstudio.com:v3/...`) and `DecodeEscapedProjectNames` (`Northwind%20Trading`): implement `AzureDevOpsRepository(Organisation, Project, Name)`; verify the facts pass
- [x] 1.3 `RemoteRepositoryShould.AnswerNullForOtherHostsBlankTextAndUnsafeAzureNames` (GitLab, a local path, null, a project holding `&`, `%26` decoded to `&`, `|`, `^`): implement the host check and the `[A-Za-z0-9 ._-]` allowlist; verify the facts pass
- [x] 1.4 `PullRequestsShould.PreferTheOpenOneCountingDraftsAndActive` and `OtherwiseTakeTheMostRecent`, `AnswerNothingForNoPullRequests`: implement `PullRequest`, `PullRequestHost`, `PullRequestState` (with `IsOpen`) and `PullRequests.ForBranch`; verify the facts pass
- [ ] 1.5 Run Stryker scoped to the new Domain files (`--mutate "**/Domain/RemoteRepository.cs"` and `"**/Domain/PullRequest*.cs"`, `--test-runner mtp`); verify no surviving mutant is left unexplained

## 2. Ports and adapters

- [x] 2.1 `GitCliShould.AnswerTheOriginUrl` and `AnswerNullWithoutAnOrigin`: add `IGit.OriginUrlAsync` (nullable query, documented in the interface comment) and implement it in `GitCli` with `git remote get-url origin`; verify against `FakeProcessRunner` and one `GitCliIntegrationShould` fact on a `TemporaryGitRoot` with and without a remote
- [x] 2.2 Add the `IPullRequests` port with `PullRequestListing` (`Listed`, `Unavailable(PullRequestGap)`) and `PullRequestGap`; add `FakePullRequests` to TestDoubles answering per repository and counting calls, with a gate so a test can hold a call open; verify `FakePullRequests` has its own fact for the gate in Core.Tests and `LayeringShould` passes
- [x] 2.3 `GitHubPullRequestsShould.ListEveryBranchFromOneCall` (recorded `gh pr list --json` output, asserting the exact argument list including `--repo northwind/Northwind.Orders`, the working directory, and the open, draft, merged and closed mappings): implement `GitHubPullRequests`; verify the facts pass
- [x] 2.4 `GitHubPullRequestsShould.ReportCliMissingWhenGhCannotStart`, `ReportNotSignedInOnExitCodeFour`, `ReportFailedOnAnythingElse`: implement the gap mapping; verify the facts pass
- [x] 2.5 `AzureDevOpsPullRequestsShould.ListEveryBranchFromOneCall` (recorded `az repos pr list --output json`, asserting the argument list, `refs/heads/` dropped, the URL built from `repository.webUrl`, and the active, draft, completed and abandoned mappings): implement `AzureDevOpsPullRequests` running the full path of `az.cmd`; verify the facts pass
- [x] 2.6 `AzureDevOpsPullRequestsShould.ReportCliMissingWithoutAzOrTheExtension`, `ReportNotSignedInOnTheLoginHint`, `ReportFailedOnAnythingElse`: implement locating `az.cmd` on PATH and the gap mapping; verify the facts pass
- [x] 2.7 `PullRequestHostsShould.SendEachRemoteToItsHost`: implement the dispatching `PullRequestHosts : IPullRequests`; verify the fact passes

## 3. Catalogue and rows

- [x] 3.1 `PullRequestCatalogueShould.AnswerLookingBeforeTheFirstListing` and `AnswerFromTheListingOnceItArrives`: implement `PullRequestCatalogue.Find` and `RefreshStaleAsync` with sociable wiring (the real `GitCli` over `FakeProcessRunner` for the origin URL, `FakePullRequests` for the host); verify the facts pass
- [x] 3.2 `PullRequestCatalogueShould.CallOncePerRepositoryForManyBranches`, `NotCallAgainWithinThreeMinutes`, `CallAgainAfterThreeMinutesKeepingTheOldAnswerMeanwhile` (with `FakeTimeProvider`): implement the cache window; verify the facts pass
- [x] 3.3 `PullRequestCatalogueShould.NotStartASecondCallWhileOneRuns` (two overlapping `RefreshStaleAsync` calls with the fake's gate held) and `CacheAnUnavailableAnswerForTheWindow`: implement the in-flight guard; verify the facts pass
- [x] 3.4 `PullRequestCatalogueShould.AnswerNoRemoteAndUnknownHostWithoutCallingAHost` and `AnswerNoBranchForADetachedHead`: implement those gaps; verify the facts pass
- [x] 3.5 `SessionRowEnricherShould.CarryEachRowsPullRequest` and `NotWaitForTheLookup` (the fake's gate held while `EnrichAsync` completes): keep the main checkout path in `Location`, add `PullRequestLookup PullRequest` to `SessionRow`, call `Find` per row and start `RefreshStaleAsync` without awaiting; verify the facts pass and every existing `SessionRow` construction compiles with the new field
- [x] 3.6 `WorktreeInventoryShould.CarryEachRowsPullRequest` and `WorktreeInventoryLeftoversShould.GiveLeftoversNoBranch`: add the field to `WorktreeRow` and fill it in `WorktreeInventory`; verify the facts pass

## 4. Keys, footers and handler

- [x] 4.1 `WordingShould.SayThePullRequestCell` ("#123 open", "#88 merged", "!4521 draft", "!4460 completed", empty for every `Missing`): implement `Wording.PullRequestCell`; verify the fact passes
- [x] 4.2 `SessionsViewShould.OpenTheSelectedRowsPullRequest`, `ExplainAMissingPullRequest`, `NeedASelectionToOpenAPullRequest`: add the `OpenPullRequest` effect, `Footer.NoPullRequest(RowName, Gap, Host)` and the `p` case in `SessionsView`; verify the facts pass
- [x] 4.3 `WorktreesViewShould.OpenTheSelectedRowsPullRequest`, `ExplainAMissingPullRequest`, `PruneWithCapitalP` and `NotPruneWithLowerP`: add the `p` case and move prune to `P` in `WorktreesView`; verify the facts pass
- [x] 4.4 `WordingShould.SayWhyThereIsNoPullRequest` (one fact per `PullRequestGap`, naming gh or az with the azure-devops extension from the host): implement the footer words; verify the reflection fact that every footer case has words passes
- [x] 4.5 `OpenPullRequestHandlerShould.OpenTheUrlAndReportIt`: implement `OpenPullRequestHandler` over `IDesktop.OpenAsync` from `folder-column-and-open-keys`, returning `OpenPullRequestOutcome(Host, Number)` declared in its own file; verify the fact passes
- [x] 4.6 `WordingOfOutcomesShould.SayThePullRequestWasOpened` ("Opened !4521 in the browser"): add the outcome's words; verify the reflection fact passes
- [x] 4.7 Register `OpenPullRequestHandler` in `Composition.Handlers` (`IDesktop` is already in `EffectPorts` from `folder-column-and-open-keys`); verify `EffectDispatcherShould` passes

## 5. Screens, wiring and documentation

- [x] 5.1 Add the PR column after BRANCH in `SessionsScreenDefinition` and `WorktreesScreenDefinition` (cell from `Wording.PullRequestCell`), the `<p> Pull request` hint in both views and `<P> Prune` in the worktrees view, and the `p` and `P` lines in `Wording.SessionsHelp`, `Wording.WorktreesHelp` and their help glossary; verify `WordingShould`'s help facts pass and `dotnet build -c Debug` is clean
- [x] 5.2 Wire `PullRequestHosts`, `PullRequestCatalogue` and the enricher and inventory changes in `Program.cs`; verify `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass
- [ ] 5.3 Run `dotnet run --project tools\HeadlessTerminal -- check` and a `drive` that opens Vantage, waits past one lookup, moves to a row with a pull request and presses `p`, then `w` and `p`, capturing to `drive-raw.txt`; verify the check exits 0, the PR column shows in both captures and the footer names the opened pull request
- [x] 5.4 README: the PR column in both column tables, `p` in both key tables, `P` for prune, and a short "Pull requests" note (signing in with `gh auth login` and `az login` plus `az extension add --name azure-devops`, the three-minute refresh, the hosts read from `origin`); CLAUDE.md: the `IGit.OriginUrlAsync` convention line and a fact that `gh` and `az` words are read only in the two pull request adapters; verify the README's keys match `Wording`'s help text
- [x] 5.5 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next minor after `main`'s; verify `.github\scripts\Assert-VersionBumped.ps1` passes locally

## Workflow follow-up

- P9 in `public-release`'s sequence: the features stream, stacked on P8 (`folder-column-and-open-keys`).
- Merge after `folder-column-and-open-keys`; rebase the column lists and help text on `main` if `header-and-help` or `remove-session-descriptions` landed in between.
- Archive the change in the same pull request (`/opsx:archive`), so `openspec\specs\pull-requests` describes `main`; verify `openspec validate --all` passes after archiving.
