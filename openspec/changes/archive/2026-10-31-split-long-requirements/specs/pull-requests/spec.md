# Spec Delta

## MODIFIED Requirements

### Requirement: Remote names never run as commands
Names taken from a remote URL, and the branch name, SHALL reach the `gh` or `az` CLI only as arguments, never as command text.

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

## ADDED Requirements

### Requirement: An Azure DevOps name that is not plain text is an unknown host
An Azure DevOps organisation, project or repository name that is not plain text for a batch file (the external-tools rule says which characters are) SHALL be treated as an unknown host: no lookup SHALL run for that repository and its rows' PR column SHALL be blank.

#### Scenario: Percent sign in an organisation name
- **WHEN** origin names an Azure DevOps organisation called north%wind
- **THEN** az is not started for that repository and its PR column is blank

### Requirement: A branch name that is not plain text is not given to az
A branch name that is not plain text for a batch file SHALL NOT be looked up on Azure DevOps: az SHALL NOT be started for it, the row's PR column SHALL be blank, and the pull request key SHALL say the branch name was not given to the Azure CLI.

#### Scenario: Percent sign in a branch name
- **WHEN** a worktree of an Azure DevOps repository is on the branch fix/50%-off
- **THEN** az is not started for it, its PR column is blank, and the pull request key says the branch name was not given to the Azure CLI
