# Proposal

## Why

Several things Vantage does take a second or more (starting a session, opening a tab, renaming, resuming after a reboot, removing a worktree) and the first worktree scan can take forty seconds, during which the only sign of life is a footer ending in three dots, or an empty table. A spinner makes the wait visible and shows the program has not hung.

## What Changes

- While an action runs off the UI thread (start, open in tab, switch to tab, stop, adopt, rename, delete, close empty tabs, resume, prune, open shell, remove, branch delete) the footer shows a spinning glyph before its "working" text; it disappears when the action reports.
- Until the first listing or scan has arrived, a spinner with "Listing sessions" or "Scanning worktrees" sits in the middle of the empty table; it disappears with the first rows and never returns on the timed refreshes. A listing failure replaces it with the existing banner.
- The spinner is Terminal.Gui's own `SpinnerView` (braille dots, automatic spin); nothing new is drawn by hand.

Out of scope: a progress bar for the worktree scan (git gives no progress), spinners in the console paths (`vantage here`, attach), the five and fifteen second refreshes (they stay silent).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-listing`: a new requirement that the view shows it is waiting for the first listing and while an action runs.
- `worktree-management`: a new requirement that the view shows it is scanning until the first scan arrives and while an action runs.

## Impact

- `Screens\SessionsView` and `Screens\WorktreesView` states gain a `Loaded` flag set by the first rows (reducer, tested).
- `Screens\Tui\SessionsScreen` and `WorktreesScreen`: a `SpinnerView` beside the footer driven by the background-action helper, and a centred one over the table until loaded; the wizard start path moves onto the same helper.
- README: one line under the sessions view. No new dependencies.
