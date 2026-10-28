# Spec Delta

## MODIFIED Requirements

### Requirement: Gitignored local files are copied into a new worktree
When the repository contains a `.worktreeinclude` file, Vantage SHALL copy the gitignored files it matches into the new worktree before starting the session.

#### Scenario: Include file present
- **WHEN** `.worktreeinclude` lists appsettings.Development.json and that file is gitignored in the main checkout
- **THEN** the file exists in the new worktree before the session starts

#### Scenario: Names with non-ASCII characters, spaces and apostrophes
- **WHEN** `.worktreeinclude` matches the gitignored files Café.local.json, my app.local.json and it's.local.json
- **THEN** all three exist in the new worktree under the same names, and the copy reports all three
