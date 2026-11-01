## MODIFIED Requirements

### Requirement: The pull request key opens it in the browser
The pull request key SHALL open the selected row's pull request in the default browser, without passing Vantage's Claude Code session markers to it, and the status line SHALL name it, in both views. An Azure DevOps pull request's page SHALL be built from the organisation, project and repository of the origin remote, URL-encoded, and the pull request's number, never from a URL in the host's answer.

#### Scenario: Open from the sessions view
- **WHEN** the user presses the pull request key on a row whose pull request is !4521
- **THEN** that pull request's page opens in the default browser, without Vantage's Claude Code session markers in the browser's environment, and the status line says !4521 was opened

#### Scenario: Azure DevOps page from the remote
- **WHEN** origin is https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders and the row's pull request is !4521, listed by az with no web URL
- **THEN** the page https://dev.azure.com/northwind/Northwind%20Trading/_git/Northwind.Orders/pullrequest/4521 opens

### Requirement: A missing pull request is explained
When the selected row shows no pull request, the pull request key SHALL open nothing and the status line SHALL say why: no row is selected, the row has no branch, the repository has no origin remote, the host is not GitHub or Azure DevOps, the `gh` or `az` CLI is not installed, the user is not signed in to it, the lookup is still running, it failed, the branch has no pull request at all, or the branch name holds a character the Azure CLI cannot be given.

#### Scenario: Not signed in
- **WHEN** the user presses the pull request key on a row of an Azure DevOps repository and `az` reports that the user must sign in
- **THEN** nothing opens and the status line says to sign in to the Azure CLI

#### Scenario: CLI missing
- **WHEN** the user presses the pull request key on a row of a GitHub repository and `gh` is not installed
- **THEN** nothing opens and the status line says the GitHub CLI is needed to show pull requests

#### Scenario: Still looking
- **WHEN** the user presses the pull request key before the branch's first lookup has answered
- **THEN** nothing opens and the status line says the pull request is still being looked up

#### Scenario: No pull request
- **WHEN** the user presses the pull request key on a row whose branch has no pull request
- **THEN** nothing opens and the status line says no pull request was found for the row's branch, with no mention of a limit

#### Scenario: Branch not given to az
- **WHEN** the user presses the pull request key on a row of an Azure DevOps repository whose branch is feature/x&calc
- **THEN** nothing opens and the status line says the branch name holds a character the Azure CLI cannot be given
