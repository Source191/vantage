# Proposal

## Why

`openspec validate --specs --strict` fails on `main`: 20 requirements in eight specs have a description over 500 characters, because each states several behaviours in one paragraph. That makes the strict check useless as a gate and each requirement hard to read, test and change on its own. The owner asked for the split on 31 October 2026, after config-tab-follow-ups merged.

## What Changes

- **Each long requirement keeps its name and every scenario,** with its description cut down to its first behaviour and its meaning unchanged.
- **Each behaviour cut out becomes an ADDED requirement of its own** in the same spec, with one or more scenarios that state what the code does today.
- **No behaviour changes.** Nothing in `src\`, the README or CLAUDE.md changes, and `Version` stays as it is (the pull request touches only `openspec\`).
- **Stale requirements found while splitting are listed, not fixed.** Correcting what a requirement says is a behaviour-contract change for a change of its own; see design.md.

Out of scope: requirements of 500 characters or fewer, however long their scenarios; rewording requirements for style; changing any requirement's meaning.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `command-line`: "A name that looks like an option needs --" split in two.
- `folder-actions`: "The folder is shown relative to its root" and "The solution key opens the folder's solution" split.
- `needs-input-notification`: "Claude Code's own bell covers attached tabs" and "The setup command adds Vantage's Windows Terminal profiles" split.
- `pull-requests`: "Remote names never run as commands" split.
- `session-attach`: "Open a session in its own Windows Terminal tab", "Delete offers to remove the session's worktree", "Rename without attaching", "Terminal sessions can be adopted", "Empty terminal sessions can be closed" and "Vantage's own tab is recognisable" split.
- `session-listing`: "The screen is a header, a table and a footer" and "The header shows the versions and the roots" split.
- `settings`: "The editor is a preset or a custom command" and "Settings are validated before they are saved" split.
- `worktree-management`: "Leftover folders are detected", "Leftover folders can be deleted as plain folders", "The worktrees header and help match the sessions view" and "One WORKTREE column names the worktree" split.

## Impact

- **Specs:** the eight files above under `openspec\specs`, rewritten at archive by the deltas; no other file.
- **Code, docs, version:** none. CI's documentation-only path applies (the pull request touches only `openspec\`).
- **Checks:** `openspec validate split-long-requirements --strict` before archiving and `openspec validate --specs --strict` passing on all 16 specs after.
