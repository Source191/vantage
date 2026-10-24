# Design

## Context

Both screens already run every action through one `RunInBackground` helper that sets a working footer, awaits off the UI thread, and puts the outcome on the footer through `app.Invoke`; the sessions wizard start is the one action with its own copy of that shape. The first load is a `TriggerLoad` whose rows arrive through the reducer as `RowsLoaded` or `WorktreeRowsLoaded`.

## Goals / Non-Goals

**Goals:** the out-of-the-box `SpinnerView` with `AutoSpin`; the reducer decides when the first load is pending (tested); the Terminal.Gui layer only shows or hides.

**Non-Goals:** hand-rolled frames; spinners during the timed refreshes.

## Decisions

- **Placement:** actions spin in the footer, where their outcome lands and where the table stays readable; the first load spins centred over the empty table, where there is nothing else to show.
- **`Loaded` on the view state**, set by the first `RowsLoaded`; `LoadFailed` leaves it false and the banner takes over the explanation. The screen shows the centred spinner only while `!Loaded && Banner is null`.
- **`AutoSpin` is switched on when a spinner is shown and off when hidden**, inside the running application (from `RunInBackground` and from `Redraw`), never in the constructor, so the spinner's timeout never outlives the action or the run.
- **Busy nesting:** the delete flows ask a question from the continuation and then start another background action; the helper stops the footer spinner before the continuation runs, and the next action starts it again.
- **`SyncWithTerminal`** (mirroring to the terminal's progress indicator) stays off: Windows Terminal would show a taskbar progress ring for every two-second action.
- **Verification:** the hand-off check already opens the wizard and quits; a `drive` script that presses `n`, F5 on a half-filled form (refused, no start) and `q` proves no spinner timeout keeps the application alive.

## Risks / Trade-offs

- [A spinner left spinning if an action throws] → the helper's catch path goes through the same continuation that hides it.
- [Braille glyphs in a font without them] → `SpinnerStyle.Dots` is what Windows Terminal's default font renders; the headless capture reads them as text.
