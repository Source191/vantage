# pull-requests Specification

## Purpose
Shows the pull request for each listed branch, from GitHub or Azure DevOps, in the sessions and worktrees tables, and opens it in the browser, so the user can see where a branch stands without leaving Vantage.

## Requirements

### Requirement: Rows show their branch's pull request
The sessions view and the worktrees view SHALL show a PR column naming the pull request for the row's branch in its repository: `#` and the number for GitHub, `!` and the number for Azure DevOps, followed by its state. The column SHALL be blank when the row has no branch, the branch has no pull request, or it cannot be looked up.

#### Scenario: Open GitHub pull request
- **WHEN** a session runs on branch fix-123 of a GitHub repository whose pull request 123 from that branch is open
- **THEN** its row shows #123 open

#### Scenario: Azure DevOps draft
- **WHEN** a worktree's branch has Azure DevOps pull request 4521 as a draft
- **THEN** its row shows !4521 draft

#### Scenario: Merged and abandoned
- **WHEN** the only pull requests for two branches are a merged GitHub one numbered 88 and an abandoned Azure DevOps one numbered 4400
- **THEN** the rows show #88 merged and !4400 abandoned

#### Scenario: No pull request
- **WHEN** a row's branch has no pull request
- **THEN** its PR column is blank

### Requirement: The host comes from the origin remote
Vantage SHALL decide where to look from the repository's origin remote URL: a GitHub HTTPS or SSH URL names an owner and repository; an Azure DevOps URL on dev.azure.com, visualstudio.com or their SSH hosts names an organisation, project and repository. A repository with no origin remote, or one on any other host, SHALL have no pull requests looked up.

#### Scenario: GitHub over SSH
- **WHEN** origin is git@github.com:northwind/Northwind.Orders.git
- **THEN** pull requests are looked up in northwind/Northwind.Orders on GitHub

#### Scenario: Azure DevOps with a space in the project
- **WHEN** origin is https://northwind@dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders
- **THEN** pull requests are looked up in organisation northwind, project Northwind Trading, repository Northwind.Orders

#### Scenario: Other host
- **WHEN** origin is https://gitlab.com/northwind/Northwind.Orders.git
- **THEN** no lookup runs and the PR column is blank for that repository

### Requirement: Lookups never delay the listing
Looking up pull requests SHALL run off the UI thread and SHALL NOT delay a refresh of either table: rows SHALL appear with a blank PR column until the repository's first answer arrives, and a later refresh SHALL show it.

#### Scenario: First refresh after start
- **WHEN** Vantage starts and the pull request lookup for a repository takes four seconds
- **THEN** the sessions in that repository are listed at once with a blank PR column, and the refresh after the answer arrives shows their pull requests

### Requirement: Lookups are made once per repository and cached
Vantage SHALL ask each host for a repository's pull requests with one listing call that covers every branch, SHALL reuse the answer, including a failed one, for three minutes, and SHALL NOT start a second call for a repository while one is running.

#### Scenario: Several sessions in one repository
- **WHEN** three sessions run on three branches of the same repository
- **THEN** one listing call is made for that repository and all three rows take their pull request from it

#### Scenario: Within the cache window
- **WHEN** the tables refresh every five seconds for two minutes after a repository was looked up
- **THEN** no further call is made for that repository

#### Scenario: Window passed
- **WHEN** a refresh happens more than three minutes after the last call for a repository
- **THEN** a new call is started and the rows keep the previous answer until it arrives

### Requirement: An open pull request wins over older ones
When a branch has more than one pull request, the row SHALL show the open (or active) one, counting drafts as open; when none is open it SHALL show the most recent.

#### Scenario: Reopened work
- **WHEN** branch fix-123 has closed pull request 90 and open pull request 97
- **THEN** the row shows #97 open

#### Scenario: Nothing open
- **WHEN** branch fix-123 has abandoned pull request 4400 and completed pull request 4460
- **THEN** the row shows !4460 completed

### Requirement: The pull request key opens it in the browser
The pull request key SHALL open the selected row's pull request in the default browser, without passing Vantage's Claude Code session markers to it, and the footer SHALL name it, in both views.

#### Scenario: Open from the sessions view
- **WHEN** the user presses the pull request key on a row whose pull request is !4521
- **THEN** that pull request's page opens in the default browser, without Vantage's Claude Code session markers in the browser's environment, and the footer says !4521 was opened

### Requirement: A missing pull request is explained
When the selected row shows no pull request, the pull request key SHALL open nothing and the footer SHALL say why: no row is selected, the row has no branch, the repository has no origin remote, the host is not GitHub or Azure DevOps, the `gh` or `az` CLI is not installed, the user is not signed in to it, the lookup is still running, it failed, or the branch has no pull request.

#### Scenario: Not signed in
- **WHEN** the user presses the pull request key on a row of an Azure DevOps repository and `az` reports that the user must sign in
- **THEN** nothing opens and the footer says to sign in to the Azure CLI

#### Scenario: CLI missing
- **WHEN** the user presses the pull request key on a row of a GitHub repository and `gh` is not installed
- **THEN** nothing opens and the footer says the GitHub CLI is needed to show pull requests

#### Scenario: Still looking
- **WHEN** the user presses the pull request key before the repository's first lookup has answered
- **THEN** nothing opens and the footer says the pull request is still being looked up

### Requirement: Remote names never run as commands
Names taken from a remote URL SHALL reach the `gh` or `az` CLI only as arguments, never as command text, and an Azure DevOps organisation, project or repository name holding anything other than letters, digits, spaces, dots, hyphens and underscores SHALL be treated as an unknown host.

#### Scenario: Ampersand in a project name
- **WHEN** origin names an Azure DevOps project called Northwind&Trading
- **THEN** no lookup runs and the PR column is blank for that repository
