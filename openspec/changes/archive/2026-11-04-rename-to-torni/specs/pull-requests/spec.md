## MODIFIED Requirements

### Requirement: The host comes from the origin remote
Torni SHALL decide where to look from the repository's origin remote URL: a GitHub HTTPS or SSH URL names an owner and repository; an Azure DevOps URL on dev.azure.com, visualstudio.com or their SSH hosts names an organisation, project and repository. A user name before the host in an HTTPS URL SHALL be accepted on either host and never kept. A repository with no origin remote, or one on any other host, SHALL have no pull requests looked up.

#### Scenario: GitHub over SSH
- **WHEN** origin is git@github.com:northwind/Northwind.Orders.git
- **THEN** pull requests are looked up in northwind/Northwind.Orders on GitHub

#### Scenario: GitHub over HTTPS with a user name
- **WHEN** origin is https://northwind@github.com/northwind/Northwind.Orders.git
- **THEN** pull requests are looked up in northwind/Northwind.Orders on GitHub, and the user name is not part of what is looked up

#### Scenario: Azure DevOps with a space in the project
- **WHEN** origin is https://northwind@dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders
- **THEN** pull requests are looked up in organisation northwind, project Northwind Trading, repository Northwind.Orders

#### Scenario: Other host
- **WHEN** origin is https://gitlab.com/northwind/Northwind.Orders.git
- **THEN** no lookup runs and the PR column is blank for that repository

### Requirement: Lookups never delay the listing
Looking up pull requests SHALL run off the UI thread and SHALL NOT delay a refresh of either table: rows SHALL appear with a blank PR column until the repository's first answer arrives, and a later refresh SHALL show it.

#### Scenario: First refresh after start
- **WHEN** Torni starts and the pull request lookup for a repository takes four seconds
- **THEN** the sessions in that repository are listed at once with a blank PR column, and the refresh after the answer arrives shows their pull requests

### Requirement: The pull request key opens it in the browser
The pull request key SHALL open the selected row's pull request in the default browser, without passing Torni's Claude Code session markers to it, and the status line SHALL name it, in both views. An Azure DevOps pull request's page SHALL be built from the organisation, project and repository of the origin remote, URL-encoded, and the pull request's number, never from a URL in the host's answer.

#### Scenario: Open from the sessions view
- **WHEN** the user presses the pull request key on a row whose pull request is !4521
- **THEN** that pull request's page opens in the default browser, without Torni's Claude Code session markers in the browser's environment, and the status line says !4521 was opened

#### Scenario: Azure DevOps page from the remote
- **WHEN** origin is https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders and the row's pull request is !4521, listed by az with no web URL
- **THEN** the page https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders/pullrequest/4521 opens

### Requirement: Lookups are made per branch and cached
Torni SHALL ask a host for the pull requests of one branch at a time, only for branches that have a row on screen, SHALL reuse a branch's answer for 60 seconds and a failed or unavailable answer for three minutes, SHALL NOT start a second lookup for a branch while one is running, and SHALL run at most four lookups at once.

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
