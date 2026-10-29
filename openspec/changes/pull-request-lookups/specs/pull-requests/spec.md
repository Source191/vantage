# Spec Delta

## ADDED Requirements

### Requirement: Lookups are made per branch and cached
Vantage SHALL ask a host for the pull requests of one branch at a time, only for branches that have a row on screen, SHALL reuse a branch's answer for 60 seconds and a failed or unavailable answer for three minutes, SHALL NOT start a second lookup for a branch while one is running, and SHALL run at most four lookups at once.

#### Scenario: Several sessions in one repository
- **WHEN** three sessions run on three branches of the same repository
- **THEN** one lookup is made per branch, each asking the host for that branch only, and each row takes its pull request from its own answer

#### Scenario: Repositories without a row are left alone
- **WHEN** forty repositories sit under the roots and the worktrees view shows rows for three of them
- **THEN** lookups run for those three repositories' branches and for no other repository

#### Scenario: Within the window
- **WHEN** the tables refresh every five seconds for 50 seconds after a branch was looked up
- **THEN** no further lookup is made for that branch

#### Scenario: Window passed
- **WHEN** a refresh happens more than 60 seconds after a branch's last lookup
- **THEN** a new lookup starts and the row keeps the previous answer until it arrives

#### Scenario: Unavailable answer kept longer
- **WHEN** the host CLI reported the user is not signed in and the tables refresh for two minutes
- **THEN** no further lookup is made for that branch until three minutes have passed

#### Scenario: Older open pull request
- **WHEN** a branch's open pull request is older than the repository's latest 100 pull requests
- **THEN** its row still shows that pull request

#### Scenario: Bounded burst
- **WHEN** twelve rows need a lookup at the same moment
- **THEN** four lookups run at once and the rest follow as each finishes

## MODIFIED Requirements

### Requirement: The pull request key opens it in the browser
The pull request key SHALL open the selected row's pull request in the default browser, without passing Vantage's Claude Code session markers to it, and the footer SHALL name it, in both views. An Azure DevOps pull request's page SHALL be built from the organisation, project and repository of the origin remote, URL-encoded, and the pull request's number, never from a URL in the host's answer.

#### Scenario: Open from the sessions view
- **WHEN** the user presses the pull request key on a row whose pull request is !4521
- **THEN** that pull request's page opens in the default browser, without Vantage's Claude Code session markers in the browser's environment, and the footer says !4521 was opened

#### Scenario: Azure DevOps page from the remote
- **WHEN** origin is https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders and the row's pull request is !4521, listed by az with no web URL
- **THEN** the page https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders/pullrequest/4521 opens

### Requirement: A missing pull request is explained
When the selected row shows no pull request, the pull request key SHALL open nothing and the footer SHALL say why: no row is selected, the row has no branch, the repository has no origin remote, the host is not GitHub or Azure DevOps, the `gh` or `az` CLI is not installed, the user is not signed in to it, the lookup is still running, it failed, or the branch has no pull request at all.

#### Scenario: Not signed in
- **WHEN** the user presses the pull request key on a row of an Azure DevOps repository and `az` reports that the user must sign in
- **THEN** nothing opens and the footer says to sign in to the Azure CLI

#### Scenario: CLI missing
- **WHEN** the user presses the pull request key on a row of a GitHub repository and `gh` is not installed
- **THEN** nothing opens and the footer says the GitHub CLI is needed to show pull requests

#### Scenario: Still looking
- **WHEN** the user presses the pull request key before the branch's first lookup has answered
- **THEN** nothing opens and the footer says the pull request is still being looked up

#### Scenario: No pull request
- **WHEN** the user presses the pull request key on a row whose branch has no pull request
- **THEN** nothing opens and the footer says no pull request was found for the row's branch, with no mention of a limit

## REMOVED Requirements

### Requirement: Lookups are made once per repository and cached
**Reason**: One listing of the latest 100 pull requests per repository, for every repository under the roots, cost about 47 az processes and 40 MB every three minutes on the owner's machine and still missed an open pull request older than the latest 100.
**Migration**: "Lookups are made per branch and cached" covers the same rows with one small lookup per branch on screen.
