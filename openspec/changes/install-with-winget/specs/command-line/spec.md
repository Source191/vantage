# Spec Delta

## MODIFIED Requirements

### Requirement: The version is printed on request
`torni --version` SHALL print the installed tool's package version to standard output, with any prerelease label and without build metadata such as the commit, and exit 0.

#### Scenario: Version
- **WHEN** Torni 0.13.0 is installed and the user runs torni --version
- **THEN** standard output is 0.13.0 and the exit code is 0

#### Scenario: Prerelease version
- **WHEN** Torni 0.17.0-preview.2 is installed from its zip and the user runs torni --version
- **THEN** standard output is 0.17.0-preview.2, with no `+` and commit after it

#### Scenario: Short form of a local build
- **WHEN** the installed tool's version is 0.13.0-local.20261026T101500 and the user runs torni -v
- **THEN** it is a parse error with exit code 1, and torni --version prints a version starting with 0.13.0-local.20261026T101500
