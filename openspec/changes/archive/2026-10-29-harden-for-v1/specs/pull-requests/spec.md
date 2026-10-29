# Spec Delta

## ADDED Requirements

### Requirement: Pull request links are https
A pull request SHALL be shown and opened only when its page address uses https; a listed pull request with any other address SHALL be left out of the answer as if the host had not listed it.

#### Scenario: A link with another scheme
- **WHEN** the GitHub listing gives a pull request whose url is file:///C:/Users/me/notes.html
- **THEN** that pull request is not shown and the pull request key does not open it

#### Scenario: An https link
- **WHEN** the GitHub listing gives a pull request whose url is https://github.com/northwind/Northwind.Orders/pull/123
- **THEN** the row shows it and the pull request key opens that address

## MODIFIED Requirements

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
