## MODIFIED Requirements

### Requirement: The version is printed on request
`vantage --version` SHALL print the installed tool's package version to standard output, with any prerelease label and without build metadata such as the commit, and exit 0.

#### Scenario: Version
- **WHEN** Vantage 0.13.0 is installed and the user runs vantage --version
- **THEN** standard output is 0.13.0 and the exit code is 0

#### Scenario: Prerelease version
- **WHEN** Vantage 0.17.0-preview.2 is installed from nuget.org and the user runs vantage --version
- **THEN** standard output is 0.17.0-preview.2, with no `+` and commit after it

#### Scenario: Short form of a local build
- **WHEN** the installed tool's version is 0.13.0-local.20261026T101500 and the user runs vantage -v
- **THEN** it is a parse error with exit code 1, and vantage --version prints a version starting with 0.13.0-local.20261026T101500
