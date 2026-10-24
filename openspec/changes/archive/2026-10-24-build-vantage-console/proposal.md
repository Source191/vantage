# Proposal

## Why

Running several Claude Code sessions at once in Windows Terminal tabs gives no single view of what each is doing, no reliable way back to a session after a reboot, and git worktrees scattered across different layouts. The native `claude agents` view supplies a supervisor, session states and a scriptable JSON listing, so a thin console can be built on documented primitives rather than a competing orchestrator.

## What Changes

- A new dotnet global tool, `vantage`, that runs full-screen in one Windows Terminal tab.
- A sessions view listing every Claude Code session on the machine with state, repository, branch, worktree and description, refreshed from the Claude Code CLI's JSON listing.
- A new-session wizard: pick a repository, give a mandatory name and an optional description, choose the current branch (default) or a new sibling worktree named `<repository>-<slug>`, optionally give an opening prompt, and start the session under Claude Code's supervisor; plus `vantage here` to do the same from the shell in the current folder.
- Drill into a session in the same tab, open it in its own colour-coded Windows Terminal tab, rename it without attaching, stop or delete it, move a plain terminal session under the supervisor, close the tabs of terminal sessions never sent a message, and resume stopped sessions after a reboot.
- A worktrees view across all repository roots with health (ok, locked, orphan, broken, empty), dirty and unpushed counts, and safe removal that refuses to destroy work unless forced with a typed confirmation.
- Attention through the terminal bell: Vantage rings when a background session starts waiting, and Claude Code's own terminal-bell channel covers attached tabs.
- Vantage-owned state limited to one folder under %LOCALAPPDATA%: an attach-tab registry, recent repositories, session descriptions, and a record of worktrees and branches Vantage created.

Out of scope for this change: running two Claude accounts at once (a later change built on the CLAUDE_CONFIG_DIR variable), managing Codex or other agents, cloud sessions, any GUI or mobile view, hosting a session inside Vantage's own pseudo-terminal, focusing an existing Windows Terminal tab (the terminal cannot do it by name), pinning sessions (the CLI exposes no pin verb), and a take-over key that leaves supervision.

## Capabilities

### New Capabilities

- `session-listing`: showing every session with its state, repository, branch, worktree, description and kind, filtered and refreshed.
- `session-launch`: starting a named supervisor-backed session in a chosen repository, on the current branch or in a new sibling worktree, from the wizard or from the shell.
- `session-attach`: working inside a session from the console, in the same tab or a new Windows Terminal tab; renaming, adopting, stopping, deleting, closing empty terminal tabs and resuming sessions.
- `worktree-management`: inventory, health classification and safe removal of git worktrees across the repository roots.
- `needs-input-notification`: the terminal bell when a session needs the user.

### Modified Capabilities

None. This is the first change in the project.

## Impact

- New repository Source191.Vantage: a .NET 10 solution with the tool project, a sociable unit test project and the HeadlessTerminal tool that drives the console in a pseudo-console for checks.
- User settings.json: `worktree.bgIsolation` set to none so background sessions do not move themselves into `.claude\worktrees`, and `preferredNotifChannel` set to `terminal_bell`. No hooks.
- Runtime dependencies: Claude Code CLI 2.1.309 or later, git, Windows Terminal 1.25 or later, PowerShell 7 for the tabs Vantage opens.
