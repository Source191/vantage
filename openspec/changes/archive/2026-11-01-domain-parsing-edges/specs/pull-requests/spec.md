## MODIFIED Requirements

### Requirement: The host comes from the origin remote
Vantage SHALL decide where to look from the repository's origin remote URL: a GitHub HTTPS or SSH URL names an owner and repository; an Azure DevOps URL on dev.azure.com, visualstudio.com or their SSH hosts names an organisation, project and repository. A user name before the host in an HTTPS URL SHALL be accepted on either host and never kept. A repository with no origin remote, or one on any other host, SHALL have no pull requests looked up.

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
