# Proposal

## Why

The header spends two of its five lines on the machine name and the Claude executable path, which never change for a user on one machine and tell them nothing at a glance, while nothing on screen says which Vantage or Claude Code version is running. The table title `sessions(all)[7]` reads like an index of the selection (and leaves open whether it counts from zero) when it is a row count, and the only explanation of the columns and keys is the README.

## What Changes

- The header's left column on both views reads `Vantage:` (the tool's version), `Claude:` (the Claude Code CLI version, read once at start-up, `unknown` when it cannot be read) and `Roots:` (the repository roots, as today), then the view's own counts. The machine name and the Claude executable path leave the header; the path moves to the `?` help.
- The Vantage tab is titled `Vantage (0.4.9)` instead of `Vantage`, after every hand-off as well as at start-up. A local install shows its prerelease label without the timestamp, `Vantage (0.4.9-local)`.
- The table title says what it counts and names the selection: `7 sessions, selected: fix-123`, or `2 of 7 sessions match "templates", selected: fix-123` while a filter is set; the worktrees view the same with `worktrees` and the selected row's folder name.
- `?` help gains a glossary of the view's columns.
- Each key hint in the header shows its help line as a tooltip when the mouse rests on it. Column-header tooltips are a spike: kept if the headless check shows them working, dropped otherwise.

Out of scope: the worktree root header line (`settings-and-worktree-root`), new columns or keys, removing descriptions (`remove-session-descriptions`), tooltips anywhere other than the header, and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-listing`: the header shows versions and roots instead of machine and executable; the table title counts rows and names the selection; help explains the columns; key hints have tooltips.
- `session-attach`: Vantage's own tab title carries the version.
- `worktree-management`: the worktrees view's title counts rows and names the selection.

## Impact

- `Core`: `ListSelection.Title` takes the noun and the selected row's label; `Wording` gains the tab title, the column glossaries and the key tooltips; `IClaudeCli` gains a nullable version query.
- `Infrastructure`: `ClaudeCli` runs `claude --version`; `TerminalConsole` takes its title from the caller.
- `Tui`: `TableScreen` builds one label per key hint with a tooltip and takes the window title from the caller; `IScreenDefinition` describes keys as pairs and header names without machine and Claude path.
- `Source191.Vantage`: `Program.cs` reads the informational version and the Claude version once and passes the header context and title in.
- Tests: `ListSelectionShould`, `SessionsViewShould`, `WorktreesViewShould`, `WordingShould`, `TerminalConsoleShould`, `ClaudeCliShould`.
- Version: next minor after `main`'s.
