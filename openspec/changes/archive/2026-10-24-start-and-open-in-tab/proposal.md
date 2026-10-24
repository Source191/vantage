# Proposal

## Why

The new-session wizard's F6 ("start and open here") attaches to the new session in the Vantage tab, so the overview disappears the moment work starts and the user has to detach to see other sessions. The common case is to start a session and keep it in its own Windows Terminal tab, which today takes two steps: F5, then `o` on the new row.

## What Changes

- **BREAKING** (keys): F6 in the new-session wizard becomes "Start and open in tab": the session starts under the supervisor and Vantage opens it in its own colour-coded Windows Terminal tab, exactly as the `o` key does for a listed session, and the overview stays on screen. The same key does the same thing in the worktrees view's `s` wizard.
- The "start and open here" behaviour (attach in the same tab straight after starting) is removed from the wizard. Enter on the new row still attaches in the same tab.
- Starting a session now resolves the session the supervisor listed (its full session id) rather than only its job id, so a tab can be registered for it at once.
- When the new session is not listed yet, Vantage still reports it as started and says to press `o` once it appears, rather than failing the start.
- README key table, the "start work on a story" scenario and the wizard's hints and button text change to match.

Out of scope: a separate key that keeps the old attach-in-this-tab behaviour; attaching to one session from two tabs; multiple Claude accounts (a later change built on CLAUDE_CONFIG_DIR); any change to the `o` key, tab colours or tab liveness tracking.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-launch`: the requirement "The session starts under the supervisor with its name" changes its follow-up: "start and open here" (attach in the same tab) is replaced by "start and open in a tab" (open the new session in its own Windows Terminal tab, as the open-in-tab key does).

## Impact

- `Launch\LaunchService` and `LaunchOutcome`: the outcome carries the listed session as well as the job id.
- `Launch\LaunchWorkflow`: gains the tab opener so both wizards (sessions and worktrees views) can open the tab without a new screen dependency.
- `Actions\TabOpener`: opens a tab for a session that is not yet a listed row.
- `Screens\Tui\LaunchWizardDialog`, `SessionsScreen`, `WorktreesScreen`: F6 result, hints, button and footer text.
- `Program.cs` wiring order (tab opener before the launch workflow), README and the `session-launch` spec.
- No new dependencies; no change to Vantage's state folder format.
