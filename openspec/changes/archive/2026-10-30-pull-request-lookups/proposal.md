# Proposal

## Why

The pull-request column failed its 0.15.0 test on Azure DevOps and was slow and heavy on both hosts. `az repos pr list` leaves `repository.webUrl` out of every pull request, so the parser throws on the first row, the whole listing counts as failed, the cell stays blank and `p` says az could not list the pull requests (the adapter tests passed because their sample has a webUrl). The worktrees view refreshes the catalogue for every repository under the roots, about 47 az listings of 100 pull requests each (0.9 MB apiece) every three minutes, which the owner measured as 80 % CPU for 20 seconds and a Wi-Fi peak near 8 Mbps on opening the view, and a change on GitHub took over three minutes to show. The owner asked for lookups per branch instead, which also removes the "among the latest 100" limit that can hide an older open pull request on a busy repository.

## What Changes

- **One lookup per row's branch.** Each host is asked for the pull requests of one branch (`gh pr list --head`, `az repos pr list --source-branch`), for the rows on screen only, never for every repository under the roots. A busy repository's older open pull request is found, and the footer no longer says "among the latest 100".
- **The Azure DevOps link is built from the remote.** The page is `https://dev.azure.com/<organisation>/<project>/_git/<repository>/pullrequest/<id>`, URL-encoded, from the facts Vantage already holds; `webUrl` is not read.
- **A shorter window.** A branch's answer is reused for 60 seconds (a failed or unavailable one for three minutes, so a signed-out CLI is not asked every minute), so a change shows within about 75 seconds of it happening.
- **A bounded burst.** At most four host lookups run at once, so opening the worktrees view no longer starts dozens of processes together.
- **Version:** a minor raise, because the host port and the footer's words change.

Out of scope: pull requests on hosts other than GitHub and Azure DevOps, a pull-request column for repositories with no row on screen, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `pull-requests`: "Lookups are made once per repository and cached" is removed and replaced by "Lookups are made per branch and cached"; "The pull request key opens it in the browser" says how an Azure DevOps link is built; "A missing pull request is explained" loses the "latest 100" limit.

## Impact

- **Core:** `Ports\IPullRequests.cs` (a branch on `ListAsync`), `Application\PullRequestCatalogue.cs` (answers keyed by checkout and branch, the two windows, the lookup limit), `Application\SessionRowEnricher.cs` and `Application\WorktreeInventory.cs` (refresh per row), `Application\WordingOfPullRequests.cs`.
- **Infrastructure:** `PullRequests\GitHubPullRequests.cs`, `PullRequests\AzureDevOpsPullRequests.cs` (the branch flag, the smaller page, the link from the remote), `PullRequests\PullRequestHosts.cs`.
- **Tests:** PullRequestCatalogueShould, SessionRowEnricherShould, WorktreeInventoryShould, GitHubPullRequestsShould, AzureDevOpsPullRequestsShould (a sample in az's real shape), WordingOfPullRequestsShould, the FakePullRequests double.
- **Docs:** README's "Pull requests" section (how lookups run and what they cost); the pull-requests spec.
- **Release:** a minor version raise; a check against a real Azure DevOps pull request at the terminal.

## Order

Pull request 3 of six for the 0.15.0 test findings, which merge one after another in this order, each raising the version one preview step: 1 attach-and-command-line-clean-up, 2 header-tooltips-and-table-fit, 3 pull-request-lookups, 4 session-tabs-and-delete, 5 dialog-fixes, 6 dependabot-version-bump. The redesign changes follow (the WHERE column, the slim header with the key bar and the Sessions and Worktrees tabs, the one-form wizard, the detail pane), and 0.17.0 with the preview suffix dropped is the release to publish once the six have merged.
