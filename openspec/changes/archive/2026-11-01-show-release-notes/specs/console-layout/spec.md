# Spec Delta

## MODIFIED Requirements

### Requirement: The settings take one line
The first row SHALL show the versions, labels orange, values white, parts separated by a grey ·: Vantage with its version and memory, then Claude and its version, with the update notice, in amber, and the below-minimum warning in their places. The repository roots and the worktree root SHALL be shown on the Config tab, not here.

#### Scenario: The row
- **WHEN** Vantage 0.18.0 uses 35 MB with Claude Code 2.1.310
- **THEN** the first row reads Vantage 0.18.0 (35 MB) · Claude 2.1.310, the labels orange and the values white, and no row above the tabs names a root

#### Scenario: An update is available
- **WHEN** 0.19.0 is the newest release
- **THEN** the first row reads Vantage 0.18.0 (35 MB; 0.19.0 available, U to update) · Claude 2.1.310, with 0.19.0 available, U to update in amber and the rest as before
