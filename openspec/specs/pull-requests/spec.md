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

### Requirement: An open pull request wins over older ones
When a branch has more than one pull request, the row SHALL show the open (or active) one, counting drafts as open; when none is open it SHALL show the most recent.

#### Scenario: Reopened work
- **WHEN** branch fix-123 has closed pull request 90 and open pull request 97
- **THEN** the row shows #97 open

#### Scenario: Nothing open
- **WHEN** branch fix-123 has abandoned pull request 4400 and completed pull request 4460
- **THEN** the row shows !4460 completed

### Requirement: The pull request key opens it in the browser
The pull request key SHALL open the selected row's pull request in the default browser, without passing Vantage's Claude Code session markers to it, and the footer SHALL name it, in both views. An Azure DevOps pull request's page SHALL be built from the organisation, project and repository of the origin remote, URL-encoded, and the pull request's number, never from a URL in the host's answer.

#### Scenario: Open from the sessions view
- **WHEN** the user presses the pull request key on a row whose pull request is !4521
- **THEN** that pull request's page opens in the default browser, without Vantage's Claude Code session markers in the browser's environment, and the footer says !4521 was opened

#### Scenario: Azure DevOps page from the remote
- **WHEN** origin is https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders and the row's pull request is !4521, listed by az with no web URL
- **THEN** the page https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders/pullrequest/4521 opens

### Requirement: A missing pull request is explained
When the selected row shows no pull request, the pull request key SHALL open nothing and the footer SHALL say why: no row is selected, the row has no branch, the repository has no origin remote, the host is not GitHub or Azure DevOps, the `gh` or `az` CLI is not installed, the user is not signed in to it, the lookup is still running, it failed, the branch has no pull request at all, or the branch name holds a character the Azure CLI cannot be given.

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

#### Scenario: Branch not given to az
- **WHEN** the user presses the pull request key on a row of an Azure DevOps repository whose branch is feature/x&calc
- **THEN** nothing opens and the footer says the branch name holds a character the Azure CLI cannot be given

### Requirement: Remote names never run as commands
Names taken from a remote URL, and the branch name, SHALL reach the `gh` or `az` CLI only as arguments, never as command text. An Azure DevOps organisation, project or repository name that is not plain text for a batch file (the external-tools rule says which characters are) SHALL be treated as an unknown host, and a branch name that is not SHALL NOT be looked up on Azure DevOps: az SHALL NOT be started for it, the row's PR column SHALL be blank, and the pull request key SHALL say the branch name was not given to the Azure CLI.

#### Scenario: Ampersand in a project name
- **WHEN** origin names an Azure DevOps project called Northwind&Trading
- **THEN** no lookup runs and the PR column is blank for that repository

#### Scenario: Ampersand in a branch name
- **WHEN** a worktree of an Azure DevOps repository is on the branch feature/x&calc
- **THEN** az is not started for it, its PR column is blank, and the pull request key says the branch name holds a character the Azure CLI cannot be given

#### Scenario: Slash in a branch name
- **WHEN** a worktree of an Azure DevOps repository is on the branch feature/refund-retry
- **THEN** az is asked for that branch

#### Scenario: Any branch name on GitHub
- **WHEN** a worktree of a GitHub repository is on the branch feature/x&calc
- **THEN** gh is asked for that branch, since gh is an executable and reads its arguments as given

### Requirement: Pull request links are https
A pull request SHALL be shown and opened only when its page address uses https; a listed pull request with any other address SHALL be left out of the answer as if the host had not listed it.

#### Scenario: A link with another scheme
- **WHEN** the GitHub listing gives a pull request whose url is file:///C:/Users/me/notes.html
- **THEN** that pull request is not shown and the pull request key does not open it

#### Scenario: An https link
- **WHEN** the GitHub listing gives a pull request whose url is https://github.com/northwind/Northwind.Orders/pull/123
- **THEN** the row shows it and the pull request key opens that address

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
