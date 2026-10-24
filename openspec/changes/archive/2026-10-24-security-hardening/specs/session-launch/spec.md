# Spec Delta

## ADDED Requirements

### Requirement: Copied local files stay inside the new worktree
The `.worktreeinclude` copy SHALL only read files inside the repository and only write files inside the new worktree; a listed path that is parent-relative, rooted or a network path SHALL be skipped, and nothing SHALL be written outside the worktree.

#### Scenario: Parent-relative path in git's listing
- **WHEN** git's ignored-file listing names ../outside.local.json and `.worktreeinclude` matches every file
- **THEN** that path is not copied and no file appears next to the worktree

#### Scenario: Rooted and network paths in git's listing
- **WHEN** git's ignored-file listing names C:/Users/me/secrets.local.json and //server/share/secrets.local.json
- **THEN** neither is copied, and only the in-repository matches reach the worktree
