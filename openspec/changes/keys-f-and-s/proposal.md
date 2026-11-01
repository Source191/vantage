# Proposal

## Why

The folder keys are hard to remember: e opens a folder and v a solution, and the same action has a different letter from the one its word suggests. On the Worktrees tab, Enter takes the user back to the Sessions tab rather than to the session itself, so going to a worktree's session is two steps. On the Config tab the selected row's description stands in the status line all the time, repeating the tooltip and pushing the tabs down.

## What Changes

- **BREAKING** Sessions tab: f opens the row's folder (was e) and s opens its solution (was v). e and v do nothing.
- **BREAKING** Worktrees tab: f opens the folder (was e), s opens the solution (was v), and n starts a session in the worktree (was s). e and v do nothing; o (Open shell) is unchanged.
- **BREAKING** Worktrees tab: Enter on a worktree with a running session opens that session's own Windows Terminal tab, or switches to it when one is open, exactly as Enter on the session's row on the Sessions tab does, and the Worktrees tab stays shown. With no session there, or a session Enter cannot open, the status line says why, as it does today.
- **BREAKING** Config tab: f opens the row's folder (was e). e does nothing.
- The key bar words become Open folder (f), Open solution (s) and, on Worktrees, New session (n) and Open session (Enter); the help, tooltips, README and smoke check follow.
- Config tab: the selected row's description is no longer shown as a standing status message; it stays as the row's hover tooltip. The status line on Config shows only messages, as on the other tabs, so it takes no rows when there is nothing to say.
- Version: 0.20.0-preview.1 (pre-1.0 minor, which may break keys).

Out of scope: keeping e and v as aliases; moving o (Open shell); any other key; multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `folder-actions`: the editor key is f and the solution key is s, in both table views.
- `worktree-management`: n starts a session in a worktree; Enter opens or switches to the worktree's session's own tab instead of selecting it on the Sessions tab; the worktree bar's words.
- `console-layout`: the Go to session scenario becomes opening the session's tab; the Sessions bar's second row; the Config description is no longer a status message, so the tone and close rules lose their exception for it.
- `config-tab`: the Config bar reads f Open folder; each row's description is a tooltip only.
- `settings`: the Open folder with dialog names the f key.
- `session-launch`: the fixed-folder wizard opens with n on a worktree row.

## Impact

- `Application\Views\SessionsView`, `WorktreesView`, `ConfigView`, `ConsoleView` and the effect routing in `Tui\TableBody` and `Tui\ConsoleScreen`; `StatusMessage.SettingHint` goes.
- `Application\WordingOfScreens` (keys and help), `WordingOfConsole` (bar rows), `StatusLayout`, `WordingOfStatus`.
- `tools\HeadlessTerminal\SmokeScript` finds keys by their bar word.
- README key tables, `CLAUDE.md`, `openspec\config.yaml` (the s on the worktrees view), the csproj `Version`.
