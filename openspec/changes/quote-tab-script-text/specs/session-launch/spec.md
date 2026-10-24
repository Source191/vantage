# Spec Delta

## MODIFIED Requirements

### Requirement: Untrusted workspaces are handled
When starting the session fails because the directory's workspace is not trusted, the wizard SHALL say so and offer to open a plain Claude Code tab in that directory so the trust dialog can be accepted, then retry.

#### Scenario: Workspace not trusted
- **WHEN** the CLI exits with a message that the workspace is not trusted
- **THEN** the wizard offers to open a plain Claude Code tab there and retries the start after the user confirms

#### Scenario: Folder name with an apostrophe
- **WHEN** the untrusted folder is named with a straight or curly apostrophe, such as `Bob's tools`
- **THEN** the trust tab shows its message with the folder name exactly as it is and starts Claude Code there
