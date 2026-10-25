# Spec Delta

## MODIFIED Requirements

### Requirement: The header shows the versions and the roots
The header's context SHALL show the Vantage version, the Claude Code CLI version, the repository roots and the worktree root, and SHALL NOT show the machine name or the Claude executable's path. A local build SHALL show its prerelease label without the build timestamp, and a Claude version that cannot be read SHALL show as unknown. The roots and the worktree root SHALL be the settings saved now, so a save from the settings dialog shows at once.

#### Scenario: Released versions
- **WHEN** Vantage 0.5.0 runs with Claude Code 2.1.310 and the root C:\Users\me\source\repos
- **THEN** the header reads Vantage: 0.5.0, Claude: 2.1.310, Roots: C:\Users\me\source\repos and Worktree root: C:\Users\me\source\worktrees above the session counts

#### Scenario: Local build
- **WHEN** the installed tool's version is 0.5.0-local.20261025T101500
- **THEN** the header reads Vantage: 0.5.0-local

#### Scenario: Claude version unreadable
- **WHEN** the Claude Code CLI's version command fails or prints something without a version
- **THEN** the header reads Claude: unknown and the view works as usual

#### Scenario: Default worktree root
- **WHEN** Vantage starts with no settings file
- **THEN** the header shows C:\Users\me\source\worktrees as the worktree root on both views
