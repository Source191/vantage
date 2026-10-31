# Proposal

## Why

The status line is drawn inside the tab's frame, straight under the detail pane's last row with no rule between them, indented and in cyan. It reads as one more line of the selected row's details, even when it reports something about another session ("Started v-ux (70ebf429). Opened v-ux in a purple tab." under v-test-plan's pane), and a success, a refusal and a failure all look the same. A first try put it above the key bar, outside the frame; the owner found it still easy to miss there, because experienced users do not look at the keys.

## What Changes

- The status line leaves the tabs. It becomes rows of the console directly under the settings row (the versions), above the tabs, owned by the console screen and showing the shown tab's message.
- It takes rows only while there is a message or an action is running. The tabs, counts and table move down to make room, and back up when it clears.
- A message wider than the screen wraps at spaces onto further rows, at most three, the last cut with an ellipsis.
- The working spinner moves with it, to the start of the first row.
- Every message has a tone, shown as a glyph and a colour at its start: a green ✓ for something done, an orange ! for a refusal or a partial result, a red ✗ for a failure, and the spinner in cyan while an action runs. The Config tab's standing description of the selected row is plain white with no glyph.
- A handler's outcome decides its own tone, so a failed start (`StartSessionOutcome.Failed`) is red although it arrives as a report rather than an exception. A message made of a result and what followed it (a removal, then deleting the branch it left) takes the tone of what followed.
- The detail pane goes back to sitting at the bottom of its tab, with nothing drawn under it inside the frame, and the tabs' frame ends directly above the key bar.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `console-layout`: adds where the status line sits (under the settings row, above the tabs, for whichever tab is shown, wrapping when long) and that every message carries a tone. The tabs start under the status line while it shows.
- `session-listing`: the status line's requirement and the "A message" scenario move the status line from beneath the detail pane to under the settings row.

The `worktree-management` and `config-tab` requirements name the status line without saying where it is, so their words stay true and they are not changed.

## Out of scope

- Multiple Claude accounts, which is a later change.
- The failure banner at the right of the tab titles' row, which stays where it is.
- Any change to the words of a message, the key bar, or when a message clears (still when the selection moves or the filter changes).
- Toasts or timed messages: the line stays until something clears it, as now.
- Keeping a row for messages when there are none, or putting short messages beside the versions: the owner chose rows that come and go.

## Impact

- `src\Source191.Vantage.Core\Application`: a status tone (closed enum), the tone of each `Footer` and `Outcome` case beside the words, the glyph per tone, and the wrapping of a message into rows.
- `src\Source191.Vantage.Tui`: `TableBody` loses its footer label, busy spinner and status layout and reports its status to the host; `ConsoleScreen` gains the status rows under the settings row and moves the tabs, counts and table title down while they show.
- Tests: tone facts and a reflection fact that fails for any `Footer` or `Outcome` case without a deliberate tone; wrapping facts.
- The README's description of the screen, and `CLAUDE.md`'s paragraph on the status line.
- `Version` in the tool's csproj rises to the next preview, since the packaged tool changes.
