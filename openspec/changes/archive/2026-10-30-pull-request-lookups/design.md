# Design

## Context

`PullRequestCatalogue` keeps one answer per main checkout (`FolderPath.Key`), refreshed after three minutes by `RefreshStaleAsync`, which `SessionRowEnricher` calls with its rows' checkouts and `WorktreeInventory` with every repository it scanned. `IPullRequests.ListAsync(repository, checkout)` runs `gh pr list --state all --limit 100` or `az repos pr list --status all --top 100` through `IProcessRunner`, and `Find(checkout, branch)` picks the branch's pull request out of the listing with `PullRequests.ForBranch`. The Azure DevOps row builds its link from `repository.webUrl`, which the real az output omits (confirmed on 27 October with the real adapter), so `Row` throws `KeyNotFoundException` and `Parse` answers `Unavailable(Failed)` for the whole listing. `RemoteRepository.Parse` already yields the organisation, project and repository name, decoded, and refuses names with characters outside letters, digits, spaces, dots, hyphens and underscores.

## Goals / Non-Goals

**Goals:**
- A lookup costs one small process and answers for one branch, so the work scales with rows on screen, not repositories on disc.
- A pull request is found however old the repository's history is.
- A signed-out or missing CLI is not asked every minute per row.

**Non-Goals:**
- Caching answers across runs of Vantage.
- A pull request for a row with no branch (a detached head has nothing to look up).

## Decisions

- **The port takes the branch.** `IPullRequests.ListAsync(RemoteRepository, FolderPath checkout, BranchName branch, CancellationToken)` answers the listing for that branch; `BranchPullRequest` stays so a host that returns another branch's pull request (az matches `--source-branch` on the ref name, which `Row` still checks) is filtered by `ForBranch` as today. `FakePullRequests` answers per repository and branch and records each request, so `WorktreeInventoryShould` can assert which branches were asked for.
- **The catalogue is keyed by checkout and branch.** `answers` and `running` use `$"{checkout.Key}|{branch.Value}"`; `RefreshStaleAsync` takes `(FolderPath Checkout, BranchName Branch)` pairs, and `Find` reads the pair's answer. Two windows: `Window` of 60 seconds for a `Listed` answer and `UnavailableWindow` of three minutes for an `Unavailable` one. The origin URL is read once per refresh as today.
  - *Rejected: 60 seconds for every answer.* A signed-out az costs about 1.5 seconds of CPU per call; twelve rows would burn that every minute for nothing.
- **At most four lookups at once**, through a `SemaphoreSlim(4)` taken inside `RefreshAsync` around the host call, so a burst of rows becomes waves of four; `running` still stops a second lookup for the same pair. Four is below the owner's observed pain (47 at once) and above the handful of rows that usually change together.
- **The host commands.** GitHub: `gh pr list --repo <owner>/<name> --head <branch> --state all --limit 20 --json number,url,state,isDraft,headRefName,createdAt`. Azure DevOps: `az repos pr list --org <url> --project <project> --repository <name> --source-branch <branch> --status all --top 20 --output json`. Twenty leaves room for a branch reused across several pull requests; `ForBranch` still prefers the open one, then the newest.
- **The Azure DevOps link comes from `AzureDevOpsRepository`.** `https://dev.azure.com/{organisation}/{Uri.EscapeDataString(project)}/_git/{Uri.EscapeDataString(name)}/pullrequest/{id}`; the adapter stops reading `repository.webUrl`. The test sample is replaced by az's real shape (no `webUrl`, the fields Vantage reads only), and a fact covers a project with a space, as the host spec's scenario names it.
- **Callers refresh per row.** `SessionRowEnricher` passes each row's checkout and branch; `WorktreeInventory` passes each registered row's repository and branch (rows without a branch, leftovers included, ask for nothing), so the 47-repository sweep goes with no other change to the scan.

## Risks / Trade-offs

- [More processes per refresh than one listing per repository when many rows share a repository] → each is a 20-row answer instead of a 100-row one, the limit bounds the burst, and rows outside the window cost nothing; measured at the terminal in the last task.
- [`az repos pr list --source-branch` expects the short name on some versions and `refs/heads/` on others] → the adapter passes the short name and the row filter keeps whichever form comes back; the terminal check runs against the owner's az.
- [A repository with many pull requests on one branch beyond 20] → the open one is normally the newest; the footer explains "no pull request" otherwise, with no claim about a limit.
